﻿﻿﻿using System;
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

namespace ThreeNH.SQCX.Infrastructure.Instruments.BTS
{
    /// <summary>
    /// 命令处理代理
    /// </summary>
    /// <param name="command">接收到的命令</param>
    /// <param name="data">接收到的数据</param>
    /// <param name="remoteError">远端传过来的错误码</param>
    /// <returns>返回真表示已处理该命令，假表示未处理</returns>
    delegate bool CommandHandler(byte command, byte[] data, int remoteError, bool isLast);

    public class Spectrometer : IColorimeter, IInstrument, IRecordManagement, ISystemSetting
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

            _commandHandlers[(byte) Command.UploadStandards] = HandleUploadRecordsCommand;
            _commandHandlers[(byte) Command.UploadTrials] = HandleUploadRecordsCommand;
//            _commandHandlers[(byte) Command.MeasureStandard] = HandleMeasureCommand;
//            _commandHandlers[(byte) Command.MeasureTrial] = HandleMeasureCommand;
        }

        public ClientProtocol Protocol => _clientProtocol;

        private void OnDataSendingProgressChanged(object sender, SendingProgressChangedEventArgs e)
        {
            if (_sendingProgressChanged != null)
            {
                int precentage = (e.Total != null)
                    ? ((e.Total == 0) ? 100 : ((int) (100.0 * e.SentCount / (int) e.Total)))
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

            if (e.IsCompleted && !Commands.HasAckData((Command) e.Operation))
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
                    int recordSize = command == (byte) Command.UploadStandards
                        ? Marshal.SizeOf(typeof(StandardRecord))
                        : Marshal.SizeOf(typeof(TrialRecord));
                    List<Specimen> list = new List<Specimen>();
                    for (int i = 0; i * recordSize < _uploadSpecimenAsyncContext.Data.Count; i++)
                    {
                        if ((byte) Command.UploadStandards == command)
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
            get { return _standardIlluminant;}
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
            get { return _colorSpace;}
            set
            {
                SetColorSpace(value);
                _colorSpace = value;
            }
        }

        private ColorDiffFormula _colorDiffFormula;
        public ColorDiffFormula ColorDiffFormula
        {
            get { return _colorDiffFormula;}
            set
            {
                SetColorDiffFormula(value);
                _colorDiffFormula = value;
            }
        }

        private ThreeNH.Optical.ColorIndex _colorIndex;
        public ThreeNH.Optical.ColorIndex ColorIndex
        {
            get { return _colorIndex;}
            set
            {
                SetColorIndex(value);
                _colorIndex = value;
            }
        }

        private UVFlag _uvFlag;
        public UVFlag UvMode
        {
            get { return _uvFlag;}
            set
            {
                SetUvMode(value);
                _uvFlag = value;
            }
        }

        public DateTime DateTime
        {
            get { return GetRtcDateTime();}
            set { SetRtcDateTime(value);}
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
            get { return _spectrumMeasurementMode;}
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

        public ThreeNH.Instruments.DeviceInfo.WavelengthBand WavelengthRange
        {
            get { return new ThreeNH.Instruments.DeviceInfo.WavelengthBand(360, 780, 10); }
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

        public Specimen MeasureStandard()
        {
            return Measure(MeasurementType.Standard, null);
        }

        public Specimen MeasureSample()
        {
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

            if (_clientProtocol.ProductType != (int) ProductType.BTS)
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

        public Specimen Measure(MeasurementType type, SpecularComponent? sc=null)
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

        private void WriteStandard(Specimen standard)
        {
            StandardRecord record = StandardRecord.FromSpecimen(standard);
            SendAndWait(Commands.DownloadStandards(new[] {record}));
        }

        private void WriteStandards(IEnumerable<Specimen> standards)
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
            DeviceInfo info = GetDeviceInfo();

            Model = info.Model;
            SN = info.Sn;
            WhiteBoardNumber = info.WhiteBoardNumber;
            FirmwareRevision = info.SoftwareVersion;
            HardwareRevision = info.HardwareVersion;

            InstrumentStatus status = GetInstrumentStatus();
            _aperture = status.Caliber;
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

        private ProductQeuipInfo GetProductQeuipInfo()
        {
            var result = SendAndWait(Commands.GetProductQeuipInfo());
            if (result.Data.Length == Marshal.SizeOf(typeof(ProductQeuipInfoV1)))
            {
                return ProductQeuipInfo.FromV1(StructConverter.BytesToStruct<ProductQeuipInfoV1>(result.Data));
            }
            else
            {
                return StructConverter.BytesToStruct<ProductQeuipInfo>(result.Data);
            }
        }


        private void SetProductQeuipInfo(ProductQeuipInfo info)
        {
            SendAndWait(Commands.SetProductQeuipInfo(info));
        }


        private void StepperMotorRunToStart()
        {
            SendAndWait(Commands.StepperMotorRunToStart());
        }


        private void StepperMotorRunToEnd()
        {
            SendAndWait(Commands.StepperMotorRunToEnd());
        }


        private void StepperMotorTestStep(uint step, uint times)
        {
            SendAndWait(Commands.StepperMotorTestStep(step, times), (int)(5000 * times));
        }


        /// <summary>
        /// DC马达测试
        /// </summary>
        /// <param name="scMode">测光方式</param>
        /// <param name="plus_time">脉冲时间</param>
        /// <param name="times">往返次数</param>
        private void DCMotorTest(ScMode scMode, uint plus_time, uint times)
        {
            SendAndWait(Commands.DCMotorTest(scMode, plus_time, times), (int) (5000 * (times + 1)));
        }

        /// <summary>
        /// 测试LED
        /// </summary>
        /// <param name="ledIndex">要测试LED的索引，如果为0xFF表示所有灯</param>
        /// <param name="on">如果为真点亮灯，否则关掉灯</param>
        private void LedTest(byte ledIndex, bool on)
        {
            SendAndWait(Commands.LedTest(ledIndex, on));
        }

        /// <summary>
        /// 相机测试
        /// </summary>
        /// <param name="on">如果为true打开相机，否则关掉相机</param>
        private void CameraTest(bool on)
        {
            SendAndWait(Commands.CameraTest(on));
        }

        /// <summary>
        /// 检测Flash
        /// </summary>
        private bool FlashTest()
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
        private bool SdramTest()
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
        private void BuzzerTest()
        {
            SendAndWait(Commands.BuzzerTest());
        }

        /// <summary>
        /// 测试显示屏
        /// </summary>
        private void LcdTest()
        {
            SendAndWait(Commands.LcdTest(), timeout: 10000);
        }

        /// <summary>
        /// 测试触屏，显示指定数格子
        /// </summary>
        /// <param name="rowCount">格子行数</param>
        /// <param name="colCount">格子列数</param>
        private void TouchScreenTest(ushort rowCount, ushort colCount)
        {
            SendAndWait(Commands.TouchScreenTest(rowCount, colCount));
        }

        /// <summary>
        /// 获取RTC时间
        /// </summary>
        /// <returns>返回获取的时间</returns>
        /// <exception cref="CommunicationException"></exception>
        private DateTime GetRtcDateTime()
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
        private void SetRtcDateTime(DateTime dateTime)
        {
            SendAndWait(Commands.SetRtcDateTime(dateTime));
        }

        /// <summary>
        /// 自动获取积分时间
        /// </summary>
        /// <param name="isTransmission">如果为真获取透射模式的积分时间，否则获取反射</param>
        /// <param name="maxAdcValue">ADC最大采样值</param>
        /// <returns>相应的积分时间</returns>
        private uint AutoGetIntegralTime(bool isTransmission, ushort maxAdcValue)
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
        private void TestPrinter()
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

        private InstrumentStatus GetInstrumentStatus()
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

        private void Send(CommandPacket packet)
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
                _clientProtocol.Send((byte) packet.Command, null, packet.Data);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private CommandPacket Wait(byte command = 0, int timeout = Int32.MaxValue)
        {
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
                }
            }

            _currentCommand = 0;

            return null;
        }

        private ProgressChangedEventHandler _sendingProgressChanged;

        /// <summary>
        /// 写入资源文件
        /// </summary>
        /// <param name="stream">要发送的资源</param>
        /// <param name="handler">进度改变处理函数；如果发送过程出错UserState会传入Exception对像</param>
        private void WriteResource(Stream stream, ProgressChangedEventHandler handler = null)
        {
            if (!_clientProtocol.IsConnected)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            _sendingProgressChanged = handler;
            _clientProtocol.SendResource((uint) stream.Length, stream);
        }

        /// <summary>
        /// 写入LOGO文件
        /// </summary>
        /// <param name="stream">要发送的LOGO</param>
        /// <param name="handler">进度改变处理函数；如果发送过程出错UserState会传入Exception对像</param>
        private void WriteLogo(Stream stream, ProgressChangedEventHandler handler = null)
        {
            if (!_clientProtocol.IsConnected)
            {
                throw new CommunicationException(CommunicationError.DeviceNotOpen);
            }

            _sendingProgressChanged = handler;
            _clientProtocol.SendLogo((uint) stream.Length, stream);
        }

        /// <summary>
        /// 生成校准的AD限制
        /// </summary>
        /// <param name="isTransmission">是否是透射模式</param>
        /// <param name="isWhite">是否是白校准</param>
        /// <returns></returns>
        private void GenerateAdLimit(bool isTransmission, bool isWhite)
        {
            // 不等待结果
            SendAndWait(Commands.GenerateAdLimit(isTransmission, isWhite), timeout: 60000);
        }

        /// <summary>
        /// 存储生成的AD限制
        /// </summary>
        /// <returns></returns>
        private void SaveAdLimits()
        {
            SendAndWait(Commands.SaveAdLimits());
        }

        /// <summary>
        /// 获取内部型号
        /// </summary>
        /// <returns>内部型号</returns>
        private InternalModelType GetInternalModel()
        {
            var result = SendAndWait(Commands.GetInternalModel());
            return (InternalModelType)result.Data[0];
        }

        /// <summary>
        /// 设置内部型号
        /// </summary>
        /// <param name="modelType">仪器内部型号</param>
        private void SetInternalModel(InternalModelType modelType)
        {
            SendAndWait(Commands.SetInternalModel(modelType));
        }

        /// <summary>
        /// 获取仪器的CPU ID
        /// </summary>
        /// <returns>CPU编号</returns>
        private byte[] GetCpuId()
        {
            var result = SendAndWait(Commands.GetCpuId());
            return result.Data;
        }

        /// <summary>
        /// 设置数据加密密钥
        /// </summary>
        /// <param name="encryption">加密密钥</param>
        private void SetEncryption(ushort encryption)
        {
            SendAndWait(Commands.SetEncryption(encryption));
        }

        /// <summary>
        /// 读取仪器认样文件中的试样
        /// </summary>
        /// <param name="fileName">试样文件名</param>
        /// <returns>读取到的试样数据</returns>
        private static Specimen[] PariseTestSpecimenFile(string fileName)
        {
            var stream = File.OpenRead(fileName);
            return PariseTestSpecimenFile(stream);
        }

        /// <summary>
        /// 读取仪器认样文件中的试样
        /// </summary>
        /// <param name="stream">已打开的试样数据文件流</param>
        /// <returns>读取到的试样数据</returns>
        private static Specimen[] PariseTestSpecimenFile(Stream stream)
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

        private WhiteBoardParameter GetWhiteBoardParameter()
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

        private void SetWhiteBoardParameter(WhiteBoardParameter parameter)
        {
            SendAndWait(Commands.SetWhiteBoardParameter(parameter));
        }

        private GratingWavelengthCoefficients GetWavelengthCoef()
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

        private void SetWavelengthCoef(GratingWavelengthCoefficients coef)
        {
            SendAndWait(Commands.SetWavelengthCoef(coef));
        }


        private SpectrumMeasurementMode GetSpectrumMeasurementMode()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.SpectrumMeasurementMode));
            return result.Data[0] == 0 ? SpectrumMeasurementMode.Reflective : SpectrumMeasurementMode.Transmissive;
        }

        private void SetSpectrumMeasurementMode(SpectrumMeasurementMode mode)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.SpectrumMeasurementMode,
                (byte) (mode == SpectrumMeasurementMode.Reflective ? 0 : 1)));
        }

        private SpecularComponent GetSpecularComponent()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.SpecularComponent));
            return ((ScMode) result.Data[0]).Uniform();
        }

        private void SetSpecularComponent(SpecularComponent sc)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.SpecularComponent, (byte) sc.ToInstrumentForm()));
        }

        private LensSwitchMode GetLensSwitchMode()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.LensSwitchMode));
            return ((LensMode)result.Data[0]).Uniform();
        }

        private void SetLensSwitchMode(LensSwitchMode mode)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.LensSwitchMode, (byte) mode.ToInstrumentForm()));
        }

        private LensPosition GetLensPosition()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.LensPosition));
            return ((LensPos) result.Data[0]).Uniform();
        }

        private void SetLensPosition(LensPosition position)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.LensPosition,
                (byte) position.ToInstrumentForm()));
        }

        private MeasurementCaliber GetMeasurementCaliber()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.MeasurementAperture));
            return ((Caliber)result.Data[0]).Uniform();
        }

        private void SetMeasurementCaliber(MeasurementCaliber caliber)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.MeasurementAperture, (byte)caliber.ToInstrumentForm()));
        }

        private UVFlag GetUvMode()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.UvMode));
            return ((ThreeNH.SQCX.Infrastructure.Instruments.BTS.UvMode)result.Data[0]).Uniform();
        }

        private void SetUvMode(UVFlag uv)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.UvMode, (byte)uv.ToInstrumentForm()));
        }

        private StandardIlluminant GetStandardIlluminant()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.StandardIlluminant));
            return ((Illuminant) result.Data[0]).Uniform();
        }

        private void SetStandardIlluminant(StandardIlluminant illuminant)
        {
            var result = SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.StandardIlluminant, (byte) illuminant.ToInstrumentForm()));
        }

        private StandardObserver GetStandardObserver()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.StandardObserver));
            return ((Observer)result.Data[0]).Uniform();
        }

        private void SetStandardObserver(StandardObserver observer)
        {
            var result = SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.StandardObserver, (byte)observer.ToInstrumentForm()));
        }

        private ThreeNH.Optical.ColorSpace GetColorSpace()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.ColorSpace));
            return ((ColorSpace) result.Data[0]).Uniform();
        }

        private void SetColorSpace(ThreeNH.Optical.ColorSpace colorSpace)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorSpace,
                (byte) colorSpace.ToInstrumentForm()));
        }

        private ThreeNH.Optical.ColorDiffFormula GetColorDiffFormula()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.ColorDiffFormula));
            return ((ColorDeltaFormula)result.Data[0]).Uniform();
        }

        private void SetColorDiffFormula(ThreeNH.Optical.ColorDiffFormula colorDiffFormula)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorDiffFormula,
                (byte)colorDiffFormula.ToInstrumentForm()));
        }

        private void SetColorIndex(ThreeNH.Optical.ColorIndex colorIndex)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.ColorIndex,
                (byte)colorIndex.ToInstrumentForm()));
        }


        private ThreeNH.Optical.ColorIndex GetColorIndex()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.ColorIndex));
            return ((ColorIndex) result.Data[0]).Uniform();
        }

        private StandardSpecimen GetCurrentStandard()
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

        private void SetCurrentStandard(StandardSpecimen standard)
        {
            var record = StandardRecord.FromSpecimen(standard);
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.CurrentStandard,
                StructConverter.StructToBytes(record)));
        }

        private Tolerance GetTolerance()
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

        private void SetTolerance(Tolerance tolerance)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.Tolerance,
                StructConverter.StructToBytes(tolerance)));
        }

        private bool IsXeLamp
        {
            get
            {
                var internalModel = GetInternalModel();
                return internalModel == InternalModelType.YS6020 ||
                       internalModel == InternalModelType.YS6080;

            }
        }

        private bool FaultCheck(FaultMode faultMode)
        {
            var packet = Commands.FaultCheck(faultMode);
            Send(packet);
            var result = Wait(packet.Command, 10000);
            return result.AckCode == (int) RemoteError.NoError;
        }
    }
}