using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommunicationBase;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;
using ThreeNH.StructHelper;

namespace ThreeNH.SQCX.Infrastructure.Instruments.ColorReader
{
    /// <summary>
    /// 通知命令
    /// </summary>
    enum NotificationCode : byte
    {
        CONNECTED = 1, // 蓝牙连接事件
        DISCONNECTED, // 连接断开事件
        CALIBRATE, // 校准仪器
        MEASURE, // 测量
        GET_SAMPLE_COUNT, // 获取标样相关的样品数
        UPLOAD_STANDARD, // 上传标样
        UPLOAD_SAMPLE, // 上传试样
        INPUT_STANDARD, // 写入标样
        DELETE_STANDARD, // 删除标样
        DELETE_SAMPLE, // 删除试样
    }

    /// <summary>
    /// 调用命令
    /// </summary>
    enum CallCode : byte
    {
        IS_CALIBRATED, // 获取是否校正（立即返回结果）
        GET_STANDARD_COUNT, // 获取标样数
        GET_TOTAL_SAMPLES, // 样品总数
        GET_MODEL, // 仪器型号
        GET_SN, // 仪器序列号
        GET_FIRMWARE_REVISION, // 固件版本号
        GET_HARDWARE_REVISION, // 硬件版本号
        GET_SOFTWARE_REVISION, // 软件版本号
        GET_MANUFACTURER_NAME, // 制造商名
        GET_REGULATORY_CERTIFICATION_DATA_LIST, // 管理认证数据表
        GET_PNP_ID, // PnP ID
        GET_OPTICAL, // 获取光学结构
        GET_WHITE_BOARD_SN, // 获取白板编号
        GET_WHITEBOARD_SPECTRUM, // 获取白板光谱数据
        GET_ILLUMINANT, // 获取当前标准光源设置
        GET_OBSERVER, // 获取当前观察者设置
        GET_CALIBER, // 获取口径设置
        GET_SC_MODE, // 获取SC模式
        GET_COLOR_SPACE, // 获取颜色空间
        GET_COLOR_INDEX, // 获取颜色指数设置
        GET_DIFF_FORMAL, // 获取色差公式
        GET_TOLERANCE, // 获取容差设置
        GET_DATETIME, // 获取仪器时间
        SET_WHITE_BOARD_SN, // 设置白板编号
        SET_WHITEBOARD_SPECTRUM, // 设置白板光谱数据
        SET_ILLUMINANT, // 设置当前标准光源设置
        SET_OBSERVER, // 设置当前观察者设置
        SET_CALIBER, // 设置口径设置
        SET_SC_MODE, // 设置SC模式
        SET_COLOR_SPACE, // 设置颜色空间
        SET_COLOR_INDEX, // 设置颜色指数设置
        SET_DIFF_FORMAL, // 设置色差公式
        SET_TOLERANCE, // 设置取容差设置
        SET_DATETIME, // 设置仪器时间
        GET_UV_MODE, // 获取UV模式
        SET_UV_MODE, // 设置UV模式
        GET_INTERNAL_MODEL, // 获取内部型号
    }

    /// <summary>
    /// 仪器通知码
    /// </summary>
    enum InstrumentNotificationCode : byte
    {
        SDS_DATA, // 串行数据
        SPMS_RESULT, // 通知测量命令的执行结果
        RS_RETURN, // 上传记录时通知上传的记录数据
        SET_DEVICE_NAME, // 废弃命令，用来设置原来的蓝牙模式的名称
        SPMS_STANDARD, // 仪器按键标样测量结果
        SPMS_TRIAL, // 仪器按键试样测量结果
    };

    enum MetrologyCode : byte
    {
        GetKmtcMode= 0x49, // 获取Kmtc模式，或发送SVD修正参数到仪器
        ReaSVDParameters,// 读取SVD修正参数到上位机
        ReadPharmaceuticalConfig =0x52,   //读取药剂配置、或读8个SVD测量数据
    }

    /// <summary>
    /// 蓝牙狗通讯协议
    /// </summary>
    public class BluetoothDongleSpectrometer : EhMc17BluetoothForCR, IInstrument, IMetrology, IMeasurementNotification,
        ISystemSetting, IRecordManagement
    {
        private const string PNP_ID = "TNH TNHMCR";


        public BluetoothDongleSpectrometer()
        {
            ListeningNotificationCode.Add((byte)InstrumentNotificationCode.SPMS_STANDARD);
            ListeningNotificationCode.Add((byte)InstrumentNotificationCode.SPMS_TRIAL);
            InstrumentCmdPacketReceived += OnInstrumentCmdPacketReceived;
        }

        private void OnInstrumentCmdPacketReceived(CmdPacket packet)
        {
            //仪器关闭，更新软件连接状态
            if (packet == null)
            {
                MeasurementReceived?.Invoke(this, null);
                return;
            }
            if (packet.Command == (byte)InstrumentNotificationCode.SPMS_STANDARD)
            {
                if (packet.DataLength == Marshal.SizeOf(typeof(StandardRecord)))
                {
                    try
                    {
                        var standard = StructConverter.BytesToStruct<StandardRecord>(packet.Data);
                        MeasurementReceived?.Invoke(this, new MeasurementNotificationArgs(standard.ToSpecimen()));
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Unpack data error: {0}", e.Message);
                    }
                }
                else
                {
                    Trace.TraceError("Received standard record from instrument, but the length is error\r\n");
                }
            }
            else if (packet.Command == (byte)InstrumentNotificationCode.SPMS_TRIAL)
            {
                if (packet.DataLength == Marshal.SizeOf(typeof(TrialRecord)))
                {
                    try
                    {
                        var trialRecord = StructConverter.BytesToStruct<TrialRecord>(packet.Data);
                        MeasurementReceived?.Invoke(this, new MeasurementNotificationArgs(trialRecord.ToSpecimen()));
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError("Unpack data error: {0}", e.Message);
                    }
                }
                else
                {
                    Trace.TraceError("Received trial record from instrument, but the length is error\r\n");
                }
            }
        }

        /// <summary>
        /// 获取字符串型的仪器配置
        /// </summary>
        /// <param name="code">命令码</param>
        /// <returns>成功返回配置的值</returns>
        private string GetStringConfig(CallCode code)
        {
            byte[] result = SendPacket(new CmdPacket(CommandType.CALL, (byte)code), 1000);
            if (result == null)
            {
                return string.Empty;
            }

            return Encoding.ASCII.GetString(result);
        }


        /// <summary>
        /// 获取int型仪器配置
        /// </summary>
        /// <param name="code">命令码</param>
        /// <returns>成功返回配置的值</returns>
        private int GetIntConfig(CallCode code)
        {
            byte[] result = SendPacket(new CmdPacket(CommandType.CALL, (byte)code), 1000);
            if (result?.Length == 4)
            {
                return BitConverter.ToInt32(result, 0);
            }
            throw new CommunicationException(CommunicationError.ParsedBadData);
        }

        private byte GetByteConfig(CallCode code)
        {
            byte[] result = SendPacket(new CmdPacket(CommandType.CALL, (byte)code), 1000);
            if (result?.Length == 1)
            {
                return result[0];
            }
            throw new CommunicationException(CommunicationError.ParsedBadData);
        }

        private void SetByteConfig(CallCode code, byte value)
        {
            byte[] result = SendPacket(new CmdPacket(CommandType.CALL, (byte)code, new[] { value }), 1000);
            if (result.Length > 0)
            {
                var s = Encoding.ASCII.GetString(result);
                if (s == "INVAL") throw new CommunicationException(CommunicationError.InvalidParameter);
                if (s != "OK") throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
            else
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public bool IsCalibrated => GetByteConfig(CallCode.IS_CALIBRATED) != 0;

        public void DoWhiteCalibration()
        {
            byte[] result =
                SendPacket(new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.CALIBRATE, new[] { (byte)1 }),
                    15000);
            if (Encoding.ASCII.GetString(result) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void DoBlackCalibration()
        {
            byte[] result =
                SendPacket(new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.CALIBRATE, new[] { (byte)0 }),
                    15000);
            if (Encoding.ASCII.GetString(result) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public Specimen Measure()
        {
            return MeasureStandard();
        }

        public Specimen MeasureStandard()
        {
            byte[] result =
                SendPacket(new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.MEASURE, new[] { (byte)0 }),
                    15000);
            if (result.Length == Marshal.SizeOf(typeof(StandardRecord)))
            {
                try
                {
                    var standard = StructConverter.BytesToStruct<StandardRecord>(result);
                    return standard.ToSpecimen();
                }
                catch (Exception e)
                {
                    throw new CommunicationException(CommunicationError.ParsedBadData);
                }
            }
            else
            {
              // return new Specimen();
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public Specimen MeasureSample()
        {
            byte[] result =
                SendPacket(new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.MEASURE, new[] { (byte)1 }),
                    15000);
            if (result.Length == Marshal.SizeOf(typeof(StandardRecord)))
            {
                try
                {
                    var trialRecord = StructConverter.BytesToStruct<TrialRecord>(result);
                    return trialRecord.ToSpecimen();
                }
                catch (Exception e)
                {
                    throw new CommunicationException(CommunicationError.ParsedBadData);
                }
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public string Model { get; private set; }

        public string SN { get; private set; }

        public string HardwareRevision { get; private set; }

        public string FirmwareRevision { get; private set; }

        public string SoftwareRevision { get; private set; }

        public string Manufacturer { get; private set; }

        public string OpticalStructure { get; private set; }

        public string WhiteBoardNumber { get; private set; }


        public DeviceType DeviceType => DeviceType.Spectrophotometer;

        public WavelengthRange WavelengthRange
        {
            get => new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP);
            set => throw new NotSupportedException();
        }

        public string DeviceName => SerialPort?.PortName;

        public void Open(object commDevice = null)
        {
            if (commDevice == null)
            {
            }
            else if (commDevice is string portName)
            {
                base.Open(portName);
            }
            else if (commDevice is SerialPort serialPort)
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }

                base.AttatchSerialPort(serialPort);
            }
            else
            {
                throw new CommunicationException(CommunicationError.DeviceOpenFailed);
            }
        }

        public event EventHandler<MeasurementNotificationArgs> MeasurementReceived;

        public IEnumerable<ValueDescription> OptionalMeasurementCalibers { get; } /*= new[]
        {
            new ValueDescription
                {Description = MeasurementCaliber.Unknown.Description(), Value = MeasurementCaliber.Unknown},
            new ValueDescription {Description = MeasurementCaliber.Phi4.Description(), Value = MeasurementCaliber.Phi4},
            new ValueDescription {Description = MeasurementCaliber.Phi8.Description(), Value = MeasurementCaliber.Phi8},
        };*/
        public IEnumerable<ValueDescription> OptionalLensPositions { get; }
        public IEnumerable<ValueDescription> OptionalLensSwitchModes { get; }
        public IEnumerable<ValueDescription> OptionalSpecularComponents { get; } = new[]
        {
            new ValueDescription{Description = SpecularComponent.SCI.Description(), Value = SpecularComponent.SCI},
            new ValueDescription{Description = SpecularComponent.SCE.Description(), Value = SpecularComponent.SCE},
            new ValueDescription{Description = SpecularComponent.SCIAndSCE.Description(), Value = SpecularComponent.SCIAndSCE},
        };

        public IEnumerable<ValueDescription> OptionalSpectrumMeasurementModes { get; }

        public IEnumerable<ValueDescription> OptionalUVFlags { get; } /* = new[]
        {
            new ValueDescription {Description = UVFlag.NoCut.Description(), Value = UVFlag.NoCut},
            new ValueDescription {Description = UVFlag.Cut400nm.Description(), Value = UVFlag.Cut400nm},
            new ValueDescription {Description = UVFlag.Cut420nm.Description(), Value = UVFlag.Cut420nm},
        };*/

        public IEnumerable<ValueDescription> OptionalStandardIlluminants =>
            from Illuminant x in Enum.GetValues(typeof(Illuminant))
            select new ValueDescription
            { Value = x.Uniform(), Description = x.Uniform().Description() };

        public IEnumerable<ValueDescription> OptionalStandardObservers { get; } = new[]
        {
            new ValueDescription
                {Description = StandardObserver.CIE1931.Description(), Value = StandardObserver.CIE1931},
            new ValueDescription
                {Description = StandardObserver.CIE1964.Description(), Value = StandardObserver.CIE1964},
        };

        public IEnumerable<ValueDescription> OptionalColorSpaces =>
            from x in Enum.GetValues(typeof(ColorReader.ColorSpace)).OfType<ColorReader.ColorSpace>()
            select new ValueDescription { Value = x.Uniform(), Description = x.Uniform().Description() };

        public IEnumerable<ValueDescription> OptionalColorDiffFormulas =>
            from x in Enum.GetValues(typeof(ColorReader.ColorDeltaFormula)).OfType<ColorReader.ColorDeltaFormula>()
            select new ValueDescription
            {
                Value = x.Uniform(),
                Description = x.Uniform().Description()
            };

        public IEnumerable<ValueDescription> OptionalColorIndexs =>
            from x in Enum.GetValues(typeof(ColorReader.ColorIndex)).OfType<ColorReader.ColorIndex>()
            select new ValueDescription
            {
                Value = x.Uniform(),
                Description = x.Uniform().Description()
            };

        public SpectrumMeasurementMode SpectrumMeasurementMode
        {
            get => SpectrumMeasurementMode.Reflective;
            set => throw new NotSupportedException();
        }

        private SpecularComponent _sc;

        public SpecularComponent SpecularComponent
        {
            get { return _sc; }
            set
            {
                byte scMode = (byte)(value.ToInstrumentForm());
                SetByteConfig(CallCode.SET_SC_MODE, scMode);
                _sc = value;
            }
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
            get { return _aperture; }
            set
            {
                var caliber = (byte)value.ToInstrumentForm();
                SetByteConfig(CallCode.SET_CALIBER, caliber);

                _aperture = value;
            }
        }


        private StandardIlluminant _illuminant;

        public StandardIlluminant StandardIlluminant
        {
            get { return _illuminant; }
            set
            {
                var illuminant = (byte)value.ToInstrumentForm();
                SetByteConfig(CallCode.SET_ILLUMINANT, illuminant);

                _illuminant = value;
            }
        }

        private StandardObserver _observer;

        public StandardObserver StandardObserver
        {
            get { return _observer; }
            set
            {
                var observer = (byte)value.ToInstrumentForm();
                SetByteConfig(CallCode.SET_OBSERVER, observer);

                _observer = value;
            }
        }
        private Optical.ColorSpace _colorSpace;
        private ColorDiffFormula _colorDiffFormula;

        public  Optical.ColorSpace ColorSpace
        {
            get { return _colorSpace; }
            set
            {
                var colorSpace = (byte)value.ToInstrumentForm();
                SetByteConfig(CallCode.SET_COLOR_SPACE, colorSpace);

                _colorSpace = value;
            }
        }

        public ColorDiffFormula ColorDiffFormula
        {
            get { return _colorDiffFormula; }
            set
            {
                var formula = (byte)value.ToInstrumentForm();
                SetByteConfig(CallCode.SET_DIFF_FORMAL, formula);

                _colorDiffFormula = value;
            }
        }

        private Optical.ColorIndex _colorIndex;
        private UVFlag _uvMode;

        public Optical.ColorIndex ColorIndex
        {
            get { return _colorIndex; }
            set
            {
                var colorIndex = (byte)value.ToInstrumentForm();
                SetByteConfig(CallCode.SET_COLOR_INDEX, colorIndex);
                _colorIndex = value;
            }
        }

        public UVFlag UvMode
        {
            get { return _uvMode; }
            set
            {
                var uvMode = (byte)value.ToInstrumentForm();
                SetByteConfig(CallCode.SET_UV_MODE, uvMode);
                _uvMode = value;
            }
        }

        public DateTime DateTime
        {
            get
            {
                var bytes = SendPacket(new CmdPacket(CommandType.CALL, (byte)CallCode.GET_DATETIME), 1000);
                if (bytes.Length == Marshal.SizeOf(typeof(RtcDateTime)))
                {
                    var datetime = StructConverter.BytesToStruct<RtcDateTime>(bytes);
                    return datetime.ToDateTime();
                }

                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
            set
            {
                var dateTime = new RtcDateTime(value);
                var bytes = SendPacket(
                    new CmdPacket(CommandType.CALL, (byte)CallCode.SET_DATETIME,
                        StructConverter.StructToBytes(dateTime)),
                    1000);
                var result = Encoding.ASCII.GetString(bytes);
                if (result == "INVAL")
                {
                    throw new CommunicationException(CommunicationError.InvalidParameter);
                }

                if (result != "OK")
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
                }
            }
        }

        public ColorimeterInterface.Tolerance SystemDefaultTolerance
        {
            get
            {
                var bytes = SendPacket(new CmdPacket(CommandType.CALL, (byte)CallCode.GET_TOLERANCE), 1000);
                if (bytes.Length == Marshal.SizeOf(typeof(RtcDateTime)))
                {
                    var tolerance = StructConverter.BytesToStruct<ColorReader.Tolerance>(bytes);
                    return new ColorimeterInterface.Tolerance()
                    {
                        dE = tolerance.dE,
                        dL = new Tuple<double, double>(tolerance.dL_Lower, tolerance.dL_Upper),
                        da = new Tuple<double, double>(tolerance.da_Lower, tolerance.da_Upper),
                        db = new Tuple<double, double>(tolerance.db_Lower, tolerance.db_Upper)
                    };
                }

                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
            set
            {
                var tolerance = new ColorReader.Tolerance()
                {
                    dE = (float)value.dE,
                    da_Lower = (float)value.da.Item1,
                    da_Upper = (float)value.da.Item2,
                    db_Lower = (float)value.db.Item1,
                    db_Upper = (float)value.db.Item2,
                    dL_Lower = (float)value.dL.Item1,
                    dL_Upper = (float)value.dL.Item2
                };
                var bytes = SendPacket(
                    new CmdPacket(CommandType.CALL, (byte)CallCode.SET_TOLERANCE,
                        StructConverter.StructToBytes(tolerance)),
                    1000);
                var result = Encoding.ASCII.GetString(bytes);
                if (result == "INVAL")
                {
                    throw new CommunicationException(CommunicationError.InvalidParameter);
                }

                if (result != "OK")
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
                }
            }
        }

        public CultureInfo Language
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public object GetSetting(string propertyName)
        {
            return typeof(BluetoothDongleSpectrometer).GetProperty(propertyName)?.GetValue(this, null);
        }

        public void SetSetting(string propertyName, object value)
        {
            typeof(BluetoothDongleSpectrometer).GetProperty(propertyName)?.SetValue(this, value, null);
        }

        public bool IsLensSwitchModeModifiable => false;
        public bool IsLensPositionModifiable => false;
        public bool IsMeasurementCaliberModifiable => false;
        public bool IsUvModifiable => false;
        public bool IsSpecularComponentModifiable => true;
        public bool IsSpectrumMeasurementModeModifiable => false;

        public void RefreshCaches()
        {
            try
            {
                Model = GetStringConfig(CallCode.GET_MODEL);
                HardwareRevision = GetStringConfig(CallCode.GET_HARDWARE_REVISION);
                FirmwareRevision = GetStringConfig(CallCode.GET_FIRMWARE_REVISION);
                SoftwareRevision = GetStringConfig(CallCode.GET_SOFTWARE_REVISION);
                Manufacturer = GetStringConfig(CallCode.GET_MANUFACTURER_NAME);
                OpticalStructure = GetStringConfig(CallCode.GET_OPTICAL);
                SN = GetStringConfig(CallCode.GET_SN);
                WhiteBoardNumber = GetStringConfig(CallCode.GET_WHITE_BOARD_SN);
                _aperture = ((Caliber)GetByteConfig(CallCode.GET_CALIBER)).Uniform();
                _sc = ((ScMode)GetByteConfig(CallCode.GET_SC_MODE)).Uniform(); ;
                _uvMode = ((UvMode)GetByteConfig(CallCode.GET_UV_MODE)).Uniform();
                _illuminant = ((Illuminant)GetByteConfig(CallCode.GET_ILLUMINANT)).Uniform(); ;
                _observer = ((Observer)GetByteConfig(CallCode.GET_OBSERVER)).Uniform(); ;
                _colorSpace = ((ColorReader.ColorSpace)GetByteConfig(CallCode.GET_COLOR_SPACE)).Uniform(); ;
                _colorIndex = ((ColorReader.ColorIndex)GetByteConfig(CallCode.GET_COLOR_INDEX)).Uniform(); ;
                _colorDiffFormula = ((ColorReader.ColorDeltaFormula)GetByteConfig(CallCode.GET_DIFF_FORMAL)).Uniform();
                InternalModel = GetInternalModel();

            }
            catch (CommunicationException ex)
            {
                Trace.TraceWarning("RefreshCaches Timeout: {0}", ex);
            }
        }

        
        public void RefreshInstrumentInfo() => RefreshCaches();

        public void RestoreFactorySettings()
        {
            throw new NotImplementedException();
        }

        public void SetInstrumentLanguage(InstrumentLanguage lang)
        {
            throw new NotImplementedException();
        }

        public void SetDateTime(DateTime datetime)
        {
            DateTime = datetime;
        }

        public void StopUpload()
        {
            throw new NotImplementedException();
        }

        public int TotalStandards => GetIntConfig(CallCode.GET_STANDARD_COUNT);

        public int TotalSamples => GetIntConfig(CallCode.GET_TOTAL_SAMPLES);

        public int GetSamplesCount(int standardIndex)
        {
            var bytes = SendPacket(new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.GET_SAMPLE_COUNT,
                BitConverter.GetBytes(standardIndex)), 1000);
            try
            {
                return BitConverter.ToInt32(bytes, 0);
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public int GetSamplesCountById(int standardId)
        {
            UInt32 tmp = (uint)(standardId | 0x80000000);
            var bytes = SendPacket(new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.GET_SAMPLE_COUNT,
                BitConverter.GetBytes(tmp)), 1000);
            try
            {
                return BitConverter.ToInt32(bytes, 0);
            }
            catch
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public StandardSpecimen UploadStandard(int standardIndex)
        {
            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.UPLOAD_STANDARD,
                    BitConverter.GetBytes(standardIndex)), 3000);
            if (bytes.Length == Marshal.SizeOf(typeof(StandardRecord)))
            {
                return StructConverter.BytesToStruct<StandardRecord>(bytes).ToSpecimen();
            }

            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public StandardSpecimen UploadStandardById(int standardId)
        {
            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.UPLOAD_STANDARD,
                    BitConverter.GetBytes((uint)(standardId | 0x80000000))), 3000);
            if (bytes.Length == Marshal.SizeOf(typeof(StandardRecord)))
            {
                return StructConverter.BytesToStruct<StandardRecord>(bytes).ToSpecimen();
            }

            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public StandardSpecimen[] UploadStandards(int startIndex, int count)
        {
            if (count < 0)
            {
                count = TotalStandards - startIndex;
            }

            var buf = new StandardSpecimen[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadStandard(startIndex + i);
            }

            return buf;
        }

        public Specimen UploadSample(int standardIndex, int sampleIndex)
        {
            List<byte> param = new List<byte>();
            param.AddRange(BitConverter.GetBytes(standardIndex));
            param.AddRange(BitConverter.GetBytes(sampleIndex));
            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.UPLOAD_SAMPLE, param.ToArray()), 3000);
            if (bytes.Length == Marshal.SizeOf(typeof(TrialRecord)))
            {
                return StructConverter.BytesToStruct<TrialRecord>(bytes).ToSpecimen();
            }

            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public Specimen UploadSampleById(int standardId, int sampleIndex)
        {
            List<byte> param = new List<byte>();
            param.AddRange(BitConverter.GetBytes((uint)(standardId | 0x80000000)));
            param.AddRange(BitConverter.GetBytes(sampleIndex));
            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.UPLOAD_SAMPLE, param.ToArray()), 3000);
            if (bytes.Length == Marshal.SizeOf(typeof(TrialRecord)))
            {
                return StructConverter.BytesToStruct<TrialRecord>(bytes).ToSpecimen();
            }
            else
            {
                var str = Encoding.ASCII.GetString(bytes);
                Trace.TraceInformation("UploadSampleById Return:\"{0}\"", str);
            }

            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public Specimen[] UploadSamples(int standardIndex, int startSampleIndex, int count)
        {
            if (count < 0)
            {
                count = GetSamplesCount(standardIndex) - startSampleIndex;
            }

            var buf = new Specimen[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadSample(standardIndex, startSampleIndex + i);
            }

            return buf;
        }

        public Specimen[] UploadSamplesById(int standardId, int startSampleIndex, int count)
        {
            if (count < 0)
            {
                count = GetSamplesCountById(standardId) - startSampleIndex;
            }

            var buf = new Specimen[count];
            for (int i = 0; i < count; i++)
            {
                buf[i] = UploadSampleById(standardId, startSampleIndex + i);
            }

            return buf;
        }


        public void UploadStandardAsync(EventHandler<SpecimenReceivedEventArgs> handler, int @from = 0, int count = -1)
        {
            throw new NotImplementedException();
        }

        public void UploadTestAsync(EventHandler<SpecimenReceivedEventArgs> handler, int standard, int @from = 0,
            int count = -1)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllRecords()
        {
            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DELETE_STANDARD,
                    BitConverter.GetBytes(-1)), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void RemoveAllSamples()
        {
            List<byte> param = new List<byte>();
            param.AddRange(BitConverter.GetBytes(-1));
            param.AddRange(BitConverter.GetBytes(-1));

            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DELETE_SAMPLE,
                    param.ToArray()), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void RemoveAllSamples(int standardIndex)
        {
            List<byte> param = new List<byte>();
            param.AddRange(BitConverter.GetBytes(standardIndex));
            param.AddRange(BitConverter.GetBytes(-1));

            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DELETE_SAMPLE,
                    param.ToArray()), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void RemoveAllSamplesById(int standardId)
        {
            List<byte> param = new List<byte>();
            param.AddRange(BitConverter.GetBytes((uint)(0x80000000 | standardId)));
            param.AddRange(BitConverter.GetBytes(-1));

            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DELETE_SAMPLE,
                    param.ToArray()), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void DeleteStandard(int standardIndex)
        {
            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DELETE_STANDARD,
                    BitConverter.GetBytes(standardIndex)), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void DeleteStandardById(int standardId)
        {
            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DELETE_STANDARD,
                    BitConverter.GetBytes((uint)(0x80000000 | standardId))), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void DeleteSample(int standardIndex, int sampleIndex)
        {
            List<byte> param = new List<byte>();
            param.AddRange(BitConverter.GetBytes(standardIndex));
            param.AddRange(BitConverter.GetBytes(sampleIndex));

            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DELETE_SAMPLE,
                    BitConverter.GetBytes(-1)), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void DeleteSampleById(int standardId, int sampleIndex)
        {
            List<byte> param = new List<byte>();
            param.AddRange(BitConverter.GetBytes((uint)(0x80000000 | standardId)));
            param.AddRange(BitConverter.GetBytes(sampleIndex));

            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.DELETE_SAMPLE,
                    param.ToArray()), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void DownloadStandard(Specimen standard)
        {
            var standardRecord = StandardRecord.FromSpecimen(standard);
            var bytes = SendPacket(
                new CmdPacket(CommandType.NOTIFICATION, (byte)NotificationCode.INPUT_STANDARD,
                    StructConverter.StructToBytes(standardRecord)), 5000);
            if (Encoding.ASCII.GetString(bytes) != "OK")
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void DownloadStandards(IEnumerable<Specimen> standards)
        {
            foreach (var standard in standards)
            {
                DownloadStandard(standard);
            }
        }

        public void Backup(StreamWriter stream)
        {
            throw new NotImplementedException();
        }

        public void Restory(StreamReader stream)
        {
            throw new NotImplementedException();
        }

        public string GetPnpId()
        {
            var result = SendPacket(new CmdPacket(CommandType.CALL, (byte)CallCode.GET_PNP_ID), 1000);
            if (result != null)
            {
                return Encoding.ASCII.GetString(result);
            }

            return null;
        }

        public InternalModelType GetInternalModel()
        {
            return (InternalModelType)GetByteConfig(CallCode.GET_INTERNAL_MODEL);
        }


        public InternalModelType InternalModel { get; private set; }

        /// <summary>
        /// 判断是否是ColorReader系列仪器
        /// </summary>
        /// <param name="serialPort">已连接的串口</param>
        /// <returns>如果是ColorReader返回真，否则返回假</returns>
        public static bool IsColorReaderSeries(SerialPort serialPort)
        {
            using (BluetoothDongleSpectrometer s = new BluetoothDongleSpectrometer())
            {
                try
                {
                    s.AttatchSerialPort(serialPort);
                    var pnpId = s.GetPnpId().Trim();
                    s.DetachSerialPort();
                    Trace.TraceInformation("GetPnpID:{0}, LENGTH: {1}", pnpId, pnpId.Length);
                    bool isEqual = string.CompareOrdinal(pnpId, PNP_ID) == 0;
                    Trace.TraceInformation("{0} {1} {2}", pnpId, isEqual ? "==" : "!=", PNP_ID);
                    return isEqual;
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("IsColorReaderSeries: {0}", ex.Message);
                    if (s.SerialPort != null)
                    {
                        s.DetachSerialPort();
                    }
                    return false;
                }
            }
        }

        #region IMetrology接口的实现
        public List<float> GetMeasureSpectrumEight(int numbers = 8)
        {
            var result = SendPacket(new CmdPacket(CommandType.CALL, (byte)MetrologyCode.ReadPharmaceuticalConfig), 1000);
            List<float> list = new List<float>();
            if (result != null&&result.Count()>0)
            {
                foreach (var item in result)
                {
                    list.Add(item);
                }
                return list;
            }
            return null;
        }

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
                var result = SendPacket(new CmdPacket(CommandType.NOTIFICATION, (byte)MetrologyCode.GetKmtcMode, packet.Bytes), 1000);
                if (Encoding.ASCII.GetString(result) != "OK")
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("SVD写入失败：{0}", Encoding.ASCII.GetString(result)));
                }
                Thread.Sleep(200);
            }
            return true;
        }

        public bool SetOLSR(List<List<float>> datas)
        {
            throw new NotImplementedException();
        }

        public List<List<float>> ReadSVDs(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            throw new NotImplementedException();
        }

        public List<float> ReadSVDList(MeasureType? measureType = null, int rows = 31, int cols = 16)
        {
            throw new NotImplementedException();
        }

        public List<float> ReadOLSRList(MeasureType? measureType = null)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}