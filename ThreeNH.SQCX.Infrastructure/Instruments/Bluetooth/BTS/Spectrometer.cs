using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
//using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommDevice;
using ThreeNH.Optical;
using ThreeNH.CommProtocol;
using ThreeNH.CommunicationBase;
using ThreeNH.DataModel;
using ThreeNH.StructHelper;
using ThreeNH.Helper;
using System.Linq;

namespace BTS
{
    /// <summary>
    /// 命令处理代理
    /// </summary>
    /// <param name="command">接收到的命令</param>
    /// <param name="data">接收到的数据</param>
    /// <param name="remoteError">远端传过来的错误码</param>
    /// <returns>返回真表示已处理该命令，假表示未处理</returns>
    delegate bool CommandHandler(byte command, byte[] data, int remoteError, bool isLast);

    public class Spectrometer : IColorimeter, IMetrology, ISpecimenClearing, IInstrument, IRecordManagement, ISystemSetting, IMeasurementNotification
    {
        #region Fields

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

        #endregion

        #region Local Classes

        class UploadSpecimenAsyncContext
        {
            public List<byte> Data = new List<byte>();
            public EventHandler<SpecimenReceivedEventArgs> Handler;
        }

        #endregion

        public Spectrometer()
        {
            _clientProtocol.Connected += OnSessionConnected;
            _clientProtocol.Disconnected += OnSessionDisconnected;
            _clientProtocol.Received += OnDataReceived;
            _clientProtocol.SendingProgressChanged += OnDataSendingProgressChanged;
            //            _clientProtocol.PassThroughDataReceived += OnPassthoughDataReceived;

            _commandHandlers[(byte)Command.UploadStandards] = HandleUploadRecordsCommand;
            _commandHandlers[(byte)Command.UploadTrials] = HandleUploadRecordsCommand;
            _commandHandlers[(byte)Command.MeasureStandard] = HandleMeasureCommand;
            _commandHandlers[(byte)Command.MeasureTrial] = HandleMeasureCommand;
        }

        private bool HandleMeasureCommand(byte command, byte[] data, int remoteerror, bool islast)
        {
            if (MeasurementReceived == null)
            {
                return false;
            }
            Thread.Sleep(1000);
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
                MeasurementReceived.Invoke(this, new MeasurementNotificationArgs(specimen));
                return true;
            }
            catch
            {
                return false;
            }
        }

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

            //            Trace.TraceInformation($"接收到：{(Command)e.Operation}, offset = {e.Offset}, length = {e.Data.Length}");

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
            //if (_clientProtocol.IsOpen)
            //{
            //    _clientProtocol.Close();
            //}
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

        private ThreeNH.Optical.ColorSpace _colorSpace;
        public ThreeNH.Optical.ColorSpace ColorSpace
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

        private ThreeNH.Optical.ColorIndex _colorIndex;
        public ThreeNH.Optical.ColorIndex ColorIndex
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
        public ThreeNH.ColorimeterInterface.Tolerance SystemDefaultTolerance
        {
            get
            {
                var t = GetTolerance();
                return new ThreeNH.ColorimeterInterface.Tolerance
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
            //            InstrumentStatus status = GetInstrumentStatus();
            //            _aperture = status.Caliber;
            //            _specularComponent = status.ScMode;
            //            _lensPosition = status.LensPos;
            //            _colorSpace = status.ColorSpace;
            //            _colorDiffFormula = status.ColorDiffFormula;
            //            _colorIndex = status.ColorIndex;
            //            _uvFlag = status.UvMode;
            //            _lensSwitchMode = status.LensSwitchMode;
            //            _spectrumMeasurementMode = status.IsTransmission
            //                ? SpectrumMeasurementMode.Transmissive
            //                : SpectrumMeasurementMode.Reflective;
            //            _observer = status.Observer;
            //            _standardIlluminant = status.Illuminant;
        }

        #endregion

        #region IInstrument

        public string Model { get; private set; }

        public InternalModelType _InternalModelType { get; set; }
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
            get { return new WavelengthRange(360, 780, 10); }
            set { throw new NotSupportedException(); }
        }

        public void DoBlackCalibration()
        {
            Calibrate(true);
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

        #endregion


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
            SendAndWait(Commands.DeleteStandard((int)(standardId & 0x80000000)), standardId == -1 ? 60000 : 5000);
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

        #endregion


        public void Connect()
        {
            Connect(null);
        }

        private void CreateSession()
        {
            _connectedEvent.Reset();
            _clientProtocol.Connect();
            _connectedEvent.WaitOne(30000);
            if (!_clientProtocol.IsConnected)
            {
                Trace.TraceWarning("建立会话失败");
                _clientProtocol.Close();
                throw new CommunicationException(CommunicationError.DeviceOpenFailed);
            }

            if (_clientProtocol.ProductType != (int)ProductType.BTS)
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
            var result = SendAndWait(Commands.Measure(type, sc?.ToInstrumentForm()), 20000);
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

        //        public Task<Specimen> MeasureAsync()
        //        {
        //            return Task<Specimen>.Factory.StartNew(() => Measure());
        //        }

        private void Calibrate(bool isBlack = true)
        {
            var cmd = Commands.Calibrate(isBlack);
            Send(cmd);
            var result = Wait(cmd.Command, 30000);
            if (result == null)
            {
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }

            if (result.Data[0] == 0)
            {
                throw new CommunicationException(CommunicationError.CalibrationFailed);
            }
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
            _InternalModelType = GetInternalModel();
            DeviceInfo info = GetDeviceInfo();

            Model = info.Model;
            SN = info.Sn;
            WhiteBoardNumber = info.WhiteBoardNumber;
            FirmwareRevision = info.SoftwareVersion;
            HardwareRevision = info.HardwareVersion;

            InstrumentStatus status = GetInstrumentStatus();
            _aperture = Model == "MCR10" ? MeasurementCaliber.Phi1 : (Model == "BTS20" ? MeasurementCaliber.Phi50 : status.Caliber);
            //_aperture = Model switch
            //{
            //    "MCR10" => MeasurementCaliber.Phi1,
            //    "BTS20" => MeasurementCaliber.Phi50,
            //    _ => status.Caliber
            //};
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
            throw new NotImplementedException();
        }

        public void SetInstrumentLanguage(InstrumentLanguage lang)
        {
            throw new NotImplementedException();
        }

        public ProductQeuipInfo GetProductQeuipInfo()
        {
            var result = SendAndWait(Commands.GetProductQeuipInfo());
            if (result.Data.Length == Marshal.SizeOf(typeof(ProductQeuipInfoV1)))
            {
                return ProductQeuipInfo.FromV1(StructConverter.BytesToStruct<ProductQeuipInfoV1>(result.Data));
            }
            else if (result.Data.Length == Marshal.SizeOf(typeof(ProductQeuipInfoV2)))
            {
                var info = StructConverter.BytesToStruct<ProductQeuipInfoV2>(result.Data);
                return ProductQeuipInfo.FromV2(info);
            }
            else
            {
                return StructConverter.BytesToStruct<ProductQeuipInfo>(result.Data);
            }
        }


        public void SetProductQeuipInfo(ProductQeuipInfo info)
        {
            if (info.MicroPore != 0x00)
                SendAndWait(Commands.SetProductQeuipInfo(info));
            else
            {
                var product = new ProductQeuipInfoV2()
                {
                    FlagUpdate = info.FlagUpdate,
                    BlackPlate = info.BlackPlate,
                    LensPosStep = info.LensPosStep,
                    BlackReflect = info.BlackReflect,
                    WhitePlate = info.WhitePlate,
                    WhiteReflectSci = info.WhiteReflectSci,
                    WhiteReflectSce = info.WhiteReflectSce,
                    DefaultLanguage = info.DefaultLanguage,
                    LogoAddr = info.LogoAddr,
                    LogoLength = info.LogoLength,
                    SoftwareVersion = info.SoftwareVersion,
                    HardwareVersion = info.HardwareVersion,
                    Sn = info.Sn,
                    ReflectModify = info.ReflectModify,
                    LensMotorAllStep = info.LensMotorAllStep,
                    MajorSensorSn = info.MajorSensorSn,
                    MinorSensorSn = info.MinorSensorSn,
                    WaveCoefMajor = info.WaveCoefMajor,
                    WaveCoefMinor = info.WaveCoefMinor,
                    Company = info.Company,
                    ModelName = info.ModelName,
                    ReflectIntegralTime = info.ReflectIntegralTime,
                    ScMotorRunTime = info.ScMotorRunTime,
                    TransIntegralTime = info.TransIntegralTime
                };
                SendAndWait(Commands.SetProductQeuipInfo(product));
            }
        }




        public void StepperMotorRunToStart()
        {
            SendAndWait(Commands.StepperMotorRunToStart());
        }


        public void StepperMotorRunToEnd()
        {
            SendAndWait(Commands.StepperMotorRunToEnd());
        }


        public void StepperMotorTestStep(uint step, uint times)
        {
            SendAndWait(Commands.StepperMotorTestStep(step, times), (int)(6000 * times));
        }


        /// <summary>
        /// DC马达测试
        /// </summary>
        /// <param name="scMode">测光方式</param>
        /// <param name="plus_time">脉冲时间</param>
        /// <param name="times">往返次数</param>
        public void DCMotorTest(ScMode scMode, uint plus_time, uint times)
        {
            SendAndWait(Commands.DCMotorTest(scMode, plus_time, times), (int)(5000 * (times + 1)));
        }

        /// <summary>
        /// 测试LED
        /// </summary>
        /// <param name="ledIndex">要测试LED的索引，如果为0xFF表示所有灯</param>
        /// <param name="on">如果为真点亮灯，否则关掉灯</param>
        public void LedTest(byte ledIndex, bool on)
        {
            SendAndWait(Commands.LedTest(ledIndex, on));
        }
        /// <summary>
        /// 仪器校正
        /// </summary>
        public void ColoredBrick1Calib()
        {
            SendAndWait(Commands.ColoredBrick1Calib());
        }
        public void ColoredBrick1Clear()
        {
            SendAndWait(Commands.ColoredBrick1Clear());
        }
        /// <summary>
        /// 相机测试
        /// </summary>
        /// <param name="on">如果为true打开相机，否则关掉相机</param>
        public void CameraTest(bool on)
        {
            SendAndWait(Commands.CameraTest(on));
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
            SendAndWait(Commands.BuzzerTest());
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
        /// 自动获取积分时间
        /// </summary>
        /// <param name="isTransmission">如果为真获取透射模式的积分时间，否则获取反射</param>
        /// <param name="maxAdcValue">ADC最大采样值</param>
        /// <returns>相应的积分时间</returns>
        public uint AutoGetIntegralTime(bool isTransmission, ushort maxAdcValue)
        {
            var cmd = Commands.AutoGetIntegralTime(isTransmission, maxAdcValue);
            Send(cmd);
            var result = Wait(cmd.Command, 300000);
            if (result == null)
            {
                throw new CommunicationException(CommunicationError.ReceivingTimeout);
            }

            if (result.Data == null)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    $"Instrument Error: {result.AckCode}");
            }

            return BitConverter.ToUInt32(result.Data, 0);
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
                _clientProtocol.Send((byte)packet.Command, null, packet.Data);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
        private static object _waitingCommandLocker = new object();

        public CommandPacket Wait(byte command = 0, int timeout = Int32.MaxValue)
        {
            lock (_waitingCommandLocker)
            {
                _waitingCommand = (Command)command;
            }
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
                    lock (_waitingCommandLocker)
                    {
                        _waitingCommand = Command.InvalidCommand;
                    }
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
        public void GenerateAdLimit(bool isTransmission, bool isWhite)
        {
            // 不等待结果
            SendAndWait(Commands.GenerateAdLimit(isTransmission, isWhite), timeout: 60000);
        }

        /// <summary>
        /// 存储生成的AD限制
        /// </summary>
        /// <returns></returns>
        public void SaveAdLimits()
        {
            SendAndWait(Commands.SaveAdLimits());
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

        /// <summary>
        /// 设置内部型号
        /// </summary>
        /// <param name="modelType">仪器内部型号</param>
        public void SetInternalModel(InternalModelType modelType)
        {
            SendAndWait(Commands.SetInternalModel(modelType));
        }

        public void SetSpecialTransmissionCaliber(SpecialTransmissionCaliber caliber)
        {
            SendAndWait(Commands.SetSpecialTransmissionCaliber(caliber));
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

        public void StepMotorCalibration(ushort value)
        {
            SendAndWait(Commands.StepMotorCalibration(value));
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

        public WhiteBoardParameter GetWhiteBoardParameter()
        {
            var result = SendAndWait(Commands.GetWhiteBoardParameter());
            try
            {
                return StructConverter.BytesToStruct<WhiteBoardParameter>(result.Data);
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void SetWhiteBoardParameter(WhiteBoardParameter parameter)
        {
            SendAndWait(Commands.SetWhiteBoardParameter(parameter));
        }

        public GratingWavelengthCoefficients GetWavelengthCoef()
        {
            var result = SendAndWait(Commands.GetWavelengthCoef());
            try
            {
                return new GratingWavelengthCoefficients(StructConverter.BytesToStruct<InnerGratingWavelengthCoefficients>(result.Data));
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void SetWavelengthCoef(GratingWavelengthCoefficients coef)
        {
            SendAndWait(Commands.SetWavelengthCoef(coef));
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
            return ((BTS.UvMode)result.Data[0]).Uniform();
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

        public ThreeNH.Optical.ColorSpace GetColorSpace()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.ColorSpace));
            return ((ColorSpace)result.Data[0]).Uniform();
        }

        public void SetColorSpace(ThreeNH.Optical.ColorSpace colorSpace)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorSpace,
                (byte)colorSpace.ToInstrumentForm()));
        }

        public ThreeNH.Optical.ColorDiffFormula GetColorDiffFormula()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.ColorDiffFormula));
            return ((ColorDeltaFormula)result.Data[0]).Uniform();
        }

        public void SetColorDiffFormula(ThreeNH.Optical.ColorDiffFormula colorDiffFormula)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorDiffFormula,
                (byte)colorDiffFormula.ToInstrumentForm()));
        }

        public void SetColorIndex(ThreeNH.Optical.ColorIndex colorIndex)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorIndex,
                (byte)colorIndex.ToInstrumentForm()));
        }


        public ThreeNH.Optical.ColorIndex GetColorIndex()
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

        public void SwitchOpticalStructure(OpticalStructure optical)
        {
            SendAndWait(Commands.SwitchOpticalStructure(optical));
        }

        /// <summary>
        /// 获取默认口径
        /// </summary>
        /// <param name="isTransCaliber">如果为true获取透射默认口径</param>
        /// <returns>如果仪器是常规型号或是获取的是透射口径，返回的口径代号为 BTS.Caliber，如果为微孔（TS8216）且为反射口径，返回的为BTS.MicroCaliber</returns>
        public byte GetDefaultCaliber(bool isTransCaliber)
        {
            var packet = SendAndWait(Commands.GetDefaultCaliber(isTransCaliber));
            if (packet.Data.Length == 1)
            {
                return packet.Data[0];
            }
            throw new CommunicationException(CommunicationError.ParsedBadData);
        }

        /// <summary>
        /// 设置默认口径
        /// </summary>
        /// <param name="isTransCaliber">如果为真设置透射口径</param>
        /// <param name="caliber">口径代号，如果是TS8216为BTS.MicroCaliber，否则为BTS.Caliber</param>
        public void SetDefaultCaliber(bool isTransCaliber, byte caliber)
        {
            SendAndWait(Commands.SetDefaultCaliber(isTransCaliber, caliber));
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
                return internalModel == InternalModelType.YS6020 ||
                       internalModel == InternalModelType.YS6080;

            }
        }

        public bool FaultCheck(FaultMode faultMode)
        {
            var packet = Commands.FaultCheck(faultMode);
            Send(packet);
            var result = Wait(packet.Command, 10000);
            return result.AckCode == (int)RemoteError.NoError;
        }



        /// <summary>
        /// 获取修正系数设置
        /// 接收会返回 CorrectionFactors_t
        /// </summary>
        /// <param name="sc">SCI或SCE</param>
        /// <param name="caliber">有效的反射口径</param>
        /// <returns></returns>
        public CorrectionFactors_t GetCorrection(ScMode sc, Caliber caliber)
        {
            var ack = SendAndWait(Commands.GetCorrection(sc, caliber));
            if (ack.AckCode == 0 && ack.Data.Length == Marshal.SizeOf(typeof(CorrectionFactors_t)))
            {
                return StructConverter.BytesToStruct<CorrectionFactors_t>(ack.Data);
            }
            throw new CommunicationException(CommunicationError.ParsedBadData);
        }

        /// <summary>
        /// 设置修正系数
        /// </summary>
        /// <param name="sc">SCI或SCE</param>
        /// <param name="caliber">有效的反射口径</param>
        /// <param name="factors">要设置的修正因子</param>
        /// <returns></returns>
        public void SetCorrection(ScMode sc, Caliber caliber, CorrectionFactors_t factors)
        {
            SendAndWait(Commands.SetCorrection(sc, caliber, factors));
        }

        /// <summary>
        /// 获取当前修正开关是否开启的命令
        /// 
        /// 接收会返回一个 byte，是否为0指示是否开启
        /// </summary>
        /// <returns></returns>
        public bool GetCorrectionEnable()
        {
            var ack = SendAndWait(Commands.GetCorrectionEnable());
            if (ack.AckCode == 0 && ack.Data.Length == 1) { 
                return ack.Data[0] != 0;
            }
            throw new CommunicationException(CommunicationError.ParsedBadData);
        }

        /// <summary>
        /// 设置修正开关是否开启
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public void SetCorrectionEnable(bool enable)
        {
            SendAndWait(Commands.SetCorrectionEnable(enable));
        }

        #region SVD
        /// <summary>
        /// SVD测量
        /// </summary>
        /// <returns></returns>
        public List<float> GetMeasureSpectrumEight(int numbers = 8)
        {

            PacketContent content = new PacketContent();
            content.Enqueue((byte)0);
            List<float> list = new List<float>();
            //var result = SendCommand((byte)Command.ReadSpectrumEight, content);
            //if (result.IsAckOk)
            //{
            //    uint n = result.PacketContent.Length / 4;
            //    for (int i = 0; i < n; i++)
            //    {
            //        list.Add(result.PacketContent.DequeueFloat());
            //    }
            //    return list;
            //}
            return null;
        }
        /// <summary>
        /// 写入算法参数矩阵
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public bool SetSVD(List<List<float>> datas, MeasureType? measureType = null, byte caliber = 0)
        {

            //if (datas == null) return false;
            //var rows = datas.Count;
            //var cols = datas[0].Count;
            //List<float> dataList = new List<float>();
            //byte index = 0;
            //for (int i = 0; i < rows; i++)
            //{
            //    index = (byte)i;
            //    PacketContent packet = new PacketContent();
            //    packet.Enqueue(index);
            //    for (int j = 0; j < cols; j++)
            //    {
            //        packet.Enqueue(datas[i][j]);
            //    }
            //    var result = SendCommand((byte)Command.WriteSVD, packet);
            //    if (!result.IsAckOk)
            //    {

            //        throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
            //            string.Format("SVD写入失败：{0}", result.PacketContent));
            //    }
            //}
            return true;
        }


        /// <summary>
        /// 读取算法参数矩阵
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <returns></returns>
        public List<List<float>> ReadSVD(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            float[,] arry2D = new float[rows, cols];
            //for (int i = 0; i < rows; i++)
            //{
            //    PacketContent content = new PacketContent();
            //    content.Enqueue((byte)i);
            //    var result = SendCommand((byte)Command.ReadSVD, content);
            //    if (result != null && result.IsAckOk)
            //    {
            //        byte tmp;
            //        result.PacketContent.Dequeue(out tmp);
            //        if (tmp == (byte)i)
            //        {
            //            for (int j = 0; j < cols; j++)
            //            {
            //                float tmp2;
            //                result.PacketContent.Dequeue(out tmp2);
            //                arry2D[i, j] = tmp2;
            //            }
            //        }
            //    }
            //}
            return Enumerable.Range(0, arry2D.GetLength(0)).Select(row => Enumerable.Range(0, arry2D.GetLength(1)).Select(col => arry2D[row, col]).ToList()).ToList();
        }

        public List<List<float>> ReadSVDs(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            throw new NotImplementedException();
        }

        public List<float> ReadSVDList(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            throw new NotImplementedException();
        }

        public bool SetOLSR(List<List<float>> datas)
        {
            throw new NotImplementedException();
        }

        public List<float> ReadOLSRList(MeasureType? measureType = null)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Optional Settings //Added by Liguanghai 20/03/27

        public IEnumerable<ValueDescription> OptionalMeasurementCalibers
        {
            get
            {
                List<MeasurementCaliber> list = new List<MeasurementCaliber>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(MeasurementCaliber.Phi4);
                        list.Add(MeasurementCaliber.Phi8);
                        list.Add(MeasurementCaliber.Phi1in);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(MeasurementCaliber.Phi4);
                        list.Add(MeasurementCaliber.Phi8);
                        list.Add(MeasurementCaliber.Phi1in);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(MeasurementCaliber.Phi4);
                        list.Add(MeasurementCaliber.Phi8);
                        list.Add(MeasurementCaliber.Phi1in);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(MeasurementCaliber.Phi4);
                        list.Add(MeasurementCaliber.Phi8);
                        list.Add(MeasurementCaliber.Phi15);
                        list.Add(MeasurementCaliber.Phi1in);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(MeasurementCaliber.Phi4);
                        list.Add(MeasurementCaliber.Phi8);
                        list.Add(MeasurementCaliber.Phi15);
                        list.Add(MeasurementCaliber.Phi1in);
                        break;
                }
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalLensPositions
        {
            get
            {
                List<LensPosition> list = new List<LensPosition>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(LensPosition.Pos4);
                        list.Add(LensPosition.Pos8);
                        list.Add(LensPosition.Pos1in);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(LensPosition.Pos4);
                        list.Add(LensPosition.Pos8);
                        list.Add(LensPosition.Pos1in);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(LensPosition.Pos4);
                        list.Add(LensPosition.Pos8);
                        list.Add(LensPosition.Pos1in);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(LensPosition.Pos4);
                        list.Add(LensPosition.Pos8);
                        list.Add(LensPosition.Pos15);
                        list.Add(LensPosition.Pos1in);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(LensPosition.Pos4);
                        list.Add(LensPosition.Pos8);
                        list.Add(LensPosition.Pos15);
                        list.Add(LensPosition.Pos1in);
                        break;
                }
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalLensSwitchModes
        {
            get
            {
                List<LensSwitchMode> list = new List<LensSwitchMode>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(LensSwitchMode.Auto);
                        list.Add(LensSwitchMode.Manual);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(LensSwitchMode.Auto);
                        list.Add(LensSwitchMode.Manual);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(LensSwitchMode.Auto);
                        list.Add(LensSwitchMode.Manual);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(LensSwitchMode.Auto);
                        list.Add(LensSwitchMode.Manual);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(LensSwitchMode.Auto);
                        list.Add(LensSwitchMode.Manual);
                        break;
                }
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalSpecularComponents
        {
            get
            {
                List<SpecularComponent> list = new List<SpecularComponent>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(SpecularComponent.SCI);
                        list.Add(SpecularComponent.SCE);
                        list.Add(SpecularComponent.SCIAndSCE);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(SpecularComponent.SCI);
                        list.Add(SpecularComponent.SCE);
                        list.Add(SpecularComponent.SCIAndSCE);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(SpecularComponent.SCI);
                        list.Add(SpecularComponent.SCE);
                        list.Add(SpecularComponent.SCIAndSCE);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(SpecularComponent.SCI);
                        list.Add(SpecularComponent.SCE);
                        list.Add(SpecularComponent.SCIAndSCE);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(SpecularComponent.SCI);
                        list.Add(SpecularComponent.SCE);
                        list.Add(SpecularComponent.SCIAndSCE);
                        break;
                }

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalSpectrumMeasurementModes
        {
            get
            {
                List<SpectrumMeasurementMode> list = new List<SpectrumMeasurementMode>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(SpectrumMeasurementMode.Reflective);
                        list.Add(SpectrumMeasurementMode.Transmissive);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(SpectrumMeasurementMode.Reflective);
                        list.Add(SpectrumMeasurementMode.Transmissive);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(SpectrumMeasurementMode.Reflective);
                        list.Add(SpectrumMeasurementMode.Transmissive);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(SpectrumMeasurementMode.Reflective);
                        list.Add(SpectrumMeasurementMode.Transmissive);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(SpectrumMeasurementMode.Reflective);
                        list.Add(SpectrumMeasurementMode.Transmissive);
                        break;
                }

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalUVFlags
        {
            get
            {
                List<UVFlag> list = new List<UVFlag>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(UVFlag.NoCut);
                        list.Add(UVFlag.Cut400nm);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(UVFlag.NoCut);
                        list.Add(UVFlag.Cut400nm);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(UVFlag.NoCut);
                        list.Add(UVFlag.Cut400nm);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(UVFlag.NoCut);
                        list.Add(UVFlag.Cut400nm);
                        list.Add(UVFlag.Cut420nm);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(UVFlag.NoCut);
                        list.Add(UVFlag.Cut400nm);
                        list.Add(UVFlag.Cut420nm);
                        list.Add(UVFlag.Cut460nm);
                        break;
                }

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalStandardIlluminants
        {
            get
            {
                List<StandardIlluminant> list = new List<StandardIlluminant>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(StandardIlluminant.D65);
                        list.Add(StandardIlluminant.D50);
                        list.Add(StandardIlluminant.A);
                        list.Add(StandardIlluminant.C);
                        list.Add(StandardIlluminant.D55);
                        list.Add(StandardIlluminant.D75);
                        list.Add(StandardIlluminant.F1);
                        list.Add(StandardIlluminant.F2);
                        list.Add(StandardIlluminant.F3);
                        list.Add(StandardIlluminant.F4);
                        list.Add(StandardIlluminant.F5);
                        list.Add(StandardIlluminant.F6);
                        list.Add(StandardIlluminant.F7);
                        list.Add(StandardIlluminant.F8);
                        list.Add(StandardIlluminant.F9);
                        list.Add(StandardIlluminant.F10);
                        list.Add(StandardIlluminant.F11);
                        list.Add(StandardIlluminant.F12);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(StandardIlluminant.D65);
                        list.Add(StandardIlluminant.D50);
                        list.Add(StandardIlluminant.A);
                        list.Add(StandardIlluminant.C);
                        list.Add(StandardIlluminant.D55);
                        list.Add(StandardIlluminant.D75);
                        list.Add(StandardIlluminant.F1);
                        list.Add(StandardIlluminant.F2);
                        list.Add(StandardIlluminant.F3);
                        list.Add(StandardIlluminant.F4);
                        list.Add(StandardIlluminant.F5);
                        list.Add(StandardIlluminant.F6);
                        list.Add(StandardIlluminant.F7);
                        list.Add(StandardIlluminant.F8);
                        list.Add(StandardIlluminant.F9);
                        list.Add(StandardIlluminant.F10);
                        list.Add(StandardIlluminant.F11);
                        list.Add(StandardIlluminant.F12);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(StandardIlluminant.D65);
                        list.Add(StandardIlluminant.D50);
                        list.Add(StandardIlluminant.A);
                        list.Add(StandardIlluminant.C);
                        list.Add(StandardIlluminant.D55);
                        list.Add(StandardIlluminant.D75);
                        list.Add(StandardIlluminant.F1);
                        list.Add(StandardIlluminant.F2);
                        list.Add(StandardIlluminant.F3);
                        list.Add(StandardIlluminant.F4);
                        list.Add(StandardIlluminant.F5);
                        list.Add(StandardIlluminant.F6);
                        list.Add(StandardIlluminant.F7);
                        list.Add(StandardIlluminant.F8);
                        list.Add(StandardIlluminant.F9);
                        list.Add(StandardIlluminant.F10);
                        list.Add(StandardIlluminant.F11);
                        list.Add(StandardIlluminant.F12);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(StandardIlluminant.D65);
                        list.Add(StandardIlluminant.D50);
                        list.Add(StandardIlluminant.A);
                        list.Add(StandardIlluminant.C);
                        list.Add(StandardIlluminant.D55);
                        list.Add(StandardIlluminant.D75);
                        list.Add(StandardIlluminant.F1);
                        list.Add(StandardIlluminant.F2);
                        list.Add(StandardIlluminant.F3);
                        list.Add(StandardIlluminant.F4);
                        list.Add(StandardIlluminant.F5);
                        list.Add(StandardIlluminant.F6);
                        list.Add(StandardIlluminant.F7);
                        list.Add(StandardIlluminant.F8);
                        list.Add(StandardIlluminant.F9);
                        list.Add(StandardIlluminant.F10);
                        list.Add(StandardIlluminant.F11);
                        list.Add(StandardIlluminant.F12);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(StandardIlluminant.D65);
                        list.Add(StandardIlluminant.D50);
                        list.Add(StandardIlluminant.A);
                        list.Add(StandardIlluminant.C);
                        list.Add(StandardIlluminant.D55);
                        list.Add(StandardIlluminant.D75);
                        list.Add(StandardIlluminant.F1);
                        list.Add(StandardIlluminant.F2);
                        list.Add(StandardIlluminant.F3);
                        list.Add(StandardIlluminant.F4);
                        list.Add(StandardIlluminant.F5);
                        list.Add(StandardIlluminant.F6);
                        list.Add(StandardIlluminant.F7);
                        list.Add(StandardIlluminant.F8);
                        list.Add(StandardIlluminant.F9);
                        list.Add(StandardIlluminant.F10);
                        list.Add(StandardIlluminant.F11);
                        list.Add(StandardIlluminant.F12);
                        break;
                }

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalStandardObservers
        {
            get
            {
                List<StandardObserver> list = new List<StandardObserver>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(StandardObserver.CIE1931);
                        list.Add(StandardObserver.CIE1964);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(StandardObserver.CIE1931);
                        list.Add(StandardObserver.CIE1964);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(StandardObserver.CIE1931);
                        list.Add(StandardObserver.CIE1964);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(StandardObserver.CIE1931);
                        list.Add(StandardObserver.CIE1964);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(StandardObserver.CIE1931);
                        list.Add(StandardObserver.CIE1964);
                        break;
                }

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorSpaces
        {
            get
            {
                List<ThreeNH.Optical.ColorSpace> list = new List<ThreeNH.Optical.ColorSpace>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_XYZ);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_Yxy);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LCH);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LUV);
                        list.Add(ThreeNH.Optical.ColorSpace.HUNTER_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.sRGB);
                        list.Add(ThreeNH.Optical.ColorSpace.MUNSELL);
                        list.Add(ThreeNH.Optical.ColorSpace.BETA_xy);
                        list.Add(ThreeNH.Optical.ColorSpace.DIN99_LAB);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_XYZ);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_Yxy);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LCH);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LUV);
                        list.Add(ThreeNH.Optical.ColorSpace.HUNTER_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.sRGB);
                        list.Add(ThreeNH.Optical.ColorSpace.MUNSELL);
                        list.Add(ThreeNH.Optical.ColorSpace.BETA_xy);
                        list.Add(ThreeNH.Optical.ColorSpace.DIN99_LAB);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_XYZ);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_Yxy);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LCH);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LUV);
                        list.Add(ThreeNH.Optical.ColorSpace.HUNTER_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.sRGB);
                        list.Add(ThreeNH.Optical.ColorSpace.MUNSELL);
                        list.Add(ThreeNH.Optical.ColorSpace.BETA_xy);
                        list.Add(ThreeNH.Optical.ColorSpace.DIN99_LAB);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_XYZ);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_Yxy);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LCH);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LUV);
                        list.Add(ThreeNH.Optical.ColorSpace.HUNTER_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.sRGB);
                        list.Add(ThreeNH.Optical.ColorSpace.MUNSELL);
                        list.Add(ThreeNH.Optical.ColorSpace.BETA_xy);
                        list.Add(ThreeNH.Optical.ColorSpace.DIN99_LAB);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_XYZ);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_Yxy);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LCH);
                        list.Add(ThreeNH.Optical.ColorSpace.CIE_LUV);
                        list.Add(ThreeNH.Optical.ColorSpace.HUNTER_LAB);
                        list.Add(ThreeNH.Optical.ColorSpace.sRGB);
                        list.Add(ThreeNH.Optical.ColorSpace.MUNSELL);
                        list.Add(ThreeNH.Optical.ColorSpace.BETA_xy);
                        list.Add(ThreeNH.Optical.ColorSpace.DIN99_LAB);
                        break;
                }

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorDiffFormulas
        {
            get
            {
                List<ColorDiffFormula> list = new List<ColorDiffFormula>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(ColorDiffFormula.CIE1976);
                        list.Add(ColorDiffFormula.CIE1994);
                        list.Add(ColorDiffFormula.CIE2000);
                        list.Add(ColorDiffFormula.CMC11);
                        list.Add(ColorDiffFormula.CMC21);
                        list.Add(ColorDiffFormula.CMC);
                        list.Add(ColorDiffFormula.DIN99);
                        list.Add(ColorDiffFormula.DHunterE);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(ColorDiffFormula.CIE1976);
                        list.Add(ColorDiffFormula.CIE1994);
                        list.Add(ColorDiffFormula.CIE2000);
                        list.Add(ColorDiffFormula.CMC11);
                        list.Add(ColorDiffFormula.CMC21);
                        list.Add(ColorDiffFormula.CMC);
                        list.Add(ColorDiffFormula.DIN99);
                        list.Add(ColorDiffFormula.DHunterE);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(ColorDiffFormula.CIE1976);
                        list.Add(ColorDiffFormula.CIE1994);
                        list.Add(ColorDiffFormula.CIE2000);
                        list.Add(ColorDiffFormula.CMC11);
                        list.Add(ColorDiffFormula.CMC21);
                        list.Add(ColorDiffFormula.CMC);
                        list.Add(ColorDiffFormula.DIN99);
                        list.Add(ColorDiffFormula.DHunterE);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(ColorDiffFormula.CIE1976);
                        list.Add(ColorDiffFormula.CIE1994);
                        list.Add(ColorDiffFormula.CIE2000);
                        list.Add(ColorDiffFormula.CMC11);
                        list.Add(ColorDiffFormula.CMC21);
                        list.Add(ColorDiffFormula.CMC);
                        list.Add(ColorDiffFormula.DIN99);
                        list.Add(ColorDiffFormula.DHunterE);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(ColorDiffFormula.CIE1976);
                        list.Add(ColorDiffFormula.CIE1994);
                        list.Add(ColorDiffFormula.CIE2000);
                        list.Add(ColorDiffFormula.CMC11);
                        list.Add(ColorDiffFormula.CMC21);
                        list.Add(ColorDiffFormula.CMC);
                        list.Add(ColorDiffFormula.DIN99);
                        list.Add(ColorDiffFormula.DHunterE);
                        break;
                }

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorIndexs
        {
            get
            {
                List<ThreeNH.Optical.ColorIndex> list = new List<ThreeNH.Optical.ColorIndex>();
                switch (_InternalModelType)
                {
                    case InternalModelType.YS6003:
                    case InternalModelType.YS6002:
                        list.Add(ThreeNH.Optical.ColorIndex.None);
                        list.Add(ThreeNH.Optical.ColorIndex.Yellowness);
                        list.Add(ThreeNH.Optical.ColorIndex.Whiteness);
                        list.Add(ThreeNH.Optical.ColorIndex.Strength);
                        list.Add(ThreeNH.Optical.ColorIndex.MetamerismIndiex);
                        list.Add(ThreeNH.Optical.ColorIndex.Opacity);
                        list.Add(ThreeNH.Optical.ColorIndex.Shade555);
                        list.Add(ThreeNH.Optical.ColorIndex.StainingFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.GardnerIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.ColorFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.PtCoIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.Haze);
                        list.Add(ThreeNH.Optical.ColorIndex.Gloss8);
                        break;
                    case InternalModelType.YS6010:
                        list.Add(ThreeNH.Optical.ColorIndex.None);
                        list.Add(ThreeNH.Optical.ColorIndex.Yellowness);
                        list.Add(ThreeNH.Optical.ColorIndex.Whiteness);
                        list.Add(ThreeNH.Optical.ColorIndex.Strength);
                        list.Add(ThreeNH.Optical.ColorIndex.MetamerismIndiex);
                        list.Add(ThreeNH.Optical.ColorIndex.Opacity);
                        list.Add(ThreeNH.Optical.ColorIndex.Shade555);
                        list.Add(ThreeNH.Optical.ColorIndex.StainingFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.GardnerIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.ColorFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.PtCoIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.Haze);
                        list.Add(ThreeNH.Optical.ColorIndex.Gloss8);
                        break;
                    case InternalModelType.YS6020:
                        list.Add(ThreeNH.Optical.ColorIndex.None);
                        list.Add(ThreeNH.Optical.ColorIndex.Yellowness);
                        list.Add(ThreeNH.Optical.ColorIndex.Whiteness);
                        list.Add(ThreeNH.Optical.ColorIndex.Strength);
                        list.Add(ThreeNH.Optical.ColorIndex.MetamerismIndiex);
                        list.Add(ThreeNH.Optical.ColorIndex.Opacity);
                        list.Add(ThreeNH.Optical.ColorIndex.Shade555);
                        list.Add(ThreeNH.Optical.ColorIndex.StainingFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.GardnerIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.ColorFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.PtCoIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.Haze);
                        list.Add(ThreeNH.Optical.ColorIndex.Gloss8);
                        break;
                    case InternalModelType.YS6060:
                        list.Add(ThreeNH.Optical.ColorIndex.None);
                        list.Add(ThreeNH.Optical.ColorIndex.Yellowness);
                        list.Add(ThreeNH.Optical.ColorIndex.Whiteness);
                        list.Add(ThreeNH.Optical.ColorIndex.Strength);
                        list.Add(ThreeNH.Optical.ColorIndex.MetamerismIndiex);
                        list.Add(ThreeNH.Optical.ColorIndex.Opacity);
                        list.Add(ThreeNH.Optical.ColorIndex.Shade555);
                        list.Add(ThreeNH.Optical.ColorIndex.StainingFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.GardnerIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.ColorFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.PtCoIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.Haze);
                        list.Add(ThreeNH.Optical.ColorIndex.Gloss8);
                        break;
                    case InternalModelType.YS6080:
                        list.Add(ThreeNH.Optical.ColorIndex.None);
                        list.Add(ThreeNH.Optical.ColorIndex.Yellowness);
                        list.Add(ThreeNH.Optical.ColorIndex.Whiteness);
                        list.Add(ThreeNH.Optical.ColorIndex.Strength);
                        list.Add(ThreeNH.Optical.ColorIndex.MetamerismIndiex);
                        list.Add(ThreeNH.Optical.ColorIndex.Opacity);
                        list.Add(ThreeNH.Optical.ColorIndex.Shade555);
                        list.Add(ThreeNH.Optical.ColorIndex.StainingFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.GardnerIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.ColorFastness);
                        list.Add(ThreeNH.Optical.ColorIndex.PtCoIndex);
                        list.Add(ThreeNH.Optical.ColorIndex.Haze);
                        list.Add(ThreeNH.Optical.ColorIndex.Gloss8);
                        break;
                }
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public bool IsLensSwitchModeModifiable
        {
            get { return SpectrumMeasurementMode == SpectrumMeasurementMode.Reflective; }
        }

        public bool IsLensPositionModifiable
        {
            get
            {
                return SpectrumMeasurementMode == SpectrumMeasurementMode.Reflective
                  && LensSwitchMode == LensSwitchMode.Manual;
            }
        }

        public bool IsMeasurementCaliberModifiable
        {
            get
            {
                return SpectrumMeasurementMode == SpectrumMeasurementMode.Reflective
                    && LensSwitchMode == LensSwitchMode.Manual;
            }
        }

        public bool YTContain400 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
       
        #endregion

        public event EventHandler<MeasurementNotificationArgs> MeasurementReceived;
    }
}