using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommDevice;
using ThreeNH.CommProtocol;
using ThreeNH.CommunicationBase;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;
using ThreeNH.StructHelper;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YHComm
{

    /// <summary>
    /// 命令处理代理
    /// </summary>
    /// <param name="command">接收到的命令</param>
    /// <param name="data">接收到的数据</param>
    /// <param name="remoteError">远端传过来的错误码</param>
    /// <returns>返回真表示已处理该命令，假表示未处理</returns>
    delegate bool CommandHandler(byte command, byte[] data, int remoteError, bool isLast);

    public class USBComm : IDisposable, IColorimeter, IMetrology, ISpecimenClearing,  IRecordManagement, ISystemSetting, IMeasurementNotification
    {
        #region Fields

        private bool _isDisposed = false;
        private const int DefaultTimeout = 20000;

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

        public USBComm()
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
                    if(data.Length>=168)
                    {
                        specimen = StructConverter.BytesToStruct<StandardRecordFor1810>(data).ToSpecimen();
                    }
                    else
                    {
                        specimen = StructConverter.BytesToStruct<StandardRecord>(data).ToSpecimen();
                    }
                }
                else
                {
                    if (data.Length >= 144)
                    {
                        specimen = StructConverter.BytesToStruct<TrialRecordFor1810>(data).ToSpecimen();
                    }
                    else
                    {
                        specimen = StructConverter.BytesToStruct<TrialRecord>(data).ToSpecimen();
                    }
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

        ~USBComm()
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

        #endregion



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

        public SpecularComponent SpecularComponent
        {
            get => SpecularComponent.Unknown;
            set => throw new NotSupportedException();
        }

        public LensSwitchMode LensSwitchMode
        {
            get => LensSwitchMode.Manual;
            set => throw new NotSupportedException();
        }

        public LensPosition LensPosition
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        private MeasurementCaliber _aperture;
        public MeasurementCaliber Aperture
        {
            get => _aperture;
            set => throw new NotSupportedException(); // 不支持用户切换口径类型
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

        public UVFlag UvMode
        {
            get => UVFlag.None;
            set => throw new NotSupportedException();
        }

        public DateTime DateTime
        {
            get { return GetRtcDateTime(); }
            set { SetRtcDateTime(value); }
        }

        public HazeReferStandard HazeReferStandard { get; private set; }

        public ColorimeterInterface.Tolerance SystemDefaultTolerance
        {
            get
            {
                var t = GetTolerance().ColorTolerance;
                if (t != null)
                {
                    var tolerance = (ColorTolerance)t;
                    return new ColorimeterInterface.Tolerance
                    {
                        dE = tolerance.DeltaE,
                        dL = new Tuple<double, double>(tolerance.Items[0].Lower, tolerance.Items[0].Upper),
                        da = new Tuple<double, double>(tolerance.Items[1].Lower, tolerance.Items[1].Upper),
                        db = new Tuple<double, double>(tolerance.Items[2].Lower, tolerance.Items[2].Upper)
                    };
                }
                return null;
            }
            set
            {
                var t = new ColorTolerance()
                {
                    DeltaE = (float)value.dE,
                    Items = new ToleranceItem[3]
                    {
                        new ToleranceItem{Lower = value.dL.Item1, Upper = value.dL.Item2},
                        new ToleranceItem{Lower = value.da.Item1, Upper = value.da.Item2},
                        new ToleranceItem{Lower = value.db.Item1, Upper = value.db.Item2},
                    }
                };
                RecordTolerance tolerance = new RecordTolerance
                {
                    ColorTolerance = t
                };
                SetTolerance(tolerance);
            }
        }

        public CultureInfo Language { get; set; }

        public object GetSetting(string propertyName)
        {
            return typeof(USBComm).GetProperty(propertyName)?.GetValue(this, null);
        }

        public void SetSetting(string propertyName, object value)
        {
            typeof(USBComm).GetProperty(propertyName)?.SetValue(this, value, null);
        }

        public bool IsUvModifiable
        {
            get { return false; }
        }
        public bool IsSpecularComponentModifiable
        {
            get { return false; }
        }
        public bool IsSpectrumMeasurementModeModifiable
        {
            get { return false; }
        }


        public void RefreshCaches()
        {
            RefreshInstrumentInfo();
        }

        #endregion

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

        /// <summary>
        /// 是否是色彩雾度计
        /// </summary>
        public bool IsColorHazeMeter { get; private set; }

        /// <summary>
        /// 是否有补偿测试口
        /// </summary>
        public bool HasCompensationPort { get; private set; }

        public WavelengthRange WavelengthRange
        {
            get { return new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP); }
            set { throw new NotSupportedException(); }
        }

        public void DoBlackCalibration()
        {
            Calibrate(CalibrationType.BLACK);
        }

        public Specimen Measure()
        {
            return MeasureHaze();
        }

        /// <summary>
        /// 测量雾度
        /// </summary>
        /// <returns></returns>
        public Specimen MeasureHaze()
        {
            var measureResult = Measure(MeasurementType.HAZE);
            if (measureResult is HazeMeasureResult result)
            {
                Specimen specimen = new Specimen();
                if (result.Spectrum != null)
                {
                    specimen.Clr = new ColorData(result.Spectrum);
                }
                specimen.MeasurementCaliber = Aperture;
                specimen.Instrument = Model;
                specimen.IsTransmission = true;
                specimen.HazeDatas = new[]
                {
                        result.Hazes[(int) HazeType.ASTM_A].ToUniform(HazeType.ASTM_A),
                        result.Hazes[(int) HazeType.ASTM_C].ToUniform(HazeType.ASTM_C),
                        result.Hazes[(int) HazeType.ISO_D65].ToUniform(HazeType.ISO_D65),
                    };
                return specimen;
            }
            if (measureResult is HazeMeasureResultFor1810 result1810)
            {
                Specimen specimen = new Specimen();
                if (result1810.Spectrum != null)
                {
                    specimen.Clr = new ColorData(result1810.Spectrum);
                }
                specimen.MeasurementCaliber = Aperture;
                specimen.Instrument = Model;
                specimen.IsTransmission = true;
                specimen.HazeDatas = new[]
                {
                        result1810.Hazes[(int) HazeType.ASTM_A].ToUniform(HazeType.ASTM_A),
                        result1810.Hazes[(int) HazeType.ASTM_C].ToUniform(HazeType.ASTM_C),
                        result1810.Hazes[(int) HazeType.ISO_D65].ToUniform(HazeType.ISO_D65),
                    };
                return specimen;
            }

            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        /// <summary>
        /// 测量清晰度
        /// </summary>
        /// <returns>清晰度结果</returns>
        public double MeasureClarity()
        {
            if (Measure(MeasurementType.CLARITY) is ushort val)
            {
                return val * 0.01;
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        /// <summary>
        /// 测量透射数据
        /// </summary>
        /// <returns>透射样品</returns>
        public Specimen MeasureTrans()
        {
            if (Measure(MeasurementType.TRANS) is TransMeasureResult result)
            {
                Specimen specimen = new Specimen();
                if (result.Spectrum != null)
                {
                    specimen.Clr = new ColorData(result.Spectrum);
                }
                specimen.MeasurementCaliber = Aperture;
                specimen.Instrument = Model;
                specimen.IsTransmission = true;

                return specimen;
            }

            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public Specimen MeasureStandard()
        {
            return Measure();
        }

        public Specimen MeasureSample()
        {
            return Measure();
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
            Calibrate(CalibrationType.WHITE);
        }

        public void DoClarityCalibration()
        {
            Calibrate(CalibrationType.CLARITY);
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

            if (_clientProtocol.ProductType != (int)ProductType.YH)
            {
                _clientProtocol.Close();
                throw new CommunicationException(CommunicationError.DeviceOpenFailed);
            }

            // _clientProtocol.PassThroughDataReceived += delegate(object sender, PassThroughReceivedEventArgs args)
            // {
            //     Trace.TraceInformation("[DEBUG]{0}", Encoding.GetEncoding("gb2312").GetString(args.Data));
            // };
            // _clientProtocol.IsPassthroughEnabled = true;
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

        /// <summary>
        /// 测量
        /// </summary>
        /// <param name="type">测量类型</param>
        /// <returns>根据不同的测量类型及仪器类型返回不同的结果
        /// <para>对于色彩雾度仪(<c>IsColorHazeMeter</c>为true时)返回以下结果：</para>
        /// <list type="bullet">
        /// <item>
        /// <term>Haze</term>
        /// <description>HazeMeasureResult</description>
        /// </item>
        /// <item>
        /// <term>TRANS</term>
        /// <description>TransMeasureResult</description>
        /// </item>
        /// <item>
        /// <term>SAMPLING_DATA</term>
        /// <description>RawSampleData</description>
        /// </item>
        /// </list>
        /// <para>对于雾度仪(<c>IsColorHazeMeter</c>为false时)返回以下结果：</para>
        /// <list type="bullet">
        /// <item>
        /// <term>Haze</term>
        /// <description>HazeData[]</description>
        /// </item>
        /// <item>
        /// <term>TRANS</term>
        /// <description>float，感应器归一化后的入光量</description>
        /// </item>
        /// <item>
        /// <term>SAMPLING_DATA</term>
        /// <description>RawSampleData</description>
        /// </item>
        /// </list>
        /// </returns>
        public object Measure(MeasurementType type)
        {
            var result = SendAndWait(Commands.Measure(type));

            if (type == MeasurementType.HAZE)
            {
                int count1 = Marshal.SizeOf(typeof(HazeMeasureResult));
                if (result.Data.Length == Marshal.SizeOf(typeof(HazeMeasureResult)))
                {
                    return StructConverter.BytesToStruct<HazeMeasureResult>(result.Data);
                }
                if (result.Data.Length == Marshal.SizeOf(typeof(HazeMeasureResultFor1810)))
                {
                    return StructConverter.BytesToStruct<HazeMeasureResultFor1810>(result.Data);
                }
                int mar = Marshal.SizeOf(typeof(HazeData));
                int count = Marshal.SizeOf(typeof(HazeData)) * Constants.HAZE_TYPE_COUNT;
                if (result.Data.Length == Marshal.SizeOf(typeof(HazeData)) * Constants.HAZE_TYPE_COUNT)
                {
                    HazeMeasureResult hazeResult = new HazeMeasureResult();
                    hazeResult.Hazes = new HazeData[Constants.HAZE_TYPE_COUNT];
                    for (int i = 0; i < Constants.HAZE_TYPE_COUNT; i++)
                    {
                        hazeResult.Hazes[i] = StructConverter.BytesToStruct<HazeData>(result.Data, i * Marshal.SizeOf(typeof(HazeData)));
                    }
                    return hazeResult;
                }
            }
            else if (type == MeasurementType.TRANS)
            {
                if (result.Data.Length == Marshal.SizeOf(typeof(TransMeasureResult)))
                {
                    return StructConverter.BytesToStruct<TransMeasureResult>(result.Data);
                }
                if (result.Data.Length == Marshal.SizeOf(typeof(short)) * Constants.HAZE_TYPE_COUNT)
                {
                    TransMeasureResult transResult = new TransMeasureResult();
                    transResult.Trans = new short[Constants.HAZE_TYPE_COUNT];
                    for (int i = 0; i < Constants.HAZE_TYPE_COUNT; i++)
                    {
                        transResult.Trans[i] = BitConverter.ToInt16(result.Data, i * Marshal.SizeOf(typeof(short)));
                    }
                    return transResult;
                }
            }
            else if (type == MeasurementType.CLARITY)
            {
                return BitConverter.ToUInt16(result.Data, 0);
            }
            else
            {
                throw new InvalidEnumArgumentException();
            }
            throw new CommunicationException(CommunicationError.ParsedBadData);
        }


        public void Calibrate(CalibrationType calibrationType)
        {
            SendAndWait(Commands.Calibrate(calibrationType), 45000);
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
            // 一次只能写入一个标样
            foreach (var specimen in standards)
            {
                WriteStandard(specimen);
            }
            // List<StandardRecord> standardRecords = new List<StandardRecord>();
            // foreach (var specimen in standards)
            // {
            //     standardRecords.Add(StandardRecord.FromSpecimen(specimen));
            // }
            //
            // SendAndWait(Commands.DownloadStandards(standardRecords.ToArray()),
            //     Math.Max(DefaultTimeout, 5000 + standardRecords.Count * 200));
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
            IsColorHazeMeter = info.IsColorHazeMeter;
            HasCompensationPort = info.HasCompensationPort;

            InstrumentStatus status = GetInstrumentStatus();
            _aperture = Model == "MCR10" ? MeasurementCaliber.Phi1 : (Model == "BTS20" ? MeasurementCaliber.Phi50 : status.Caliber);
            _colorSpace = status.ColorSpace;
            _colorDiffFormula = status.ColorDiffFormula;
            _colorIndex = status.ColorIndex;
            _observer = status.Observer;
            _standardIlluminant = status.Illuminant;
            HazeReferStandard = status.HazeReferStandard;
            IsClarityValid = info.IsClarityValid;
            OpticalStructure = "0/d";
        }

        public bool IsClarityValid { get; private set; }

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
            byte[] bytes = null;
            switch (field)
            {
                case ProductInfoField.INTERNAL_MODEL:
                    //InternalModelType modelType = (InternalModelType)value;
                    bytes = new byte[] { (byte)value };
                    break;
                case ProductInfoField.DEFAULT_LANGUAGE:
                    var language = (Language)value;
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
                case ProductInfoField.SPECTROGRAPH:
                    bytes = StructConverter.StructToBytes((SpectrographParameter)value);
                    break;
                case ProductInfoField.REFLECT_MODIFY:
                    bytes = FloatArrayToBytes((float[])value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }

            SendAndWait(Commands.SetProductInfo(field, bytes));
        }

        public object GetProductInfo(ProductInfoField field)
        {
            var result = SendAndWait(Commands.GetProductInfo(field));
            switch (field)
            {
                case ProductInfoField.INTERNAL_MODEL:
                    return (InternalModelType)result.Data[0];
                case ProductInfoField.DEFAULT_LANGUAGE:
                    return (Language)result.Data[0];
                case ProductInfoField.DEVICE_MODEL_NAME:
                case ProductInfoField.SOFTWARE_VERSION:
                case ProductInfoField.HARDWARE_VERSION:
                case ProductInfoField.SN:
                    return ByteArrayToString(result.Data);
                case ProductInfoField.SPECTROGRAPH:
                    return StructConverter.BytesToStruct<SpectrographParameter>(result.Data);
                case ProductInfoField.REFLECT_MODIFY:
                    return ByteArrayToFloats(result.Data);
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
        /// <param name="direction">正向或反向</param>
        /// <param name="time">持续时间，不能超过1000</param>
        public void DCMotorTest(bool direction, UInt16 time = 0)
        {
            SendAndWait(Commands.DCMotorTest(direction, time), 1000);
        }

        /// <summary>
        /// 测试LED
        /// </summary>
        /// <param name="lamps">要测试LED，每位代表一个（实际只有一个灯，直接传入-1）</param>
        /// <param name="on">如果为真点亮灯，否则关掉灯</param>
        public void LedTest(int lamps, bool on)
        {
            SendAndWait(Commands.LedTest(lamps, on));
        }

        /// <summary>
        /// 检测蜂鸣器
        /// </summary>
        public void BuzzerTest()
        {
            SendAndWait(Commands.BuzzerTest(), 15000);
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
        /// 读取仪器试样文件中的试样
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

        public MeasurementCaliber GetMeasurementCaliber()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.MeasurementAperture));
            return ((Aperture)result.Data[0]).Uniform();
        }

        public void SetMeasurementCaliber(MeasurementCaliber caliber)
        {
            SendAndWait(Commands.SetMeasurementParameter(
                MeasurementParameter.MeasurementAperture, (byte)caliber.ToInstrumentForm()));
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

        public HazeReferStandard GetHazeReferStandard()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.RefStandard));
            return (HazeReferStandard)result.Data[0];
        }

        public void SetHazeReferStandard(HazeReferStandard referStandard)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.RefStandard,
                new byte[] { (byte)referStandard }));
            HazeReferStandard = referStandard;
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

        public RecordTolerance GetTolerance()
        {
            var result = SendAndWait(Commands.GetMeasurementParameter(MeasurementParameter.Tolerance));
            try
            {
                return StructConverter.BytesToStruct<RecordTolerance>(result.Data);
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void SetTolerance(RecordTolerance tolerance)
        {
            SendAndWait(Commands.SetMeasurementParameter(MeasurementParameter.Tolerance,
                StructConverter.StructToBytes(tolerance)));
        }


        public InstrumentLanguage GetLanguage()
        {
            var result = SendAndWait(Commands.GetProductInfo(ProductInfoField.DEFAULT_LANGUAGE));
            if (result.Data?.Length == 1)
            {
                return ((Language)result.Data[0]).Uniform();
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
        /// 获取雾度修正设置
        /// </summary>
        /// <param name="aperture">口径</param>
        /// <returns>成功返回相应的修正设置</returns>
        public CorrectionCoefficients GetHazeCorrection(MeasurementCaliber aperture)
        {
            var packets = SendAndWait(Commands.GetProductInfo(ProductInfoField.HAZE_CORRECTION_COEFFICIENTS, new []{(byte)aperture.ToInstrumentForm()}));
            return StructConverter.BytesToStruct<CorrectionCoefficients>(packets.Data);
        }

        /// <summary>
        /// 设置雾度修正
        /// </summary>
        /// <param name="coefficients">修正设置。如果要清除修正设置，写入一个默认构造函数创建的实例即可。</param>
        public void SetHazeCorrection(CorrectionCoefficients coefficients)
        {
            Send(Commands.SetProductInfo(ProductInfoField.HAZE_CORRECTION_COEFFICIENTS, StructConverter.StructToBytes(coefficients)));
        }

        /// <summary>
        /// 获取透过率修正设置
        /// </summary>
        /// <param name="aperture">口径</param>
        /// <returns>成功返回相应的修正设置</returns>
        public CorrectionCoefficients GetTransCorrection(MeasurementCaliber aperture)
        {
            var packets = SendAndWait(Commands.GetProductInfo(ProductInfoField.TRANS_CORRECTION_COEFFICIENTS, new[] { (byte)aperture.ToInstrumentForm() }));
            return StructConverter.BytesToStruct<CorrectionCoefficients>(packets.Data);
        }

        /// <summary>
        /// 设置透过率修正
        /// </summary>
        /// <param name="coefficients">修正设置。如果要清除修正设置，写入一个默认构造函数创建的实例即可。</param>
        public void SetTransCorrection(CorrectionCoefficients coefficients)
        {
            Send(Commands.SetProductInfo(ProductInfoField.TRANS_CORRECTION_COEFFICIENTS, StructConverter.StructToBytes(coefficients)));
        }

        public event EventHandler<MeasurementNotificationArgs> MeasurementReceived;

        #region SVD修正
        /// <summary>
        /// 获取SVD算法计算样本数据
        /// </summary>
        /// <param name="measuere_type">1光井，0补偿口</param>
        /// <returns></returns>
        public List<float> GetMeasureSpectrumEight(int numbers = 8)
        {
            GetHazeReferStandard();
            var result = SendAndWait(Commands.Measure());

            // 获取光阱的测量数据
            PacketContent content = new PacketContent();
            content.Enqueue((byte)Command.MeasureSVDAlgoDatas);
            content.Enqueue((byte)MeasureType.OpticalTrap);
            List<float> list = new List<float>();
            result = SendAndWait(Commands.ReadSpectrumEight(content.Bytes));
            if (null != result.Data && result.Data.Length > 0)
            {
                var packet = new Packer(result.Data);
                while (packet.Dequeue(out float value))
                {
                    list.Add(value);
                }
                return list;
            }
            return null;
        }

        /// <summary>
        /// 读取算法参数矩阵
        /// </summary>
        /// <param name="caliber"></param>
        /// <returns></returns>
        public List<List<float>> ReadSVDs(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            PacketContent content = new PacketContent();
            List<float> list = new List<float>();
            content.Enqueue((byte)Command.ReadAndWriteSVDAlgoParams);
            content.Enqueue((byte)measureType);
            var result = SendAndWait(Commands.ReadSVD(content.Bytes));

            if (null != result.Data && result.Data.Length > 0)
            {
                var packet = new Packer(result.Data);
                packet.Dequeue(out byte way); // 弹出测量模式字节
                packet.Dequeue(out byte zero1); // 弹出三个0字节
                packet.Dequeue(out byte zero2);
                packet.Dequeue(out byte zero3);
                while (packet.Dequeue(out float value))
                {
                    list.Add(value);
                }
            }
            if (result.AckCode != 0)
            {
                Trace.TraceInformation($"执行失败，收到错误码：{(RemoteError)result.AckCode}");
                throw new CommunicationException(((RemoteError)result.AckCode).ToCommError());
            }

            return null;
        }

        public List<float> ReadSVDList(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            PacketContent content = new PacketContent();
            List<float> list = new List<float>();
            content.Enqueue((byte)Command.ReadAndWriteSVDAlgoParams);
            content.Enqueue((byte)measureType);
            var result = SendAndWait(Commands.ReadSVD(content.Bytes));

            if (null != result.Data && result.Data.Length > 0)
            {
                var packet = new Packer(result.Data);
                packet.Dequeue(out byte way); // 弹出测量模式字节
                packet.Dequeue(out byte zero1); // 弹出三个0字节
                packet.Dequeue(out byte zero2);
                packet.Dequeue(out byte zero3);
                while (packet.Dequeue(out float value))
                {
                    list.Add(value);
                }
            }
            if (result.AckCode != 0)
            {
                Trace.TraceInformation($"执行失败，收到错误码：{(RemoteError)result.AckCode}");
                throw new CommunicationException(((RemoteError)result.AckCode).ToCommError());
            }

            return list;
        }
        /// <summary>
        /// 写入算法参数矩阵
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="caliber"></param>
        /// <returns></returns>
        public bool SetSVD(List<List<float>> datas, MeasureType? measureType = null, byte caliber = 0)
        {
            if (datas == null) return false;
            var rows = datas.Count;
            var cols = datas[0].Count;
            List<float> dataList = new List<float>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    dataList.Add(datas[i][j]);
                }
            }

            PacketContent packet = new PacketContent();
            packet.Enqueue((byte)Command.ReadAndWriteSVDAlgoParams);
            packet.Enqueue((byte)measureType);
            for (int i = 0; i < dataList.Count; i++)
            {
                packet.Enqueue(dataList[i], AlgoParameterType.Quadratic);// 不能转为小端所以用Quadratic
            }
            var result = SendAndWait(Commands.WriteSVD(packet.Bytes));
            if (result.AckCode != 0)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    string.Format("SVD写入失败：{0}", result.AckCode));
            }

            return true;
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
                list.Add(MeasurementCaliber.Unknown);
                list.Add(MeasurementCaliber.Phi4);
                list.Add(MeasurementCaliber.Phi8);
                list.Add(MeasurementCaliber.Phi15);
                list.Add(MeasurementCaliber.Phi20);
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

        public IEnumerable<ValueDescription> OptionalSpecularComponents => null;

        public IEnumerable<ValueDescription> OptionalSpectrumMeasurementModes => null;

        public IEnumerable<ValueDescription> OptionalUVFlags => null;

        public IEnumerable<ValueDescription> OptionalStandardIlluminants
        {
            get
            {
                List<StandardIlluminant> list = new List<StandardIlluminant>
                {
                    StandardIlluminant.D65,
                    StandardIlluminant.D50,
                    StandardIlluminant.A,
                    StandardIlluminant.C,
                    StandardIlluminant.D55,
                    StandardIlluminant.D75,
                    StandardIlluminant.F1,
                    StandardIlluminant.F2,
                    StandardIlluminant.F3,
                    StandardIlluminant.F4,
                    StandardIlluminant.F5,
                    StandardIlluminant.F6,
                    StandardIlluminant.F7,
                    StandardIlluminant.F8,
                    StandardIlluminant.F9,
                    StandardIlluminant.F10,
                    StandardIlluminant.F11,
                    StandardIlluminant.F12
                };

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
                List<Optical.ColorSpace> list = new List<Optical.ColorSpace>
                {
                    Optical.ColorSpace.CIE_LAB,
                    Optical.ColorSpace.CIE_XYZ,
                    Optical.ColorSpace.CIE_Yxy,
                    Optical.ColorSpace.CIE_LCH,
                    Optical.ColorSpace.sRGB,
                    Optical.ColorSpace.BETA_xy,
                };
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorDiffFormulas
        {
            get
            {
                List<ColorDiffFormula> list = new List<ColorDiffFormula>
                {
                    ColorDiffFormula.CIE1976,
                    ColorDiffFormula.CIE1994,
                    ColorDiffFormula.CIE2000,
                    ColorDiffFormula.CMC11,
                    ColorDiffFormula.CMC21,
                    ColorDiffFormula.CMC,
                    ColorDiffFormula.DIN99,
                    ColorDiffFormula.DHunterE
                };

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorIndexs
        {
            get
            {
                List<Optical.ColorIndex> list = new List<Optical.ColorIndex>
                {
                    Optical.ColorIndex.Yellowness,
                    Optical.ColorIndex.Whiteness,
                    Optical.ColorIndex.Strength,
                    Optical.ColorIndex.GardnerIndex,
                    Optical.ColorIndex.PtCoIndex
                };


                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public SpectrumMeasurementMode SpectrumMeasurementMode
        {
            get => SpectrumMeasurementMode.Transmissive;
            set => throw new NotSupportedException();
        }

        public bool IsLensSwitchModeModifiable => false;

        public bool IsLensPositionModifiable => false;

        public bool IsMeasurementCaliberModifiable => false;

        public bool YTContain400 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

    }
}
