using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommDevice;
using ThreeNH.CommProtocol;
using ThreeNH.CommunicationBase;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.StructHelper;
using ThreeNH.Optical;

namespace ThreeNH.SQCX.Infrastructure.Instruments.ColorReader
{
    /// <summary>
    /// 命令处理代理
    /// </summary>
    /// <param name="command">接收到的命令</param>
    /// <param name="data">接收到的数据</param>
    /// <param name="remoteError">远端传过来的错误码</param>
    /// <returns>返回真表示已处理该命令，假表示未处理</returns>
    internal delegate bool CommandHandler(byte command, byte[] data, int remoteError, bool isLast);

    public class Spectrometer : IDisposable, IColorimeter, IMetrology, ISpecimenClearing,  IRecordManagement, ISystemSetting, IMeasurementNotification
    {
        #region Fields

        private bool _isDisposed = false;
        private const int DefaultTimeout = 5000;

        private ClientProtocol _clientProtocol = new ClientProtocol();
        private int _commError = 0;
        private ManualResetEvent _connectedEvent = new ManualResetEvent(false);
        private AutoResetEvent _ackEvent = new AutoResetEvent(false);
        private AutoResetEvent _recvEvent = new AutoResetEvent(false);
        private Mutex _mutex = new Mutex(false);
        private List<byte> _recvBuffer = new List<byte>();
        private byte _currentCommand = 0; // 当前正在接收的命令
        private Command _waitingCommand = Command.InvalidCommand; // 当前正在等待的命令
        private Dictionary<byte, CommandHandler> _commandHandlers = new Dictionary<byte, CommandHandler>();
        private UploadSpecimenAsyncContext _uploadSpecimenAsyncContext;

        #endregion Fields

        #region Local Classes

        private class UploadSpecimenAsyncContext
        {
            public List<byte> Data = new List<byte>();
            public EventHandler<SpecimenReceivedEventArgs> Handler;
        }

        #endregion Local Classes

        public Spectrometer()
        {
            _clientProtocol.Connected += OnSessionConnected;
            _clientProtocol.Disconnected += OnSessionDisconnected;
            _clientProtocol.Received += OnDataReceived;
            _clientProtocol.SendingProgressChanged += OnDataSendingProgressChanged;
            // _clientProtocol.PassThroughDataReceived += OnPassthoughDataReceived;

            _commandHandlers[(byte)Command.UploadStandards] = HandleUploadRecordsCommand;
            _commandHandlers[(byte)Command.UploadTrials] = HandleUploadRecordsCommand;
            _commandHandlers[(byte)Command.MeasureStandard] = HandleMeasureCommand;
            _commandHandlers[(byte)Command.MeasureTrial] = HandleMeasureCommand;
        }

        private bool HandleMeasureCommand(byte command, byte[] data, int remoteError, bool islast)
        {
            if (MeasurementReceived == null)
            {
                return false;
            }

            if (command == (byte)_waitingCommand)
            {
                return false;
            }

            if (data == null || data.Length == 0)
            {
                return false;
            }

            try
            {
                Specimen specimen = null;
                if (command == (byte)Command.MeasureStandard)
                {
                    specimen = StructConverter.BytesToStruct<StandardRecord>(data).ToSpecimen();
                }
                else
                {
                    specimen = StructConverter.BytesToStruct<TrialRecord>(data).ToSpecimen();
                }

                // 如果名称是No Name，表示仪器端没有命名，将其名字设为空以便在添加到SQCX列表时自动命名
                if (specimen.Name == "No Name")
                {
                    specimen.Name = null;
                }
                MeasurementReceived.Invoke(this, new MeasurementNotificationArgs(specimen));
                return true;
            }
            catch
            {
                return false;
            }
        }

        ~Spectrometer()
        {
            Dispose();
        }

        #region IDisposable

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _clientProtocol.Dispose();
                _connectedEvent.Dispose();
                _ackEvent.Dispose();
                _recvEvent.Dispose();
                _mutex.Dispose();

                _isDisposed = true;
            }
        }

        #endregion IDisposable

        public ClientProtocol Protocol => _clientProtocol;

        private void OnDataSendingProgressChanged(object sender, SendingProgressChangedEventArgs e)
        {
            if (_sendingProgressChanged != null)
            {
                int precentage = (e.Total != null)
                    ? ((e.Total == 0) ? 100 : ((int)(100.0 * e.SentCount / (int)e.Total)))
                    : 0;
                if (e.ErrorCode == ErrorCode.Successfull)
                {
                    _sendingProgressChanged.BeginInvoke(this, new ProgressChangedEventArgs(precentage, null), null,
                        null);
                }
                else
                {
                    var ex = new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        $"Error code: {e.RemoteErrorCode}");
                    _sendingProgressChanged.BeginInvoke(this, new ProgressChangedEventArgs(precentage, ex), null,
                        null);
                }

                if (e.IsCompleted)
                {
                    _sendingProgressChanged = null;
                }
            }

            _currentCommand = e.Operation;

            if (e.ErrorCode != ErrorCode.Successfull)
            {
                Trace.TraceWarning($"发送数据出错：{e.ErrorCode}");
                if (!_commandHandlers.ContainsKey(e.Operation) ||
                    !_commandHandlers[e.Operation](e.Operation, null, e.RemoteErrorCode, true))
                {
                    _commError = e.RemoteErrorCode;
                    _ackEvent.Set();
                }

                return;
            }

            if (e.IsCompleted && !Commands.HasAckData((Command)e.Operation))
            {
                if (!_commandHandlers.ContainsKey(e.Operation) ||
                    !_commandHandlers[e.Operation](e.Operation, null, 0, true))
                {
                    _ackEvent.Set();
                }
            }
        }

        private void OnDataReceived(object sender, ReceivedEventArgs e)
        {
            if (e.Operation == 0)
            {
                return;
            }

            // Trace.TraceInformation($"接收到：{(Command)e.Operation}, offset = {e.Offset}, length = {e.Data.Length}");

            if (e.Offset == 0)
            {
                _currentCommand = e.Operation;
            }

            if (e.Data != null && e.Data.Length > 0)
            {
                lock (_recvBuffer)
                {
                    _recvBuffer.AddRange(e.Data);
                }
            }

            if (!_commandHandlers.ContainsKey(e.Operation) ||
                !_commandHandlers[e.Operation](e.Operation, e.Data, e.RemoteErrorCode, e.IsLast))
            {
                if (e.IsLast || e.Data == null)
                {
                    _commError = e.RemoteErrorCode;
                    _ackEvent.Set();
                }

                _recvEvent.Set();
            }
        }

        private bool HandleUploadRecordsCommand(byte command, byte[] data, int remoteError, bool isLast)
        {
            if (_uploadSpecimenAsyncContext == null)
            {
                return false;
            }

            lock (_uploadSpecimenAsyncContext)
            {
                if (remoteError != 0)
                {
                    _uploadSpecimenAsyncContext.Handler.BeginInvoke(this,
                        new SpecimenReceivedEventArgs(new CommunicationException(
                            CommunicationError.ExecuteCommandFailed,
                            $"Error Code: {remoteError}")), null, null);
                }
                else
                {
                    _uploadSpecimenAsyncContext.Data.AddRange(data);
                    int recordSize = command == (byte)Command.UploadStandards
                        ? Marshal.SizeOf(typeof(StandardRecord))
                        : Marshal.SizeOf(typeof(TrialRecord));
                    List<Specimen> list = new List<Specimen>();
                    for (int i = 0; i * recordSize < _uploadSpecimenAsyncContext.Data.Count; i++)
                    {
                        if ((byte)Command.UploadStandards == command)
                        {
                            list.Add(StructConverter
                                .BytesToStruct<StandardRecord>(_uploadSpecimenAsyncContext.Data, i * recordSize)
                                .ToSpecimen());
                        }
                        else
                        {
                            list.Add(StructConverter
                                .BytesToStruct<TrialRecord>(_uploadSpecimenAsyncContext.Data, i * recordSize)
                                .ToSpecimen());
                        }
                    }

                    if (list.Count > 0)
                    {
                        _uploadSpecimenAsyncContext.Data.RemoveRange(0, list.Count * recordSize);
                        _uploadSpecimenAsyncContext.Handler.BeginInvoke(this,
                            new SpecimenReceivedEventArgs(list.ToArray(), !isLast), null, null);
                    }
                }

                if (isLast)
                {
                    _uploadSpecimenAsyncContext = null;
                }
                return true;
            }
        }

        private void OnSessionDisconnected(object sender, EventArgs eventArgs)
        {
            _ackEvent.Set();
        }

        private void OnSessionConnected(object sender, ConnectedEventArgs e)
        {
            _connectedEvent.Set();
        }

        private CommandPacket SendAndWait(CommandPacket packet, int timeout = DefaultTimeout)
        {
            Send(packet);
            var result = Wait(packet.Command, timeout);
            if (result == null)
            {
                // 特殊处理SVD写入命令
                if (packet.Command == (byte)Command.GetKmtcMode)
                {
                    result = new CommandPacket(packet.Command, null);
                    result.AckCode = 0;
                    return result;
                }
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }

            if (result.AckCode != 0)
            {
                Trace.TraceInformation($"执行失败，收到错误码：{(RemoteError)result.AckCode}");
                throw new CommunicationException(((RemoteError)result.AckCode).ToCommError());
            }

            if (!IsOpen)
            {
                // 连接已断开
                throw new CommunicationException(CommunicationError.DeviceDisconnected);
            }

            return result;
        }


        public ICommDevice CommunicationDevice
        {
            get { return _clientProtocol?.CommDevice; }
            set
            {
                if (IsOpen)
                {
                    throw new CommunicationException(CommunicationError.DeviceHasOpened);
                }
                _clientProtocol.CommDevice = value;
                DeviceName = value?.DeviceName;
                if (_clientProtocol.IsOpen)
                {
                    CreateSession();
                }
            }
        }

        #region ISystemSetting

        private SpecularComponent _specularComponent;

        public SpecularComponent SpecularComponent
        {
            get { return _specularComponent; }
            set
            {
                SetSpecularComponent(value);
                _specularComponent = value;
            }
        }

        private LensSwitchMode _lensSwitchMode;

        public LensSwitchMode LensSwitchMode
        {
            get { return _lensSwitchMode; }
            set
            {
                SetLensSwitchMode(value);
                _lensSwitchMode = value;
            }
        }

        private LensPosition _lensPosition;

        public LensPosition LensPosition
        {
            get { return _lensPosition; }
            set
            {
                SetLensPosition(value);
                _lensPosition = value;
            }
        }

        private MeasurementCaliber _aperture;

        public MeasurementCaliber Aperture
        {
            get { return _aperture; }
            set
            {
                SetMeasurementCaliber(value);
                _aperture = value;
            }
        }

        private StandardIlluminant _standardIlluminant;

        public StandardIlluminant StandardIlluminant
        {
            get { return _standardIlluminant; }
            set
            {
                SetStandardIlluminant(value);
                _standardIlluminant = value;
            }
        }

        private StandardObserver _observer;

        public StandardObserver StandardObserver
        {
            get { return _observer; }
            set
            {
                SetStandardObserver(value);
                _observer = value;
            }
        }

        private Optical.ColorSpace _colorSpace;

        public Optical.ColorSpace ColorSpace
        {
            get { return _colorSpace; }
            set
            {
                SetColorSpace(value);
                _colorSpace = value;
            }
        }

        private ColorDiffFormula _colorDiffFormula;

        public ColorDiffFormula ColorDiffFormula
        {
            get { return _colorDiffFormula; }
            set
            {
                SetColorDiffFormula(value);
                _colorDiffFormula = value;
            }
        }

        private Optical.ColorIndex _colorIndex;

        public Optical.ColorIndex ColorIndex
        {
            get { return _colorIndex; }
            set
            {
                SetColorIndex(value);
                _colorIndex = value;
            }
        }

        private UVFlag _uvFlag;

        public UVFlag UvMode
        {
            get { return _uvFlag; }
            set
            {
                SetUvMode(value);
                _uvFlag = value;
            }
        }

        public DateTime DateTime
        {
            get { return GetRtcDateTime(); }
            set { SetRtcDateTime(value); }
        }

        public ColorimeterInterface.Tolerance SystemDefaultTolerance
        {
            get
            {
                var t = GetTolerance();
                return new ColorimeterInterface.Tolerance
                {
                    dE = t.dE,
                    dL = new Tuple<double, double>(t.dL_Lower, t.dL_Upper),
                    da = new Tuple<double, double>(t.da_Lower, t.da_Upper),
                    db = new Tuple<double, double>(t.db_Lower, t.db_Upper),
                };
            }
            set
            {
                var t = new Tolerance
                {
                    dE = (float)value.dE,
                    dL_Lower = (float)value.dL.Item1,
                    dL_Upper = (float)value.dL.Item2,
                    da_Lower = (float)value.da.Item1,
                    da_Upper = (float)value.da.Item2,
                    db_Lower = (float)value.db.Item1,
                    db_Upper = (float)value.db.Item2
                };
                SetTolerance(t);
            }
        }

        public CultureInfo Language { get; set; }

        public object GetSetting(string propertyName)
        {
            return typeof(Spectrometer).GetProperty(propertyName)?.GetValue(this, null);
        }

        public void SetSetting(string propertyName, object value)
        {
            typeof(Spectrometer).GetProperty(propertyName)?.SetValue(this, value, null);
        }

        public bool IsUvModifiable
        {
            get { return true; }
        }

        public bool IsSpecularComponentModifiable
        {
            get { return true; }
        }

        public bool IsSpectrumMeasurementModeModifiable
        {
            get { return true; }
        }

        private SpectrumMeasurementMode _spectrumMeasurementMode;

        public SpectrumMeasurementMode SpectrumMeasurementMode
        {
            get { return _spectrumMeasurementMode; }
            set
            {
                SetSpectrumMeasurementMode(value);
                _spectrumMeasurementMode = value;
            }
        }

        public void RefreshCaches()
        {
            RefreshInstrumentInfo();
        }

        #endregion ISystemSetting

        #region IInstrument

        public string Model { get; private set; }

        public InternalModelType _InternalModelType { get; private set; }
        public string FirmwareRevision { get; private set; }
        public string HardwareRevision { get; private set; }
        public string SN { get; private set; }

        public string SoftwareRevision
        {
            get { return FirmwareRevision; }
        }

        public string Manufacturer { get; }
        public string WhiteBoardNumber { get; private set; }

        public DeviceType DeviceType
        {
            get { return DeviceType.Spectrophotometer; }
        }

        public string OpticalStructure { get; private set; }

        public bool IsOpen => _clientProtocol.IsConnected;

        public WavelengthRange WavelengthRange
        {
            get { return new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP); }
            set { throw new NotSupportedException(); }
        }

        public void DoBlackCalibration()
        {
            Calibrate(true);
        }

        public void ProductBlackCalibration()
        {
            SendAndWait(Commands.ProductBlackCalibration(), 10000);
        }

        public void ProductWhiteCalibration()
        {
            SendAndWait(Commands.ProductWhiteCalibration(), 10000);
        }

        public Specimen Measure()
        {
            return Measure(MeasurementType.Standard, null);
        }

        //private static bool ifMeasuredStdLilin = false;//20191014lilin add
        public Specimen MeasureStandard()
        {
            //ifMeasuredStdLilin = true;

            return Measure(MeasurementType.Standard, null);
        }

        public Specimen MeasureSample()
        {
            //if (ifMeasuredStdLilin == false)
            //{
            //    return null;
            //}
            return Measure(MeasurementType.Sample, null);
        }

        public bool IsCalibrated
        {
            get
            {
                var status = GetInstrumentStatus();
                return status.IsCalibrated;
            }
        }

        public void DoWhiteCalibration()
        {
            Calibrate(false);
        }

        public string DeviceName
        {
            get;
            private set;
        }

        public void Open(object commDevice = null)
        {
            if (commDevice is ICommDevice)
            {
                CommunicationDevice = (ICommDevice)commDevice;
                DeviceName = CommunicationDevice.DeviceName;
            }
            else
            {
                Connect(commDevice);
            }
        }

        public void Close()
        {
            Disconnect();
        }

        #endregion IInstrument

        #region IRecordManagement

        public void UploadStandardAsync(EventHandler<SpecimenReceivedEventArgs> handler, int @from = 0, int count = -1)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _uploadSpecimenAsyncContext = new UploadSpecimenAsyncContext();
            _uploadSpecimenAsyncContext.Handler = handler;

            Send(Commands.UploadStandards(from, count));
        }

        public void UploadTestAsync(EventHandler<SpecimenReceivedEventArgs> handler, int standard, int @from = 0,
            int count = -1)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _uploadSpecimenAsyncContext = new UploadSpecimenAsyncContext();
            _uploadSpecimenAsyncContext.Handler = handler;

            Send(Commands.UploadTrials(standard, from, count));
        }

        public void DeleteAllSpecimens()
        {
            DeleteStandard(-1);
        }

        public int TotalStandards
        {
            get { return GetStandardCount(); }
        }

        public int TotalSamples
        {
            get { return GetTestCount(-1); }
        }

        public int GetSamplesCount(int standardIndex)
        {
            throw new NotImplementedException();
        }

        public int GetSamplesCountById(int standardId)
        {
            return GetTestCount(standardId);
        }

        public StandardSpecimen UploadStandard(int standardIndex)
        {
            return UploadStandards(standardIndex, 1)[0];
        }

        public StandardSpecimen UploadStandardById(int standardId)
        {
            throw new NotImplementedException();
        }

        public StandardSpecimen[] UploadStandards(int startIndex, int count)
        {
            return Array.ConvertAll(GetStandards(startIndex, count), input => (StandardSpecimen)input);
        }

        public Specimen UploadSample(int standardIndex, int sampleIndex)
        {
            throw new NotImplementedException();
        }

        public Specimen UploadSampleById(int standardId, int sampleIndex)
        {
            return UploadSamplesById(standardId, sampleIndex, 1)[0];
        }

        public Specimen[] UploadSamples(int standardIndex, int startSampleIndex, int count)
        {
            throw new NotImplementedException();
        }

        public Specimen[] UploadSamplesById(int standardId, int startSampleIndex, int count)
        {
            return GetTests(standardId, startSampleIndex, count);
        }

        public void RemoveAllRecords()
        {
            DeleteStandard(-1);
        }

        public void RemoveAllSamples()
        {
            DeleteSampleById(-1, -1);
        }

        public void RemoveAllSamples(int standardIndex)
        {
            DeleteSample(standardIndex, -1);
        }

        public void RemoveAllSamplesById(int standardId)
        {
            DeleteSampleById(standardId, -1);
        }

        public void DeleteStandard(int standardIndex)
        {
            SendAndWait(Commands.DeleteStandard(standardIndex), standardIndex == -1 ? 60000 : 5000);
        }

        public void DeleteStandardById(int standardId)
        {
            SendAndWait(Commands.DeleteStandard((int)(standardId | 0x80000000)), standardId == -1 ? 60000 : 5000);
        }

        public void DeleteSample(int standardIndex, int sampleIndex)
        {
            throw new NotImplementedException();
        }

        public void DeleteSampleById(int standardId, int sampleIndex)
        {
            SendAndWait(Commands.DeleteSample(standardId, sampleIndex), standardId == -1 ? 60000 : 5000);
        }

        public void DownloadStandard(Specimen standard)
        {
            WriteStandard(standard);
        }

        public void DownloadStandards(IEnumerable<Specimen> standards)
        {
            WriteStandards(standards);
        }

        public void Backup(StreamWriter stream)
        {
            throw new NotImplementedException();
        }

        public void Restory(StreamReader stream)
        {
            throw new NotImplementedException();
        }

        #endregion IRecordManagement

        public void Connect()
        {
            Connect(null);
        }

        private void CreateSession()
        {
            _connectedEvent.Reset();
            _clientProtocol.Connect();

            _connectedEvent.WaitOne(10000);
            if (!_clientProtocol.IsConnected)
            {
                Trace.TraceWarning("建立会话失败");
                _clientProtocol.Close();
                throw new CommunicationException(CommunicationError.DeviceOpenFailed);
            }

            if (_clientProtocol.ProductType != (int)ProductType.CR && _clientProtocol.ProductType != (int)ProductType.JSON)
            {
                _clientProtocol.Close();
                throw new CommunicationException(CommunicationError.DeviceOpenFailed);
            }
        }

        public void Connect(object deviceAddress)
        {
            if (!_clientProtocol.Open(deviceAddress as string))
            {
                Trace.TraceWarning("打开设备失败");
                throw new CommunicationException(CommunicationError.DeviceOpenFailed);
            }

            CreateSession();
            DeviceName = _clientProtocol.CommDevice.DeviceName;
        }

        public void Disconnect()
        {
            _clientProtocol.Close();
        }

        public Specimen Measure(MeasurementType type, SpecularComponent? sc = null)
        {
            var result = SendAndWait(Commands.Measure(type, sc?.ToInstrumentForm()), 30000);
            Specimen specimen;
            if (type == MeasurementType.Standard)
            {
                var record = StructConverter.BytesToStruct<StandardRecord>(result.Data);
                specimen = record.ToSpecimen();
            }
            else
            {
                var record = StructConverter.BytesToStruct<TrialRecord>(result.Data);
                specimen = record.ToSpecimen();
            }
            specimen.Name = null;
            specimen.ID = 0;
            return specimen;
        }

        private void Calibrate(bool isBlack = true)
        {
            SendAndWait(Commands.Calibrate(isBlack), 10000);
        }

        public int GetStandardCount()
        {
            var result = SendAndWait(Commands.GetStandardCount());

            return BitConverter.ToInt32(result.Data, 0);
        }

        public int GetTestCount(int standard)
        {
            var result = SendAndWait(Commands.GetTrialCount(standard), standard < 0 ? 60000 : 5000);
            return BitConverter.ToInt32(result.Data, 0);
        }

        public Factors GetWhiteBoardFactors()
        {
            var result = SendAndWait(Commands.GetWhiteBoardFactors());
            return StructConverter.BytesToStruct<Factors>(result.Data);
        }

        public float GetFactors()
        {
            var result = SendAndWait(Commands.GetFactors());
            return BitConverter.ToSingle(result.Data, 0);
        }

        public void SetWhiteBoardFactors(Factors factors)
        {
            SendAndWait(Commands.SetWhiteBoardFactors(factors));
        }

        public void SetFactors(float factor)
        {
            SendAndWait(Commands.SetFactors(factor), 10000);
        }

        public MNRecord[] GetRecords(int start, int count)
        {
            var result = SendAndWait(Commands.UploadStandards(start, count), 60000);
            var list = new List<MNRecord>();
            int size = Marshal.SizeOf(typeof(StandardRecord));
            for (int i = 0; i * size < result.Data.Length; i++)
            {
                var record = StructConverter.BytesToStruct<StandardRecord>(result.Data, i * size);
                var length = Marshal.SizeOf(typeof(MNRecord));
                list.Add(record.Record);
            }

            return list.ToArray();
        }

        public Specimen[] GetStandards(int @from = 0, int count = -1)
        {
            var result = SendAndWait(Commands.UploadStandards(from, count), 60000);
            List<Specimen> list = new List<Specimen>();
            int size = Marshal.SizeOf(typeof(StandardRecord));
            for (int i = 0; i * size < result.Data.Length; i++)
            {
                var record = StructConverter.BytesToStruct<StandardRecord>(result.Data, i * size);
                list.Add(record.ToSpecimen());
            }

            return list.ToArray();
        }

        public Specimen[] GetTests(int standard, int @from = 0, int count = -1)
        {
            var result = SendAndWait(Commands.UploadTrials(standard, from, count), 60000);
            List<Specimen> list = new List<Specimen>();
            int size = Marshal.SizeOf(typeof(TrialRecord));
            for (int i = 0; i * size < result.Data.Length; i++)
            {
                var record = StructConverter.BytesToStruct<TrialRecord>(result.Data, i * size);
                list.Add(record.ToSpecimen());
            }

            return list.ToArray();
        }

        public void StopUpload()
        {
            throw new NotImplementedException();
        }

        public void DeleteTests(int standard)
        {
            DeleteSampleById(standard, -1);
        }

        public void WriteStandard(Specimen standard)
        {
            StandardRecord record = StandardRecord.FromSpecimen(standard);
            SendAndWait(Commands.DownloadStandards(new[] { record }));
        }

        public void WriteStandards(IEnumerable<Specimen> standards)
        {
            List<StandardRecord> standardRecords = new List<StandardRecord>();
            foreach (var specimen in standards)
            {
                standardRecords.Add(StandardRecord.FromSpecimen(specimen));
            }

            SendAndWait(Commands.DownloadStandards(standardRecords.ToArray()),
                Math.Max(DefaultTimeout, 5000 + standardRecords.Count * 200));
        }

        public void RefreshInstrumentInfo()
        {
            ProductInfo product = GetProductInfo();
            _InternalModelType = product.InternalModel;
            DeviceInfo info = GetDeviceInfo();

            Model = info.Model;
            SN = info.Sn;
            WhiteBoardNumber = info.WhiteBoardNumber;
            FirmwareRevision = info.SoftwareVersion;
            HardwareRevision = info.HardwareVersion;

            InstrumentStatus status = GetInstrumentStatus();
            _aperture = Model == "MCR10" ? MeasurementCaliber.Phi1 : (Model == "BTS20" ? MeasurementCaliber.Phi50 : status.Caliber);
            _specularComponent = status.ScMode;
            _lensPosition = status.LensPos;
            _colorSpace = status.ColorSpace;
            _colorDiffFormula = status.ColorDiffFormula;
            _colorIndex = status.ColorIndex;
            _uvFlag = status.UvMode;
            _lensSwitchMode = status.LensSwitchMode;
            _spectrumMeasurementMode = status.IsTransmission
                ? SpectrumMeasurementMode.Transmissive
                : SpectrumMeasurementMode.Reflective;
            _observer = status.Observer;
            _standardIlluminant = status.Illuminant;

            OpticalStructure = SpectrumMeasurementMode == SpectrumMeasurementMode.Transmissive ? "d/0" : "d/8";
        }

        public void SetDateTime(DateTime datetime)
        {
            SetRtcDateTime(datetime);
        }

        public void RestoreFactorySettings()
        {
            SendAndWait(Commands.Restore());
        }

        public void SetInstrumentLanguage(InstrumentLanguage lang)
        {
            throw new NotImplementedException();
        }

        public void SetProductInfo(ProductInfo info)
        {
            SendAndWait(Commands.SetProductEquipInfo(info));
        }

        private static byte[] ConvertToByteArray(string s, int arrLength)
        {
            if (s == null)
            {
                return new byte[arrLength];
            }

            var codes = Encoding.UTF8.GetBytes(s);
            if (codes.Length > arrLength)
            {
                throw new ArgumentException("The length of encoding is too long.");
            }
            var bytes = new byte[arrLength];
            Array.Copy(codes, bytes, Math.Min(codes.Length, bytes.Length));
            return bytes;
        }

        private static byte[] FloatArrayToBytes(float[] values)
        {
            List<byte> list = new List<byte>();
            foreach (var f in values)
            {
                list.AddRange(BitConverter.GetBytes(f));
            }

            return list.ToArray();
        }

        private static byte[] UshortArrayToBytes(ushort[] values)
        {
            List<byte> list = new List<byte>();
            foreach (var f in values)
            {
                list.AddRange(BitConverter.GetBytes(f));
            }

            return list.ToArray();
        }

        /// <summary>
        /// 设置指定出厂参数字段
        /// </summary>
        /// <param name="field">要设置的字段</param>
        /// <param name="value">值，与相应的ProductInfo的字段类型相同，
        /// 但对于CAMERA_CENTER是长度为2的ushort数组，对UPDATE为bool型，其他与ProductInfo类型一样
        /// </param>
        /// <remarks>
        /// 注意：设置完成后要存储需要调用SetProductInfo(ProductInfoField.UPDATE, true)，如果要取消设置则
        /// 调用SetProductInfo(ProductInfoField.UPDATE, false)。
        /// 但是对于ALL，设置后会立即存储。
        /// </remarks>
        public void SetProductInfo(ProductInfoField field, Object value)
        {
            if (field == ProductInfoField.ALL)
            {
                SetProductInfo((ProductInfo)value);
                return;
            }

            byte[] bytes = null;
            switch (field)
            {
                case ProductInfoField.INTERNAL_MODEL:
                    //InternalModelType modelType = (InternalModelType)value;
                    bytes = new byte[] { (byte)value };
                    break;

                case ProductInfoField.DEFAULT_LANGUAGE:
                    var language = (ColorReader.Language)value;
                    bytes = new byte[] { (byte)language };
                    break;

                case ProductInfoField.DEVICE_MODEL_NAME:
                    bytes = ConvertToByteArray((string)value, 16);
                    break;

                case ProductInfoField.SOFTWARE_VERSION:
                    bytes = ConvertToByteArray((string)value, 16);
                    break;

                case ProductInfoField.HARDWARE_VERSION:
                    bytes = ConvertToByteArray((string)value, 16);
                    break;

                case ProductInfoField.SN:
                    bytes = ConvertToByteArray((string)value, 16);
                    break;

                case ProductInfoField.BLACK_PLATE:
                    bytes = StructConverter.StructToBytes((CalibrationBoard)value);
                    break;

                case ProductInfoField.WHITE_PLATE:
                    bytes = StructConverter.StructToBytes((CalibrationBoard)value);
                    break;

                case ProductInfoField.SPECTROGRAPH:
                    bytes = StructConverter.StructToBytes((SpectrographParameter)value);
                    break;

                case ProductInfoField.UPDATE:
                    bytes = new[] { (bool)value ? (byte)1 : (byte)0 };
                    break;

                case ProductInfoField.REFLECT_MODIFY:
                    bytes = FloatArrayToBytes((float[])value);
                    break;

                case ProductInfoField.CAMERA_CENTER:
                    bytes = UshortArrayToBytes((ushort[])value);
                    break;

                case ProductInfoField.MOTOR_TIME:
                    bytes = BitConverter.GetBytes((ushort)value);
                    break;

                case ProductInfoField.THRESHOLD_8MM:
                case ProductInfoField.THRESHOLD_4MM:
                    bytes = BitConverter.GetBytes((float)value);
                    break;

                case ProductInfoField.CUSTOMIZE_CALIBER:
                    var caliber = (Caliber)value;
                    bytes = new[] { (byte)caliber };
                    break;

                case ProductInfoField.SENSOR_TYPE:
                    var sensorType = (SensorType)value;
                    bytes = new[] { (byte)sensorType };
                    break;

                case ProductInfoField.IS_DEMONSTRATOR:
                    var isDemonstrator = (bool)value;
                    bytes = new[] { isDemonstrator ? (byte)1 : (byte)0 };
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }

            SendAndWait(Commands.SetProductInfo(field, bytes));
        }

        public object GetProductInfo(ProductInfoField field)
        {
            if (field == ProductInfoField.ALL)
            {
                return GetProductInfo();
            }
            var result = SendAndWait(Commands.GetProductInfo(field));
            switch (field)
            {
                case ProductInfoField.INTERNAL_MODEL:
                    return (InternalModelType)result.Data[0];

                case ProductInfoField.DEFAULT_LANGUAGE:
                    return (ColorReader.Language)result.Data[0];

                case ProductInfoField.DEVICE_MODEL_NAME:
                case ProductInfoField.SOFTWARE_VERSION:
                case ProductInfoField.HARDWARE_VERSION:
                case ProductInfoField.SN:
                    return ByteArrayToString(result.Data);

                case ProductInfoField.BLACK_PLATE:
                case ProductInfoField.WHITE_PLATE:
                    return StructConverter.BytesToStruct<CalibrationBoard>(result.Data);

                case ProductInfoField.SPECTROGRAPH:
                    return StructConverter.BytesToStruct<SpectrographParameter>(result.Data);
                //case ProductInfoField.UPDATE:
                //    break;
                case ProductInfoField.REFLECT_MODIFY:
                    return ByteArrayToFloats(result.Data);

                case ProductInfoField.CAMERA_CENTER:
                    return new ushort[] { BitConverter.ToUInt16(result.Data, 0), BitConverter.ToUInt16(result.Data, 2) };

                case ProductInfoField.MOTOR_TIME:
                    return BitConverter.ToUInt16(result.Data, 0);

                case ProductInfoField.THRESHOLD_8MM:
                    return BitConverter.ToSingle(result.Data, 0);

                case ProductInfoField.THRESHOLD_4MM:
                    return BitConverter.ToSingle(result.Data, 0);
                //case ProductInfoField.ALL:
                //    return StructConverter.BytesToStruct<ProductInfo>(result.Data);
                case ProductInfoField.CUSTOMIZE_CALIBER:
                    return (Caliber)result.Data[0];

                case ProductInfoField.SENSOR_TYPE:
                    return (SensorType)result.Data[0];

                case ProductInfoField.IS_DEMONSTRATOR:
                    return result.Data[0] != 0;

                default:
                    throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }
        }

        private float[] ByteArrayToFloats(byte[] bytes)
        {
            if (bytes == null) return null;
            var list = new List<float>();
            for (int index = 0; index < bytes.Length; index += 4)
            {
                list.Add(BitConverter.ToSingle(bytes, index));
            }

            return list.ToArray();
        }

        private string ByteArrayToString(byte[] bytes)
        {
            if (bytes == null)
                return null;

            int index = Array.IndexOf(bytes, 0);
            if (index == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(bytes, 0, index < 0 ? bytes.Length : index);
        }

        /// <summary>
        /// 获取定制型号名
        /// </summary>
        public string GetCustomizedModelName()
        {
            var result = SendAndWait(Commands.GetProductInfo(ProductInfoField.DEVICE_MODEL_NAME));
            if (result.Data.Length > 0)
            {
                int count = 0;
                while (result.Data[count++] != 0)
                {
                    // do nothing
                }
                return Encoding.ASCII.GetString(result.Data, 0, count - 1);
            }

            return null;
        }

        public void SetCustomizedModelName(string modelName)
        {
            var bytes = new byte[Constants.DEVICE_MODEL_NAME_LENGTH];
            if (!string.IsNullOrWhiteSpace(modelName))
            {
                var codes = Encoding.UTF8.GetBytes(modelName);
                if (codes.Length > Constants.DEVICE_MODEL_NAME_LENGTH)
                {
                    throw new CommunicationException(CommunicationError.InvalidParameter,
                        $"The model name must be less than or equal {Constants.DEVICE_MODEL_NAME_LENGTH}");
                }

                for (int i = 0; i < codes.Length; i++)
                {
                    bytes[i] = codes[i];
                }
            }

            SendAndWait(Commands.SetProductInfo(ProductInfoField.DEVICE_MODEL_NAME, bytes));
        }

        public void DcMotorReset()
        {
            SendAndWait(Commands.DcMotorReset());
        }

        /// <summary>
        /// 设置DC马达持续时间
        /// </summary>
        /// <param name="time">持续时间</param>
        public void DcMotorTimeSet(UInt16 time)
        {
            SendAndWait(Commands.DcMotorTimeSet(time));
        }

        public UInt16 DcMotorTimeGet()
        {
            var result = SendAndWait(Commands.DcMotorTimeGet());
            if (result?.Data != null)
            {
                return BitConverter.ToUInt16(result.Data, 0);
            }
            throw new CommunicationException(CommunicationError.ParsedBadData);
        }

        /// <summary>
        /// DC马达测试
        /// </summary>
        /// <param name="scMode">测光方式</param>
        /// <param name="time">持续时间，如果为0则使用系统默认配置</param>
        public void DCMotorTest(ScMode scMode, UInt16 time = 0)
        {
            SendAndWait(Commands.DCMotorTest(scMode, time), (int)(5000 + time));
        }

        /// <summary>
        /// 测试LED
        /// </summary>
        /// <param name="ledIndex">要测试LED</param>
        /// <param name="on">如果为真点亮灯，否则关掉灯</param>
        public void LedTest(LedIndex ledIndex, bool on)
        {
            SendAndWait(Commands.LedTest(ledIndex, on));
        }

        ///// <summary>
        ///// 相机测试
        ///// </summary>
        ///// <param name="on">如果为true打开相机，否则关掉相机</param>
        //public void CameraTest(bool on)
        //{
        //    SendAndWait(Commands.CameraTest(on));
        //}

        /// <summary>
        /// 打开相机
        /// </summary>
        public void OpenCamera()
        {
            SendAndWait(Commands.CameraTest(CameraTestCode.Open));
        }

        /// <summary>
        /// 关闭相机
        /// </summary>
        public void CloseCamera()
        {
            SendAndWait(Commands.CameraTest(CameraTestCode.Close));
        }

        public void EnableCameraConfig()
        {
            SendAndWait(Commands.CameraTest(CameraTestCode.EnableConfig));
        }

        public void DisableCameraConfig()
        {
            SendAndWait(Commands.CameraTest(CameraTestCode.DisableConfig));
        }

        public void SaveCameraConfig()
        {
            SendAndWait(Commands.CameraTest(CameraTestCode.SaveConfig));
        }

        /// <summary>
        /// 获取相机影像
        /// </summary>
        /// <returns></returns>
        public byte[] GetCameraImage()
        {
            var bytes = SendAndWait(Commands.GetCameraImage(), 0xFFFF).Data;
            //if (bytes.Length != 240 * 240 * 2)
            //    throw new CommunicationException(CommunicationError.ParsedBadData);
            return bytes;
            //// 旋转90度以与仪器端一致
            //var buffer = new byte[bytes.Length];
            //for (int row = 0; row < 240; row++)
            //{
            //    for (int col = 0; col < 240; col++)
            //    {
            //        int sourceIndex = (row * 240 + col) * 2;
            //        int targetIndex = (col * 240 + (239-row)) * 2;
            //        buffer[targetIndex] = bytes[sourceIndex];
            //        buffer[targetIndex + 1] = bytes[sourceIndex + 1];
            //    }
            //}

            //return buffer;
        }

        /// <summary>
        /// 获取相机中心点设置
        /// </summary>
        /// <returns></returns>
        public ushort[] GetCameraCenter()
        {
            var result = SendAndWait(Commands.GetProductInfo(ProductInfoField.CAMERA_CENTER));
            try
            {
                return new ushort[]
                {
                    BitConverter.ToUInt16(result.Data, 0),
                    BitConverter.ToUInt16(result.Data, 2)
                };
            }
            catch (Exception e)
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void SetCameraCenter(ushort x, ushort y)
        {
            var parameters = BitConverter.GetBytes(x).Concat(BitConverter.GetBytes(y));
            SendAndWait(Commands.SetProductInfo(ProductInfoField.CAMERA_CENTER, parameters.ToArray())); // 设置位置
            SendAndWait(Commands.SetProductInfo(ProductInfoField.UPDATE, new byte[] { 1 })); // 存储
        }

        /// <summary>
        /// 检测Flash
        /// </summary>
        public bool FlashTest()
        {
            try
            {
                SendAndWait(Commands.FlashTest(), timeout: 30000);
            }
            catch (CommunicationException ex)
            {
                if (ex.ErrorCode == CommunicationError.ExecuteCommandFailed)
                {
                    return false;
                }

                throw;
            }

            return true;
        }

        /// <summary>
        /// 检测外部RAM
        /// </summary>
        public bool SdramTest()
        {
            try
            {
                SendAndWait(Commands.SdramTest(), timeout: 30000);
            }
            catch (CommunicationException ex)
            {
                if (ex.ErrorCode == CommunicationError.ExecuteCommandFailed)
                {
                    return false;
                }

                throw;
            }

            return true;
        }

        /// <summary>
        /// 检测蜂鸣器
        /// </summary>
        public void BuzzerTest()
        {
            SendAndWait(Commands.BuzzerTest(), 15000);
        }

        /// <summary>
        /// 测试显示屏
        /// </summary>
        public void LcdTest()
        {
            SendAndWait(Commands.LcdTest(), timeout: 10000);
        }

        /// <summary>
        /// 测试触屏，显示指定数格子
        /// </summary>
        /// <param name="rowCount">格子行数</param>
        /// <param name="colCount">格子列数</param>
        public void TouchScreenTest(ushort rowCount, ushort colCount)
        {
            SendAndWait(Commands.TouchScreenTest(rowCount, colCount));
        }

        /// <summary>
        /// 获取RTC时间
        /// </summary>
        /// <returns>返回获取的时间</returns>
        /// <exception cref="CommunicationException"></exception>
        public DateTime GetRtcDateTime()
        {
            var data = SendAndWait(Commands.GetRtcDateTime());
            var datetime = StructConverter.BytesToStruct<RtcDateTime>(data.Data);
            return new DateTime(datetime.year, datetime.month, datetime.date,
                datetime.hour, datetime.minute, datetime.second);
        }

        /// <summary>
        /// 设置RTC时间
        /// </summary>
        /// <param name="dateTime">要设置的日期</param>
        public void SetRtcDateTime(DateTime dateTime)
        {
            SendAndWait(Commands.SetRtcDateTime(dateTime));
        }

        /// <summary>
        /// 测试打印机
        /// </summary>
        public void TestPrinter()
        {
            SendAndWait(Commands.TestPrinter());
        }

        public DeviceInfo GetDeviceInfo()
        {
            var cmd = Commands.GetDeviceInfo();
            Send(cmd);
            var result = Wait(cmd.Command, 5000);
            if (result == null)
            {
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }

            if (result.Data == null)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            return StructConverter.BytesToStruct<DeviceInfo>(result.Data);
        }

        public InstrumentStatus GetInstrumentStatus()
        {
            var cmd = Commands.GetInstrumentStatus();
            Send(cmd);
            var result = Wait(cmd.Command, 5000);
            if (result == null)
            {
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }

            if (result.Data == null)
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }

            return StructConverter.BytesToStruct<InstrumentStatus>(result.Data);
        }

        public void Send(CommandPacket packet)
        {
            if (!_mutex.WaitOne(0))
            {
                throw new CommunicationException(CommunicationError.DeviceBusy);
            }

            try
            {
                if (!_clientProtocol.IsConnected)
                {
                    throw new CommunicationException(CommunicationError.DeviceDisconnected);
                }

                _recvBuffer.Clear();
                _ackEvent.Reset();
                _commError = 0;
                _currentCommand = 0;
                Debug.WriteLine((byte)packet.Command);
                if (packet.Data != null && packet.Data.Length > 0)
                {
                    Debug.WriteLine(string.Join(" ", packet.Data.Select(b => b.ToString("X2"))));
                }
                if (!_clientProtocol.Send((byte)packet.Command, null, packet.Data))
                {
                    throw new CommunicationException(CommunicationError.DeviceBusy);
                }
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public CommandPacket Wait(byte command = 0, int timeout = Int32.MaxValue)
        {
            _waitingCommand = (Command)command;
            while (_ackEvent.WaitOne(timeout) && _clientProtocol.IsConnected)
            {
                _mutex.WaitOne();
                try
                {
                    if (command == 0 || command == _currentCommand)
                    {
                        var result = new CommandPacket(_currentCommand);
                        result.AckCode = _commError;
                        lock (_recvBuffer)
                        {
                            if (_commError == 0 && _recvBuffer.Count > 0)
                            {
                                result.Data = _recvBuffer.ToArray();
                                _recvBuffer.Clear();
                            }
                        }

                        return result;
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                    _waitingCommand = Command.InvalidCommand;
                }
            }

            // TODO：暂时这样写，这里应该有更好的方法解决写入参数超时的问题（ztd:20251013）
            if ((Command)command == Command.GetKmtcMode && _clientProtocol.IsConnected)
            {
                _mutex.WaitOne();
                try
                {
                    if (command == 0 || command == _currentCommand)
                    {
                        var result = new CommandPacket(_currentCommand);
                        result.AckCode = _commError;
                        lock (_recvBuffer)
                        {
                            if (_commError == 0 && _recvBuffer.Count > 0)
                            {
                                result.Data = _recvBuffer.ToArray();
                                _recvBuffer.Clear();
                            }
                        }

                        return result;
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                    _waitingCommand = Command.InvalidCommand;
                }
            }

            _currentCommand = 0;
            _waitingCommand = Command.InvalidCommand;

            return null;
        }

        private ProgressChangedEventHandler _sendingProgressChanged;

        /// <summary>
        /// 写入资源文件
        /// </summary>
        /// <param name="stream">要发送的资源</param>
        /// <param name="handler">进度改变处理函数；如果发送过程出错UserState会传入Exception对像</param>
        public void WriteResource(Stream stream, ProgressChangedEventHandler handler = null)
        {
            if (!_clientProtocol.IsConnected)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            _sendingProgressChanged = handler;
            _clientProtocol.SendResource((uint)stream.Length, stream);
        }

        /// <summary>
        /// 写入LOGO文件
        /// </summary>
        /// <param name="stream">要发送的LOGO</param>
        /// <param name="handler">进度改变处理函数；如果发送过程出错UserState会传入Exception对像</param>
        public void WriteLogo(Stream stream, ProgressChangedEventHandler handler = null)
        {
            if (!_clientProtocol.IsConnected)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            _sendingProgressChanged = handler;
            _clientProtocol.SendLogo((uint)stream.Length, stream);
        }

        /// <summary>
        /// 生成校准的AD限制
        /// </summary>
        /// <param name="isTransmission">是否是透射模式</param>
        /// <param name="isWhite">是否是白校准</param>
        /// <returns></returns>
        public void GenerateAdLimit(Caliber caliber)
        {
            if (caliber != Caliber.Phi4 && caliber != Caliber.Phi8)
            {
                throw new CommunicationException(CommunicationError.InvalidParameter);
            }
            // 不等待结果
            SendAndWait(Commands.GenerateAdLimit(caliber), timeout: 60000);
        }

        /// <summary>
        /// 获取ADC采样值
        /// </summary>
        /// <remarks>
        /// 该函数只采样，不会负责点亮、关灯、转动马达
        /// </remarks>
        public SpectraAdcSampleData GetSpectraAdcSampleData()
        {
            var result = SendAndWait(Commands.GetAdData(), 10000);
            try
            {
                return StructConverter.BytesToStruct<SpectraAdcSampleData>(result.Data);
            }
            catch (Exception e)
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        /// <summary>
        /// 获取内部型号
        /// </summary>
        /// <returns>内部型号</returns>
        public InternalModelType GetInternalModel()
        {
            var result = SendAndWait(Commands.GetInternalModel());
            return (InternalModelType)result.Data[0];
        }

        public byte GetKmtcMode()
        {
            var result = SendAndWait(Commands.GetKmtcMode());
            return result.Data[0];
        }

        public StandardNames GetStandardNames()
        {
            var result = SendAndWait(Commands.GetStandardNames());
            return StructConverter.BytesToStruct<StandardNames>(result.Data);
        }

        public void SetStandardNames(StandardNames standardNames)
        {
            SendAndWait(Commands.SetStandardNames(standardNames));
        }

        public ToleranceCIE1976 GetCIE1976Tolerance()
        {
            var result = SendAndWait(Commands.GetCIE1976Tolerance());
            return StructConverter.BytesToStruct<ToleranceCIE1976>(result.Data);
        }

        public void SetCIE1976Tolerance(ToleranceCIE1976 tolerance)
        {
            SendAndWait(Commands.SetCIE1976Tolerance(tolerance));
        }

        public List<PharmaceuticalConfig> ReadPharmaceuticalConfig()
        {
            var result = SendAndWait(Commands.ReadPharmaceuticalConfig());
            var length = Marshal.SizeOf(typeof(PharmaceuticalConfig));
            var configs = new List<PharmaceuticalConfig>();
            for (int i = 0; i < result.Data.Length; i += length)
            {
                var data = new byte[length];
                Array.Copy(result.Data, i, data, 0, length);
                var config = StructConverter.BytesToStruct<PharmaceuticalConfig>(data);
                configs.Add(config);
            }
            return configs;
        }

        public void WritePharmaceuticalConfig(List<PharmaceuticalConfig> configs)
        {
            SendAndWait(Commands.WritePharmaceuticalConfig(configs));
        }

        public PharmaceuticalInfo ReadPharmaceuticalInfo()
        {
            var result = SendAndWait(Commands.ReadPharmaceuticalInfo());
            return StructConverter.BytesToStruct<PharmaceuticalInfo>(result.Data);
        }

        public void WritePharmaceuticalInfo(PharmaceuticalInfo info)
        {
            SendAndWait(Commands.WritePharmaceuticalInfo(info));
        }

        public ProductInfo GetProductInfo()
        {
            var result = SendAndWait(Commands.GetProductInfo(ProductInfoField.ALL));
            if (result.Data.Length == Marshal.SizeOf(typeof(ProductInfo)))
            {
                return StructConverter.BytesToStruct<ProductInfo>(result.Data);
            }
            else if (result.Data.Length == Marshal.SizeOf(typeof(ProductInfoV0)))
            {
                return new ProductInfo(StructConverter.BytesToStruct<ProductInfoV0>(result.Data));
            }

            throw new CommunicationException(CommunicationError.ParsedBadData);
        }

        //public float GetTolerance()
        //{
        //    var result = SendAndWait(Commands.GetTolerance());
        //    return BitConverter.ToSingle(result.Data,0);
        //}

        /// <summary>
        /// 设置内部型号
        /// </summary>
        /// <param name="modelType">仪器内部型号</param>
        public void SetInternalModel(byte modelType)
        {
            SendAndWait(Commands.SetInternalModel(modelType));
        }

        /// <summary>
        /// 获取仪器的CPU ID
        /// </summary>
        /// <returns>CPU编号</returns>
        public byte[] GetCpuId()
        {
            var result = SendAndWait(Commands.GetCpuId());
            return result.Data;
        }

        /// <summary>
        /// 设置数据加密密钥
        /// </summary>
        /// <param name="encryption">加密密钥</param>
        public void SetEncryption(ushort encryption)
        {
            SendAndWait(Commands.SetEncryption(encryption));
        }

        public void SetKmtcMode(byte kmtcMode)
        {
            SendAndWait(Commands.SetKmtcMode(kmtcMode));
        }

        /// <summary>
        /// 读取仪器认样文件中的试样
        /// </summary>
        /// <param name="fileName">试样文件名</param>
        /// <returns>读取到的试样数据</returns>
        public static Specimen[] PariseTestSpecimenFile(string fileName)
        {
            var stream = File.OpenRead(fileName);
            return PariseTestSpecimenFile(stream);
        }

        /// <summary>
        /// 读取仪器认样文件中的试样
        /// </summary>
        /// <param name="stream">已打开的试样数据文件流</param>
        /// <returns>读取到的试样数据</returns>
        public static Specimen[] PariseTestSpecimenFile(Stream stream)
        {
            var recordSize = Marshal.SizeOf(typeof(TrialRecord));
            Specimen[] specimens = new Specimen[stream.Length / recordSize];
            byte[] buffer = new byte[recordSize];

            int index = 0;
            while (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
            {
                specimens[index++] = StructConverter.BytesToStruct<TrialRecord>(buffer).ToSpecimen();
            }

            return specimens;
        }

        public CalibrationBoard GetWhiteBoardParameter()
        {
            var result = SendAndWait(Commands.GetWhiteBoardParameter());
            try
            {
                return StructConverter.BytesToStruct<CalibrationBoard>(result.Data);
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void SetWhiteBoardParameter(CalibrationBoard parameter)
        {
            SendAndWait(Commands.SetWhiteBoardParameter(parameter));
        }

        public SpectrographParameter GetWavelengthCoef()
        {
            var result = SendAndWait(Commands.GetProductInfo(ProductInfoField.SPECTROGRAPH));
            try
            {
                return StructConverter.BytesToStruct<SpectrographParameter>(result.Data);
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void SetWavelengthCoef(SpectrographParameter coef)
        {
            SendAndWait(Commands.SetProductInfo(ProductInfoField.SPECTROGRAPH, StructConverter.StructToBytes(coef)));
        }

        public SpectrumMeasurementMode GetSpectrumMeasurementMode()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.SpectrumMeasurementMode));
            return result.Data[0] == 0 ? SpectrumMeasurementMode.Reflective : SpectrumMeasurementMode.Transmissive;
        }

        public void SetSpectrumMeasurementMode(SpectrumMeasurementMode mode)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.SpectrumMeasurementMode,
                (byte)(mode == SpectrumMeasurementMode.Reflective ? 0 : 1)));
        }

        public SpecularComponent GetSpecularComponent()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.SpecularComponent));
            return ((ScMode)result.Data[0]).Uniform();
        }

        public void SetSpecularComponent(SpecularComponent sc)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.SpecularComponent, (byte)sc.ToInstrumentForm()));
        }

        public LensSwitchMode GetLensSwitchMode()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.LensSwitchMode));
            return ((LensMode)result.Data[0]).Uniform();
        }

        public void SetLensSwitchMode(LensSwitchMode mode)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.LensSwitchMode, (byte)mode.ToInstrumentForm()));
        }

        public LensPosition GetLensPosition()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.LensPosition));
            return ((LensPos)result.Data[0]).Uniform();
        }

        public void SetLensPosition(LensPosition position)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.LensPosition,
                (byte)position.ToInstrumentForm()));
        }

        public MeasurementCaliber GetMeasurementCaliber()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.MeasurementAperture));
            return ((Caliber)result.Data[0]).Uniform();
        }

        public void SetMeasurementCaliber(MeasurementCaliber caliber)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.MeasurementAperture, (byte)caliber.ToInstrumentForm()));
        }

        public UVFlag GetUvMode()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.UvMode));
            return ((UvMode)result.Data[0]).Uniform();
        }

        public void SetUvMode(UVFlag uv)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.UvMode, (byte)uv.ToInstrumentForm()));
        }

        public StandardIlluminant GetStandardIlluminant()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.StandardIlluminant));
            return ((Illuminant)result.Data[0]).Uniform();
        }

        public void SetStandardIlluminant(StandardIlluminant illuminant)
        {
            var result = SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.StandardIlluminant, (byte)illuminant.ToInstrumentForm()));
        }

        public StandardObserver GetStandardObserver()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.StandardObserver));
            return ((Observer)result.Data[0]).Uniform();
        }

        public void SetStandardObserver(StandardObserver observer)
        {
            var result = SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.StandardObserver, (byte)observer.ToInstrumentForm()));
        }

        public Optical.ColorSpace GetColorSpace()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.ColorSpace));
            return ((ColorSpace)result.Data[0]).Uniform();
        }

        public void SetColorSpace(Optical.ColorSpace colorSpace)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorSpace,
                (byte)colorSpace.ToInstrumentForm()));
        }

        public Optical.ColorDiffFormula GetColorDiffFormula()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.ColorDiffFormula));
            return ((ColorDeltaFormula)result.Data[0]).Uniform();
        }

        public void SetColorDiffFormula(Optical.ColorDiffFormula colorDiffFormula)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorDiffFormula,
                (byte)colorDiffFormula.ToInstrumentForm()));
        }

        public void SetColorIndex(Optical.ColorIndex colorIndex)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorIndex,
                (byte)colorIndex.ToInstrumentForm()));
        }

        public Optical.ColorIndex GetColorIndex()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.ColorIndex));
            return ((ColorIndex)result.Data[0]).Uniform();
        }

        public StandardSpecimen GetCurrentStandard()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.CurrentStandard));
            try
            {
                var standard = StructConverter.BytesToStruct<StandardRecord>(result.Data);
                return standard.ToSpecimen();
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void SetCurrentStandard(StandardSpecimen standard)
        {
            var record = StandardRecord.FromSpecimen(standard);
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.CurrentStandard,
                StructConverter.StructToBytes(record)));
        }

        public Tolerance GetTolerance()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.Tolerance));
            try
            {
                return StructConverter.BytesToStruct<Tolerance>(result.Data);
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void SetTolerance(Tolerance tolerance)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.Tolerance,
                StructConverter.StructToBytes(tolerance)));
        }

        public bool IsXeLamp
        {
            get
            {
                var internalModel = GetInternalModel();
                return false;//internalModel == InternalModelType.ColorReader7020 ||//20191017lilin change to comment
                             //internalModel == InternalModelType.ColorReader7080;
            }
        }

        public bool FaultCheck(FaultMode faultMode)
        {
            var packet = Commands.FaultCheck(faultMode);
            Send(packet);
            var result = Wait(packet.Command, 10000);
            return result.AckCode == (int)RemoteError.NoError;
        }

        public InstrumentLanguage GetLanguage()
        {
            var result = SendAndWait(Commands.GetProductInfo(ProductInfoField.DEFAULT_LANGUAGE));
            if (result.Data?.Length == 1)
            {
                return ((ColorReader.Language)result.Data[0]).Uniform();
            }
            throw new CommunicationException(CommunicationError.ParsedBadData);
        }

        public void SetLanguage(InstrumentLanguage language)
        {
            SendAndWait(Commands.SetProductInfo(ProductInfoField.DEFAULT_LANGUAGE, new[] { (byte)language.ToInstrument() }));
        }

        /// <summary>
        /// 设置蓝牙名称
        /// </summary>
        /// <param name="name">蓝牙名称，转换成UTF8编码后长度不能超过 Constants.BLE_NAME_LENGTH</param>
        public void SetBleName(string name)
        {
            SendAndWait(Commands.BleSetName(name));
        }

        /// <summary>
        /// 开关蓝牙
        /// </summary>
        /// <param name="on">true开启，false关闭</param>
        public void SwitchBle(bool on)
        {
            SendAndWait(Commands.BleSwitch(on));
        }

        /// <summary>
        /// 给蓝牙发送字符串
        /// </summary>
        /// <param name="str">要发送的字符串</param>
        public void SendStringToBle(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            SendAndWait(Commands.BleSendData(Encoding.UTF8.GetBytes(str)));
        }

        public bool IsBleEnabled()
        {
            var result = SendAndWait(Commands.GetSystemCfg(SystemCfgField.BLE));
            return result.Data[0] != 0 ? true : false;
        }

        /// <summary>
        /// 进入boot模式，准备升级固件
        /// </summary>
        public void EnterBootloader()
        {
            var enterbootcmd= new CommandPacket((byte)Command.EnterBoot);
            SendAndWait(enterbootcmd);
        }

        #region SVD Function
        public List<float> GetMeasureSpectrumEight(int numbers = 8)
        {
            PacketContent content = new PacketContent();
            List<float> list = new List<float>();
            var result = SendAndWait(Commands.ReadSpectrumEight());
            if (result.Data.Length > 0)
            {
                var packet = new Packer(result.Data);
                while (packet.Dequeue(out float value))
                {
                    list.Add(value);
                }
                Console.WriteLine(string.Join(",", list));
                return list;
            }
            return null;
        }

        /// <summary>
        /// 读取算法参数矩阵
        /// </summary>
        /// <returns></returns>
        public List<float> ReadSVDList(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            List<float> list = new List<float>();
            for (int i = 0; i < cols; i++)
            {
                var result = SendAndWait(Commands.ReadSVD(Convert.ToByte(i)));

                if (null != result.Data &&  null != result.Data &&result.Data.Length > 0)
                {
                    var packet = new Packer(result.Data);
                    while (packet.Dequeue(out float value))
                    {
                        list.Add(value);
                    }
                    Debug.WriteLine(string.Join(",", list));
                }
                if (result.AckCode != 0)
                {
                    Trace.TraceInformation($"执行失败，收到错误码：{(RemoteError)result.AckCode}");
                    throw new CommunicationException(((RemoteError)result.AckCode).ToCommError());
                }

                Thread.Sleep(200);
            }

            return list;
        }
        public List<List<float>> ReadSVDs(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 写入算法参数矩阵
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public bool SetSVD(List<List<float>> datas, MeasureType? measureType = null, byte caliber = 0)
        {
            if (datas == null) return false;
            var rows = datas.Count;
            var cols = datas[0].Count;
            
            for (int i = 0; i < rows; i++)
            {
                PacketContent packet = new PacketContent();
                byte packageNum = (byte)i;
                packet.Enqueue(packageNum);
                for (int j = 0; j < cols; j++)
                {
                    packet.Enqueue(datas[i][j], AlgoParameterType.Quadratic);
                }
                var result = SendAndWait(Commands.WriteSVD(packet.Bytes), 200);
                if (result.AckCode != 0)
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("SVD写入失败：{0}", result.AckCode));
                }
                Thread.Sleep(200);
            }
            return true;
        }

        public bool SetOLSR(List<List<float>> datas)
        {
            if (datas == null) return false;
            
            List<float> dataLists = datas.SelectMany(o => o).ToList();
            int dataCountHalf = dataLists.Count / 2;

            // 分两次下发
            for (int i = 0; i < 2; i++)
            {
                PacketContent packet = new PacketContent();
                byte packageNum = (byte)i;
                packet.Enqueue(packageNum);

                for (int j = i * dataCountHalf; j < (i+ 1) * dataCountHalf; j++)
                {
                    packet.Enqueue(dataLists[j], AlgoParameterType.Quadratic);
                }

                var result = SendAndWait(Commands.WriteSVD(packet.Bytes), 200);

                if (result.AckCode != 0)
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("SVD写入失败：{0}", result.AckCode));
                }
                Thread.Sleep(200);
            }

            return true;
        }

        public List<float> ReadOLSRList(MeasureType? measureType = null)
        {
            List<float> list = new List<float>();
            for (int i = 0; i < 2; i++)
            {
                var result = SendAndWait(Commands.ReadSVD(Convert.ToByte(i)));

                if (null != result.Data && null != result.Data && result.Data.Length > 0)
                {
                    var packet = new Packer(result.Data);
                    while (packet.Dequeue(out float value))
                    {
                        list.Add(value);
                    }
                    Debug.WriteLine(string.Join(",", list));
                }
                if (result.AckCode != 0)
                {
                    Trace.TraceInformation($"执行失败，收到错误码：{(RemoteError)result.AckCode}");
                    throw new CommunicationException(((RemoteError)result.AckCode).ToCommError());
                }

                Thread.Sleep(200);
            }

            return list;
        }
        #endregion

        #region Optional Settings //Added by Liguanghai 20/03/27

        public IEnumerable<ValueDescription> OptionalMeasurementCalibers
        {
            get
            {
                List<MeasurementCaliber> list = new List<MeasurementCaliber>();

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalLensPositions
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<ValueDescription> OptionalLensSwitchModes
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<ValueDescription> OptionalSpecularComponents
        {
            get
            {
                List<SpecularComponent> list = new List<SpecularComponent>();
                list.Add(SpecularComponent.SCI);
                list.Add(SpecularComponent.SCE);
                list.Add(SpecularComponent.SCIAndSCE);
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalSpectrumMeasurementModes
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<ValueDescription> OptionalUVFlags
        {
            get
            {
                List<UVFlag> list = new List<UVFlag>();
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalStandardIlluminants
        {
            get
            {
                List<StandardIlluminant> list = new List<StandardIlluminant>();
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalStandardObservers
        {
            get
            {
                List<StandardObserver> list = new List<StandardObserver>();
                list.Add(StandardObserver.CIE1931);
                list.Add(StandardObserver.CIE1964);
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorSpaces
        {
            get
            {
                List<Optical.ColorSpace> list = new List<Optical.ColorSpace>();
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorDiffFormulas
        {
            get
            {
                List<ColorDiffFormula> list = new List<ColorDiffFormula>();
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorIndexs
        {
            get
            {
                List<Optical.ColorIndex> list = new List<Optical.ColorIndex>();
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public bool IsLensSwitchModeModifiable
        {
            get { return true; }
        }

        public bool IsLensPositionModifiable
        {
            get { return true; }
        }

        public bool IsMeasurementCaliberModifiable
        {
            get { return true; }
        }

        public bool YTContain400 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion Optional Settings //Added by Liguanghai 20/03/27

        public event EventHandler<MeasurementNotificationArgs> MeasurementReceived;
    }
}