using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommunicationBase;
using ThreeNH.Optical;
using ThreeNH.ColorAlgorithm;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using ThreeNH.DataModel;
using ThreeNH.StructHelper;
using DateTime = System.DateTime;
using ColorDiffFormula = ThreeNH.Optical.ColorDiffFormula;
using ColorSpace = ThreeNH.Optical.ColorSpace;
using MeasurementCaliber = ThreeNH.Optical.MeasurementCaliber;
using SpecularComponent = ThreeNH.Optical.SpecularComponent;
using StandardIlluminant = ThreeNH.Optical.StandardIlluminant;
using StandardObserver = ThreeNH.Optical.StandardObserver;
using ThreeNH.Helper;
using System.Linq;
using ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer.Instrument;

namespace ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer
{
    public class Spectrometer : InstrumentBase, IColorimeter, IMetrology, ISpecimenClearing, IRecordManagement, ISystemSetting, ITargetLibraryManagement, IMeasurementNotification
    {
        public static readonly WavelengthRange WAVELENGTHRANGE =
            new WavelengthRange(400, 700, 10);

        //EventHandler<SpecimenReceivedEventArgs> UploadingArrivedHandler;
        int _stopUpload = 0;    // 停止正在进行的异步上传

        //public Spectrometer()
        //{
        //}

        Parser _packetParser = new Parser();
        protected override PacketParser PacketParser
        {
            get
            {
                return _packetParser;
            }
        }

        protected override void OnPacketArrived(DataPacket packet, Exception ex)
        {
            switch ((Command)packet.Command)
            {
                case Command.Measure:
                    OnMeasurePackedReceived(packet, ex);
                    break;
                case Command.SpecimenAutoUpload:
                    OnMeasurePackedReceived(packet, ex);
                    break;
                case Command.BluetoothExit:
                    OnMeasurePackedReceived(packet, ex);
                    break;
                //case Command.SpecimenPacket:
                //    OnSpecimenPacketArrived(packet, ex);
                //    break;
                default:
                    break;
            }
        }

        void OnMeasurePackedReceived(DataPacket packet, Exception ex)
        {
            if ((Command)packet.Command == Command.BluetoothExit)
            {
                MeasurementReceived?.Invoke(this, null);
            }
            else
            {
                if (packet != null)
                {
                    try
                    {
                        Specimen specimen = DequeueMeasureResult(packet);
                        MeasurementReceived?.Invoke(this, new MeasurementNotificationArgs(specimen));
                    }
                    catch
                    {

                    }
                }
            }

        }

        //        public MeasurementCaliber MeasurementCaliber
        //        {
        //            get; private set;
        //        }
        //        
        public InternalModelType InternalModelType { get; set; }

        public string SN
        {
            get; private set;
        }

        private Optical.SpecularComponent _specularComponent = Optical.SpecularComponent.SCIAndSCE;
        public SpectrumMeasurementMode SpectrumMeasurementMode
        {
            get { return SpectrumMeasurementMode.Reflective; }
            set { throw new NotSupportedException(); }
        }

        public Optical.SpecularComponent SpecularComponent
        {
            get { return _specularComponent; }
            set
            {
                MeasurementParameter para = GetMeasurementParameter();
                para.SpecularComponent = value;
                SetMeasurementParameter(para);
                _specularComponent = value;
            }
        }

        public LensSwitchMode LensSwitchMode
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public LensPosition LensPosition
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        private Optical.MeasurementCaliber _aperture;
        public Optical.MeasurementCaliber Aperture
        {
            get { return _aperture; }

            set
            {
                MeasurementParameter para = GetMeasurementParameter();
                para.MeasurementCaliber = value;
                SetMeasurementParameter(para);
                _aperture = value;
            }
        }

        public Optical.StandardIlluminant StandardIlluminant
        {
            get { return _standardIlluminant; }
            set
            {
                MeasurementParameter para = GetMeasurementParameter();
                para.PrimaryIlluminant = value;
                SetMeasurementParameter(para);
                _standardIlluminant = value;
            }
        }

        public StandardObserver StandardObserver
        {
            get { return _standardObserver; }
            set
            {
                MeasurementParameter para = GetMeasurementParameter();
                para.Observer = value;
                SetMeasurementParameter(para);
                _standardObserver = value;
            }
        }

        public ColorSpace ColorSpace
        {
            get { return _colorSpace; }
            set
            {
                var opt = GetChromaticityOperations();
                opt.ColorSpace = value;
                SetChromaticityOperations(opt);
                _colorSpace = value;
            }
        }

        public ColorDiffFormula ColorDiffFormula
        {
            get { return _colorDiffFormula; }
            set
            {
                var opt = GetChromaticityOperations();
                opt.ColorDiffFormula = value;
                SetChromaticityOperations(opt);
                _colorDiffFormula = value;
            }
        }

        public ColorIndex ColorIndex
        {
            get { return _colorIndex; }
            set { _colorIndex = value; }
        }

        public UVFlag UvMode
        {
            get { return _uvMode; }
            set
            {
                if (value != UVFlag.None && value != UVFlag.On)
                    throw new InvalidOperationException();
                MeasurementParameter para = GetMeasurementParameter();
                para.UV = value;
                SetMeasurementParameter(para);
                _uvMode = value;
            }
        }
        public DateTime DateTime
        {
            get { throw new NotSupportedException(); }
            set { SetDateTime(value); }
        }
        private Tolerance _defaultTolerance;
        public ColorimeterInterface.Tolerance SystemDefaultTolerance
        {
            get
            {
                var t = _defaultTolerance;
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
                _defaultTolerance = t;
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

        public bool IsSpecularComponentModifiable { get; private set; } = true;
        public bool IsSpectrumMeasurementModeModifiable
        {
            get { return false; }
        }

        private UVFlag _uv = UVFlag.None;
        private StandardObserver _standardObserver;
        private Optical.StandardIlluminant _standardIlluminant;
        private UVFlag _uvMode;
        private Optical.ColorSpace _colorSpace;
        private ColorDiffFormula _colorDiffFormula;
        private ColorIndex _colorIndex;

        public string Model { get; private set; }

        public string HardwareRevision { get; private set; }
        public string FirmwareRevision { get; private set; }
        public string SoftwareRevision
        {
            get { return FirmwareRevision; }
        }
        public string Manufacturer { get; }
        public string OpticalStructure { get; private set; }

        public string WhiteBoardNumber
        {
            get; private set;
        }

        public DeviceType DeviceType
        {
            get { return DeviceType.Spectrophotometer; }
        }

        public WavelengthRange WavelengthRange
        {
            get { return WAVELENGTHRANGE; }
            set { throw new NotImplementedException(); }
        }

        public void Connect()
        {
            Connect(null);
        }

        public override void Connect(object device)
        {
            try
            {
                base.Connect(device);
                //SayHello();

                RefreshInstrumentInfo();
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }
        }

        public override void Disconnect()
        {
            try
            {
                base.Disconnect();
            }
            catch
            {

            }
            if (IsOpen)
            {
                SayGoodbye();
                base.Disconnect();
            }
        }


        internal ColorData WSCI { get; set; }

        internal ColorData WSCE { get; set; }
        /// <summary>
        /// 初始化白板参数(8度光泽度需要白板反射率)
        /// </summary>
        private void InitializeWhiteBoardData()
        {
            if (!IsOpen)
                return;
            var whiteBoardParameter = GetWhiteBoardParameter();
            if (whiteBoardParameter.SCI != null)
                WSCI = new ColorData(whiteBoardParameter.SCI);
            if (whiteBoardParameter.SCE != null)
                WSCE = new ColorData(whiteBoardParameter.SCE);
        }

        private int? CalculateEightDegreesGloss(ColorData sCI, ColorData sCE)
        {
            if (WSCI == null || WSCE == null || sCI == null || sCE == null)
                return null;
            if (WSCI.Spectrum == null || WSCE.Spectrum == null || sCI.Spectrum == null || sCE.Spectrum == null)
                return null;
            return ColorAlgorithm.Formula.CalculateEightDegreesGloss(sCI.Spectrum, sCE.Spectrum, WSCI.Spectrum, WSCE.Spectrum);
            // return AlgorithmExtension.clralgo_8_gloss_data(sCI.Spectrum, sCE.Spectrum, WSCI.Spectrum, WSCE.Spectrum);
        }
        /// <summary>
        /// 与仪器握手，告短上位机的SpecularComponent和UV设置
        /// </summary>
        private void SayHello()
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)1);
            SampleDataFlags flags = new SampleDataFlags();
            flags.SpecularComponent = SpecularComponent;
            flags.UVFlag = UvMode;
            content.Enqueue(flags.LowByte);

            SendCommand((byte)Command.SayHello, content);
        }
        /// <summary>
        /// 告诉仪器准备断开连接
        /// </summary>
        private void SayGoodbye()
        {
            try
            {
                PacketContent content = new PacketContent();
                content.Enqueue((ushort)0);
                SendCommand((byte)Command.SayHello, content);
            }
            catch
            {
                // 使么都不做
            }
        }

        public void DoBlackCalibration()
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)Instrument.CalibrationType.Black);
            var result = SendCommand((byte)Command.Calibrate, content);
            if (result == null || !result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.CalibrationFailed,
                    string.Format("Black calibration failed: {0}", result.Ack));
            }
        }

        public bool IsCalibrated
        {
            get
            {
                var devInfo = GetDeviceInfo();
                return devInfo.BlackCalibration && devInfo.WhiteCalibration;
            }
        }

        public void DoWhiteCalibration()
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)Instrument.CalibrationType.White);
            var result = SendCommand((byte)Command.Calibrate, content);

            if (result == null || !result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.CalibrationFailed,
                    string.Format("White calibration failed: {0}", result.Ack));
            }
        }

        /// <summary>
        /// 判断光谱反射率是否有效
        /// </summary>
        /// <param name="spectrum">要判断的光谱反射率</param>
        /// <param name="isUvOn">UV是否开启</param>
        /// <returns>如果sepctrum为null或是反射率数据在有效区间内返回真</returns>
        private bool IsSpectrumValid(Spectrum spectrum, bool isUvOn = true)
        {
            if (spectrum == null)
                return true;

            double up = isUvOn ? 300.0 : 150.0;
            foreach (var r in spectrum.Data)
            {
                if (r < 0.0 || r > up)
                    return false;
            }
            return true;
        }

        public Specimen Measure()
        {
#if false
            var result = SendCommand((byte)Command.Measure);

            if (result != null && result.IsAckOk)
            {
                return DequeueMeasureResult(result);
            }
                        return null;
#endif
            //满足计量工具SCE计量
            return MeasureStandard();

        }

        public Specimen Measure(SpecularComponent sc, UVFlag uv)
        {
            PacketContent content = new PacketContent();
            SampleDataFlags flags = new SampleDataFlags();
            flags.SpecularComponent = sc;
            flags.UVFlag = uv;
            content.Enqueue(flags.LowByte);

            var result = SendCommand((byte)Command.Measure, content);

            if (result != null && result.IsAckOk)
            {
                return DequeueMeasureResult(result);
            }
            return null;
        }

        public Specimen MeasureStandard()
        {
            return Measure(SpecularComponent.SCIAndSCE, this.UvMode);//20191225 lilin change to comment Measure();
        }

        public Specimen MeasureSample()
        {
            return Measure(this.SpecularComponent, this.UvMode);//20191225 lilin change to comment Measure();
        }

        private Specimen DequeueMeasureResult(DataPacket result)
        {
            ushort tmp;
            result.PacketContent.Dequeue(out tmp);

            SampleDataFlags flags = new SampleDataFlags(tmp);
            Console.WriteLine("{0} {1} {2}", tmp, flags.HasSCI, flags.HasSCE);
            Specimen specimen = new Specimen();

            if (flags.HasSCI)
            {
                Spectrum sciSpectrum = new Spectrum(WAVELENGTHRANGE);
                result.PacketContent.Dequeue(sciSpectrum);
                specimen.SCI = new ColorData(sciSpectrum);
            }
            if (flags.HasSCE)
            {
                Spectrum sceSpectrum = new Spectrum(WAVELENGTHRANGE);
                result.PacketContent.Dequeue(sceSpectrum);
                specimen.SCE = new ColorData(sceSpectrum);
            }

            //45度测光方式不分SCI/SCE
            if (String.Compare(OpticalStructure, "45/0", StringComparison.OrdinalIgnoreCase) == 0 ||
                String.Compare(OpticalStructure, "0/45", StringComparison.OrdinalIgnoreCase) == 0)
            {
                specimen.Clr = specimen.SCI;
                specimen.SCI = null;
                specimen.SCE = null;
            }

            specimen.DateTime = DateTime.Now;
            specimen.MeasurementCaliber = Aperture;// flags.MeasurementCaliber;
            specimen.UV = flags.UVFlag;
            specimen.Instrument = Model;
            specimen.OpticalStruct = OpticalStructure;
            specimen.InstrumentSN = SN;
            specimen.Gloss8 = CalculateEightDegreesGloss(specimen.SCI, specimen.SCE);
            return specimen;
        }
        private Specimen DequeueMeasureExResult(DataPacket result)
        {
            ushort tmp;
            result.PacketContent.Dequeue(out tmp);
            SampleDataFlags flags = new SampleDataFlags(tmp);
            Console.WriteLine("{0} {1} {2}", tmp, flags.HasSCI, flags.HasSCE);
            Specimen specimen = new Specimen();
            if (flags.HasSCI)
            {
                Spectrum sciSpectrum = new Spectrum(WAVELENGTHRANGE);
                result.PacketContent.Dequeue(sciSpectrum);
                specimen.SCI = new ColorData(sciSpectrum);
            }
            if (flags.HasSCE)
            {
                Spectrum sceSpectrum = new Spectrum(WAVELENGTHRANGE);
                result.PacketContent.Dequeue(sceSpectrum);
                specimen.SCE = new ColorData(sceSpectrum);
            }

            //45度测光方式不分SCI/SCE
            if (String.Compare(OpticalStructure, "45/0", StringComparison.OrdinalIgnoreCase) == 0 ||
                String.Compare(OpticalStructure, "0/45", StringComparison.OrdinalIgnoreCase) == 0)
            {
                specimen.Clr = specimen.SCI;
                specimen.SCI = null;
                specimen.SCE = null;
            }

            specimen.DateTime = DateTime.Now;
            specimen.MeasurementCaliber =  Aperture;// flags.MeasurementCaliber;
            specimen.UV = flags.UVFlag;
            specimen.Instrument = Model;
            specimen.OpticalStruct = OpticalStructure;
            specimen.InstrumentSN = SN;
            specimen.Tag = result.PacketContent.DequeueArray(result.PacketContent.DequeueFloat, 8);
            return specimen;
        }


        public DeviceInfo GetDeviceInfo()
        {
            var result = SendCommand((byte)Command.GetDeviceInfo);
            if (result != null && result.IsAckOk)
            {
                DeviceInfo info;
                result.PacketContent.Dequeue(out info);
                return info;
            }
            return null;
        }

        public VersionInfo GetVersionInfo()
        {
            var devInfo = GetDeviceInfo();

            return new VersionInfo
            {
                HardwareVersion = devInfo.HardwareVersion,
                ModelName = devInfo.ModelName,
                SN = devInfo.SN,
                SoftwareVersion = devInfo.SoftwareVersion
            };
        }

        public void SetVersionInfo(VersionInfo info)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(info.HardwareVersion);
            content.Enqueue(info.SoftwareVersion);
            content.Enqueue(info.SN);
            content.Enqueue(info.ModelName);

            var result = SendCommand((byte)Command.SetVersionInfo, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    "Set version info failed: " + result.Ack.ToString());
            }
        }
        public ushort[] ReadWaveLengths()
        {
            var result = SendCommand((byte)Command.GetWaveLengths);

            if (result != null && result.IsAckOk)
            {
                return result.PacketContent.DequeueArray(result.PacketContent.DequeueUInt16, 8);
            }
            return null;
        }
        public float[] ReadYSWaveLengthOffset()
        {
            var result = SendCommand((byte)Command.GetYSWaveLengthOffset);

            if (result != null && result.IsAckOk)
            {
                return result.PacketContent.DequeueArray(result.PacketContent.DequeueFloat, 2);
            }
            return null;
        }

        public void ResetWaveLengths()
        {
            var result = SendCommand((byte)Command.DefaultWaveLengths);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }
        public void ResetNotOffsetWaveLengths()
        {
            var result = SendCommand((byte)Command.SetYSNotOffset);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }
        public void SetWaveLengths(ushort[] datas)
        {
            PacketContent content = new PacketContent();
            foreach (var data in datas)
            {
                content.Enqueue(data);
            }
            var result = SendCommand((byte)Command.SetWaveLengths, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    "Set version info failed: " + result.Ack.ToString());
            }
        }

        public void SetYSWaveLengthsOffset(float[] datas)
        {
            PacketContent content = new PacketContent();
            foreach (var data in datas)
            {
                content.Enqueue(data);
            }
            var result = SendCommand((byte)Command.SetYSWaveLengthOffset, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    "Set version info failed: " + result.Ack.ToString());
            }
        }
        public bool SetSVD(List<List<float>> datas, MeasureType? measureType = null, byte caliber = 0)
        {
            if (datas == null) return false;
            var rows = datas.Count;
            var cols = datas[0].Count;
            for (int i = 0; i < rows; i++)
            {
                PacketContent content = new PacketContent();
                byte index = (byte)i;
                content.Enqueue(index);
                foreach (var data in datas[i])
                {
                    content.Enqueue(data);
                }
                var result = SendCommand((byte)Command.WriteSVD, content);
                if (!result.IsAckOk)
                {
                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("SVD写入失败：{0}", result.Ack));
                }
            }
            return true;
        }

        public List<List<float>> ReadSVDs(MeasureType? measureType = null, int rows = 31, int cols = 8)
        {
            float[,] arry2D = new float[rows, 8];
            for (int i = 0; i < rows; i++)
            {
                PacketContent content = new PacketContent();
                content.Enqueue((byte)i);
                var result = SendCommand((byte)Command.ReadSVD, content);
                if (result != null && result.IsAckOk)
                {
                    byte tmp;
                    result.PacketContent.Dequeue(out tmp);
                    if (tmp == (byte)i)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            float tmp2;
                            result.PacketContent.Dequeue(out tmp2);
                            arry2D[i, j] = tmp2;
                        }
                    }
                }
            }
            return Enumerable.Range(0, arry2D.GetLength(0)).Select(row => Enumerable.Range(0, arry2D.GetLength(1)).Select(col => arry2D[row, col]).ToList()).ToList();
        }

        public bool ResetSVD()
        {
            var result = SendCommand((byte)Command.ResetSVD);
            return result.IsAckOk;
        }
        public List<float> GetMeasureSpectrumEight(int numbers = 8)
        {
            PacketContent content = new PacketContent();
            var result = SendCommand((byte)Command.ReadSpectrumEight, content, null, true, 20000);
            List<float> data = new List<float>();
            if (result != null && result.IsAckOk)
            {
                for (int j = 0; j < numbers; j++)
                {
                    float tmp2;
                    result.PacketContent.Dequeue(out tmp2);
                    data.Add(tmp2);
                }
                return data;
            }
            return null;

        }
        public Specimen MeasureEx()
        {
            PacketContent content = new PacketContent();
            SampleDataFlags flags = new SampleDataFlags();
            flags.SpecularComponent = SpecularComponent.SCI;
            flags.UVFlag = this.UvMode;
            content.Enqueue(flags.LowByte);

            var result = SendCommand((byte)Command.MeasureEx, content);

            if (result != null && result.IsAckOk)
            {
                return DequeueMeasureExResult(result);
            }
            return null;
        }

        /// <summary>
        /// KS系列仪器的型号以KS-、PS5或PS4开头
        /// </summary>
        /// <returns></returns>
        public bool IsKSSeries()
        {
            return Model != null && (Model.StartsWith("KS-", StringComparison.OrdinalIgnoreCase)
                || Model.StartsWith("PS5", StringComparison.OrdinalIgnoreCase)
                || Model.StartsWith("PS4", StringComparison.OrdinalIgnoreCase));
        }

        public MeasurementParameter GetMeasurementParameter()
        {
            var result = SendCommand((byte)Command.GetMeasurementParamter);
            if (result != null && result.IsAckOk)
            {
                MeasurementParameter para;
                if (IsKSSeries())
                {
                    result.PacketContent.Dequeue(true, out para);
                }
                else
                {
                    result.PacketContent.Dequeue(false, out para);
                }
                return para;
            }
            return null;
        }

        public void SetMeasurementParameter(MeasurementParameter para)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(para, IsKSSeries());
            var result = SendCommand((byte)Command.SetMeasurementParameter, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    string.Format("Execute se measurement parameter command failed: {0}", result.Ack));
            }
        }

        /// <summary>
        /// 获取存样个数
        /// </summary>
        /// <returns></returns>
        public int GetTargetCount()
        {
            ushort from = 0;
            ushort count = 0xFFFF;
            GetSpecimens(Instrument.SampleType.Target, 0, ref from, ref count);
            return count;
        }



        public void ClearLibrary()
        {
            DeleteSpecimens(SampleType.Target);
        }

        /// <summary>
        /// 获取标样的个数
        /// </summary>
        /// <returns>标样个数</returns>
        public int GetStandardCount()
        {
            ushort from = 0;
            ushort count = 0xFFFF;
            GetSpecimens(Instrument.SampleType.Standard, 0, ref from, ref count);
            return count;
        }

        /// <summary>
        /// 获取给定标样下的试样个数
        /// </summary>
        /// <param name="standard">标样索引</param>
        /// <returns>返回试样个数</returns>
        public int GetTestCount(int standard)
        {
            if (standard < 0)
            {
                DeviceInfo info = GetDeviceInfo();
                return info.TestCount;
            }

            ushort from = 0;
            ushort count = 0xFFFF;
            GetSpecimens(Instrument.SampleType.Test, (ushort)standard, ref from, ref count);
            return count;
        }

        /// <summary>
        /// 上传标样
        /// </summary>
        /// <param name="from">要上传的标样的序号，从0开始</param>
        /// <param name="count">要上传的个数；如果为-1，上传from到最后一个标样</param>
        /// <returns>返回上传的标样</returns>
        public Specimen[] GetStandards(int from = 0, int count = -1)
        {
            int totalCount = GetStandardCount();
            ushort hfrom = (ushort)from;
            ushort hcount = (count > 0) ? (ushort)count : (ushort)(totalCount - from);
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return GetSpecimens(Instrument.SampleType.Standard, 0, ref hfrom, ref hcount);
        }

        /// <summary>
        /// 上传试样
        /// </summary>
        /// <param name="standard">所属标样的索引</param>
        /// <param name="from">起始序号，从0开始</param>
        /// <param name="count">上传的个数</param>
        /// <returns>返回上传的数据</returns>
        public Specimen[] GetTests(int standard, int from = 0, int count = -1)
        {
            int totalCount = GetTestCount(standard);
            ushort hfrom = (ushort)from;
            ushort hcount = (count > 0) ? (ushort)count : (ushort)(totalCount - from);
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return GetSpecimens(Instrument.SampleType.Test, (ushort)standard, ref hfrom, ref hcount);
        }

        /// <summary>
        /// 上传存样
        /// </summary>
        /// <param name="from">起始序号，从0开始</param>
        /// <param name="count">上传的个数</param>
        /// <returns>返回上传的数据</returns>
        public Specimen[] ReadTargets(int from = 0, int count = -1)
        {
            int totalCount = GetTargetCount();
            ushort hfrom = (ushort)from;
            ushort hcount = (count > 0) ? (ushort)count : (ushort)(totalCount - from);
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            var targets = GetSpecimens(Instrument.SampleType.Target, 0, ref hfrom, ref hcount);
            if (hcount >= 0 && targets == null || targets.Length != hcount)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            return targets;
        }

        /// <summary>
        /// 上传样品数据
        /// </summary>
        /// <param name="type">要上传的样品类型</param>
        /// <param name="standard">如果上传的是试样，为标样索引，指示要上传哪个标样下的试样；否则忽略</param>
        /// <param name="from">样品的起始位置，从0开始；在返回null时返回有效的起始位置</param>
        /// <param name="count">上传的样品个数；在返回null时返回有效的样品个数</param>
        /// <returns>如果给定的索引或个数超出范围，返回空；如果成功返回上传的样品</returns>
        Specimen[] GetSpecimens(Instrument.SampleType type, ushort standard, ref ushort from, ref ushort count)
        {

            PacketContent content = new PacketContent();
            content.Enqueue((byte)type);
            content.Enqueue(standard);
            content.Enqueue(from);
            content.Enqueue(count);
            var result = SendCommand((byte)Command.UploadSpecimen, content);

            // 如果执行失败，会返回实际的样品起始位置和数量
            if (!result.IsAckOk)
            {
                byte sampleType;
                result.PacketContent.Dequeue(out sampleType);
                result.PacketContent.Dequeue(out standard);
                result.PacketContent.Dequeue(out from);
                result.PacketContent.Dequeue(out count);
                return null;
            }

            List<Specimen> list = new List<Specimen>();
            // 告诉仪器上传数据
            result = SendCommand((byte)Command.TransportSpecimen);
            byte endFlag = 0;
            ushort seq = 0;
            while (endFlag == 0)
            {
                if (result == null || result.PacketContent == null)
                {
                    break;
                }

                result.PacketContent.Dequeue(out seq);
                result.PacketContent.Dequeue(out endFlag);

                try
                {
                    DequeueSpecimens(result, list, type == Instrument.SampleType.Standard);
                }
                catch (Exception ex)
                {
                    SendCommand((byte)Command.StopTransportSpecimen, waitingAck: false);

                    // 打印完整异常链
                    Trace.TraceError("上传样品解析数据出错");
                    for (var e = ex; e != null; e = e.InnerException)
                    {
                        Trace.TraceError("  [{0}] {1}", e.GetType().Name, e.Message);
                        Trace.TraceError("  {0}", e.StackTrace);
                    }

                    // 打印出错时包里还剩多少字节、前 64 字节 HEX
                    if (result?.PacketContent != null)
                    {
                        var bytes = result.PacketContent.Bytes;
                        Trace.TraceError("剩余字节数: {0}, seq={1}, endFlag={2}, 已解析样品数={3}",
                            bytes.Length, seq, endFlag, list.Count);
                        Trace.TraceError("剩余HEX: {0}",
                            string.Join(" ", bytes.Take(64).Select(b => b.ToString("X2"))));
                    }

                    throw new CommunicationException(CommunicationError.ParsedBadData, innerException: ex);
                }

                if (endFlag == 0)
                {
                    // 发送应答，表示已接收到数据，请求继续传输
                    content.Clear();
                    content.Enqueue(seq);
                    result = SendCommand((byte)Command.SpecimenPacket, content, DataPacket.AckOk);
                }
            }
            return list.ToArray();
        }

        private void DequeueSpecimens(DataPacket result, List<Specimen> list, bool isStd)
        {
            int sampleIndex = 0;
            while (result.PacketContent.Length > 0)
            {
                sampleIndex++;
                int remainBefore = (int)result.PacketContent.Length;
                Trace.TraceInformation("[Specimen] 开始解析第 {0} 个样品, 剩余 {1} 字节",
                    sampleIndex, remainBefore);

                Trace.TraceInformation("[Specimen] 头部HEX: {0}",
            string.Join(" ", result.PacketContent.Bytes.Take(32).Select(b => b.ToString("X2"))));

                try
                {
                    SampleData sample = null;
                    result.PacketContent.Dequeue(out sample);

                    Trace.TraceInformation("[Specimen] 第 {0} 个 OK: flags=0x{1:X4}, index={2}, name={3}, standard=0x{4:X4}, 剩余={5}",
                sampleIndex, sample.flags, sample.index, sample.name, sample.standard,
                result.PacketContent.Length);

                    Specimen specimen = null;
                    if (isStd)
                    {
                        DataModel.StandardSpecimen standard = new DataModel.StandardSpecimen();
                        if (sample.tolerance != null)
                        {

                            var tolerance = new DataModel.Tolerance(
                                new DataModel.ChromaticityField[]
                                {
                                DataModel.ChromaticityField.dL,
                                DataModel.ChromaticityField.da,
                                DataModel.ChromaticityField.db,
                                DataModel.ChromaticityField.dEab,
                                    //DataModel.ChromaticityField.dE94,
                                    //DataModel.ChromaticityField.dE00,
                                    //DataModel.ChromaticityField.dEcmc
                                });

                            Func<double, double, bool> isLimitValid = (lower, upper) => lower < upper;

                            tolerance[DataModel.ChromaticityField.dL] =
                                new DataModel.ToleranceItem
                                {
                                    Lower = sample.tolerance.dL_Lower,
                                    Upper = sample.tolerance.dL_Upper,
                                    UseForJudgement = isLimitValid(sample.tolerance.dL_Lower, sample.tolerance.dL_Upper)
                                };
                            tolerance[DataModel.ChromaticityField.da] =
                                new DataModel.ToleranceItem
                                {
                                    Lower = sample.tolerance.da_Lower,
                                    Upper = sample.tolerance.da_Upper,
                                    UseForJudgement = isLimitValid(sample.tolerance.da_Lower, sample.tolerance.da_Upper)
                                };
                            tolerance[DataModel.ChromaticityField.db] =
                                new DataModel.ToleranceItem
                                {
                                    Lower = sample.tolerance.db_Lower,
                                    Upper = sample.tolerance.db_Upper,
                                    UseForJudgement = isLimitValid(sample.tolerance.db_Lower, sample.tolerance.db_Upper)
                                };
                            var dE = new DataModel.ToleranceItem
                            {
                                Upper = sample.tolerance.dE,
                                UseForJudgement = sample.tolerance.dE > 0
                            };
                            tolerance[DataModel.ChromaticityField.dEab] = dE;

                            standard.ToleranceSetting = new DataModel.ToleranceSetting();
                            standard.ToleranceSetting.PrimaryTolerance = tolerance;
                            standard.ToleranceSetting.SecondaryTolerance = new DataModel.Tolerance(tolerance);
                            standard.ToleranceSetting.TertiaryTolerance = new DataModel.Tolerance(tolerance);
                        }

                        specimen = standard;
                    }
                    else
                    {
                        specimen = new Specimen();
                    }

                    SampleDataFlags flags = new SampleDataFlags(sample.flags);
                    if (OpticalStructure == "45/0")
                    {
                        specimen.Clr = sample.sci;
                    }
                    else
                    {
                        specimen.SCI = sample.sci;
                        specimen.SCE = sample.sce;
                    }
                    specimen.OpticalStruct = OpticalStructure;


                    specimen.Name = sample.name;
                    specimen.DateTime = sample.dateTime.ToSystemDateTime();
                    specimen.ID = sample.index;
                    // KS/PS 口径编码与常规机不同，按型号解析才能导出正确口径
                    specimen.MeasurementCaliber = flags.GetMeasurementCaliber(IsKSSeries());
                    specimen.UV = flags.UVFlag;
                    specimen.Instrument = Model;
                    specimen.InstrumentSN = SN;

                    list.Add(specimen);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[Specimen] 第 {0} 个失败, 剩余={1}, offset≈{2}",
                sampleIndex, result.PacketContent.Length, remainBefore - result.PacketContent.Length);
                    Trace.TraceError("[Specimen] 异常: {0}", ex);
                    throw;
                }
               
            }
        }

        void ReceiveSpecimensThreadFunc(object obj)
        {
            object[] parameters = (object[])obj;
            EventHandler<SpecimenReceivedEventArgs> specimenReceivedHandler = (EventHandler<SpecimenReceivedEventArgs>)parameters[0];
            Instrument.SampleType sampleType = (Instrument.SampleType)parameters[1];

            try
            {
                var result = SendCommand((byte)Command.TransportSpecimen);
                byte endFlag = 0;
                ushort seq = 0;
                while (endFlag == 0)
                {
                    result.PacketContent.Dequeue(out seq);
                    result.PacketContent.Dequeue(out endFlag);
                    List<Specimen> list = new List<Specimen>();
                    DequeueSpecimens(result, list, sampleType == Instrument.SampleType.Standard);
                    specimenReceivedHandler?.Invoke(this, new SpecimenReceivedEventArgs(list.ToArray(), endFlag == 0));

                    if (_stopUpload != 0)
                    {
                        SendCommand((byte)Command.StopTransportSpecimen, waitingAck: false);
                        break;
                    }

                    if (endFlag == 0)
                    {
                        // 发送应答，表示已接收到数据，请求继续传输
                        PacketContent content = new PacketContent();
                        content.Enqueue(seq);
                        result = SendCommand((byte)Command.SpecimenPacket, content, DataPacket.AckOk);
                    }
                }

            }
            catch (Exception ex)
            {
                specimenReceivedHandler?.Invoke(this, new SpecimenReceivedEventArgs(ex));
            }
        }

        void UploadSpecimensAsync(EventHandler<SpecimenReceivedEventArgs> handler, Instrument.SampleType type,
            ushort standard, ushort from, ushort count)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)type);
            content.Enqueue(standard);
            content.Enqueue(from);
            content.Enqueue(count);
            var result = SendCommand((byte)Command.UploadSpecimen, content);
            if (result == null || !result.IsAckOk)
                return;

            _stopUpload = 0;
            var thread = new Thread(ReceiveSpecimensThreadFunc);
            thread.Start(new object[] { handler, type });
        }

        public void UploadStandardAsync(EventHandler<SpecimenReceivedEventArgs> handler, int from = 0, int count = -1)
        {
            int totalCount = GetStandardCount();
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            ushort hfrom = (ushort)from;
            ushort hcount = (count != -1) ? (ushort)count : (ushort)(totalCount - from);

            UploadSpecimensAsync(handler, Instrument.SampleType.Standard,
                0, hfrom, hcount);
        }

        public void UploadTestAsync(EventHandler<SpecimenReceivedEventArgs> handler, int standard, int from = 0, int count = -1)
        {
            int totalCount = GetTestCount(standard);
            if ((from + count) > totalCount || count == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            ushort hfrom = (ushort)from;
            ushort hcount = (count != -1) ? (ushort)count : (ushort)(totalCount - from);

            UploadSpecimensAsync(handler, Instrument.SampleType.Test,
                (ushort)standard, hfrom, hcount);
        }

        public void StopUpload()
        {
            Interlocked.Exchange(ref _stopUpload, 1);
        }

        protected override byte AckCommand(byte command)
        {
            switch ((Command)command)
            {
                case Command.TransportSpecimen: return (byte)Command.SpecimenPacket;
                case Command.StopTransportSpecimen: return 0;
                case Command.SayHello: return 0;
                case Command.GetMotorDuration: return 212;
                default: return command;
            }
        }

        public void DeleteAllSpecimens()
        {
            DeleteSpecimens(Instrument.SampleType.Standard);
        }

        public void DeleteTests(int standard)
        {
            DeleteSpecimens(Instrument.SampleType.Test, standard);
        }

        private void DeleteSpecimens(Instrument.SampleType type, int standard = -1)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)type);
            content.Enqueue((ushort)standard);
            var result = SendCommand((byte)Command.DeleteSpecimen, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    string.Format("Delete {0} failed: {1}", type, result.Ack));
            }
        }

        /// <summary>
        /// 获取白板参数
        /// </summary>
        /// <returns>返回白板参数</returns>
        public WhiteBoardParameter GetWhiteBoardParameter()
        {
            var result = SendCommand((byte)Command.ReadWhiteBoardData);
            if (result.IsAckOk)
            {
                WhiteBoardParameter para = new WhiteBoardParameter();

                string strValue;
                result.PacketContent.Dequeue(out strValue);
                para.InnerNumber = strValue;
                result.PacketContent.Dequeue(out strValue);
                para.OuterNumber = strValue;

                Spectrum spectrum = new Spectrum(new WavelengthRange(400, 700, 10));
                result.PacketContent.Dequeue(spectrum);
                for (int i = 0; i < spectrum.Data.Length; i++)
                    spectrum.Data[i] /= 100.0;
                para.SCI = spectrum;

                spectrum = new Spectrum(new WavelengthRange(400, 700, 10));
                result.PacketContent.Dequeue(spectrum);
                for (int i = 0; i < spectrum.Data.Length; i++)
                    spectrum.Data[i] /= 100.0;
                para.SCE = spectrum;

                return para;
            }

            throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                string.Format("Get white board parameter failed: {0}", result.Ack));
        }

        /// <summary>
        /// 设置白板参数
        /// </summary>
        /// <param name="para">白板参数</param>
        public void SetWhiteBoardParameter(WhiteBoardParameter para)
        {
            //发送SCI数据
            PacketContent content = new PacketContent();
            content.Enqueue((byte)0);
            content.Enqueue(para.InnerNumber);
            content.Enqueue(para.OuterNumber);
            foreach (var x in para.SCI.Data)
            {
                content.Enqueue((float)(x * 100.0));
            }
            var result = SendCommand((byte)Command.WriteWhiteBoardData, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(
                    CommunicationError.ExecuteCommandFailed,
                    string.Format("Write white board parameter failed: {0}", result.Ack));
            }

            // 发送SCE反射率
            content.Clear();
            content.Enqueue((byte)1);
            content.Enqueue(para.InnerNumber);
            content.Enqueue(para.OuterNumber);
            foreach (var x in para.SCE.Data)
            {
                content.Enqueue((float)(x * 100.0));
            }
            result = SendCommand((byte)Command.WriteWhiteBoardData, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(
                    CommunicationError.ExecuteCommandFailed,
                    string.Format("Write white board parameter failed: {0}", result.Ack));
            }
        }

        private static void EnqueueXyz(PacketContent content, CIEXYZ xyz,
            Optical.StandardIlluminant illuminant, Optical.StandardObserver observer)
        {
            List<byte> byteList = new List<byte>();
            byteList.Add((byte)observer.ToInstrumentForm());
            byteList.Add((byte)illuminant.ToInstrumentForm());
            byteList.AddRange(((float)xyz.X).ToNetworkOrderBytes());
            byteList.AddRange(((float)xyz.Y).ToNetworkOrderBytes());
            byteList.AddRange(((float)xyz.Z).ToNetworkOrderBytes());

            int totalLength = WAVELENGTHRANGE.Count * sizeof(float);
            for (int i = 0; i < totalLength; i++)
            {
                if (i < byteList.Count)
                    content.Enqueue(byteList[i]);
                else
                    content.Enqueue((byte)0);
            }
        }

        /// <summary>
        /// 将样品写入存样库
        /// </summary>
        /// <param name="target">要写入的样品</param>
        public void WriteToLibrary(Specimen target)
        {
            WriteStandard(target, true);
        }


        public void WriteToLibrary(IEnumerable<Specimen> targets)
        {
            foreach (var target in targets)
            {
                WriteToLibrary(target);
            }
        }

        public void WriteStandard(Specimen standard, bool toTargetLibrary = false)
        {
            Formula[] sciFormulas = standard.SCI?.Formulas;
            Formula[] sceFormulas = standard.SCE?.Formulas;
            Formula[] clrFormulas = standard.Clr?.Formulas;

            // 检查参数是不有效
            if ((sciFormulas != null && sciFormulas.Length != 1) ||
                (sceFormulas != null && sceFormulas.Length != 1))
            {
                throw new CommunicationException(CommunicationError.InvalidParameter,
                        string.Format("SCI or SCE has multiple color data"));
            }
            if (standard.SpecularComponent == SpecularComponent.SCIAndSCE)
            {
                if (!((sciFormulas == null && sceFormulas == null) ||
                    (sciFormulas != null && sceFormulas != null &&
                    sciFormulas[0].Observer == sceFormulas[0].Observer &&
                    sciFormulas[0].Illuminant == sceFormulas[0].Illuminant)))
                {
                    throw new CommunicationException(CommunicationError.InvalidParameter,
                        string.Format("SCI and SCE must have same data type"));
                }
            }

            SampleDataFlags flags = new SampleDataFlags();
            flags.UVFlag = standard.UV;
            // 回写仪器：KS/PS 须用 ToInstrumentFormForKS，否则特殊口径无法正确落盘
            flags.SetMeasurementCaliber(standard.MeasurementCaliber, IsKSSeries());
            flags.HasTolerance = true;
            flags.SpecularComponent = standard.SpecularComponent;
            if (toTargetLibrary)
            {
                flags.IsTargetColor = true;
            }

            if (standard.GetSpectrum(SpecularComponent.SCI) != null
                || standard.GetSpectrum(SpecularComponent.SCE) != null
                || standard.GetSpectrum(SpecularComponent.Unknown) != null)
            {
                flags.IsReflectanceFromUser = true;
            }
            else
            {
                flags.IsXyz = true;
            }

            // 仪器中标样只能存储9个字符
            byte[] name = new byte[9];
            byte[] bytes = Encoding.ASCII.GetBytes(standard.Name);
            for (int i = 0; i < 8; i++)
            {
                name[i] = i < bytes.Length ? bytes[i] : (byte)0;
            }
            name[8] = 0;

            // 如果標樣數據同時有SCI和SCE分兩次發送
            // 否則一次發送完所有數據

            PacketContent content = new PacketContent();

            content.Enqueue((byte)0);
            content.Enqueue(flags.Flags);
            content.Enqueue(name);

            if (flags.IsReflectance)
            {
                if (standard.SpecularComponent == SpecularComponent.SCI ||
                    standard.SpecularComponent == SpecularComponent.SCIAndSCE)
                {
                    content.Enqueue(standard.GetSpectrum(SpecularComponent.SCI));
                }
                else if (standard.SpecularComponent == SpecularComponent.Unknown)
                {
                    content.Enqueue(standard.GetSpectrum(SpecularComponent.Unknown));
                }
                else //if (standard.SpecularComponent == SpecularComponent.SCE)
                {
                    content.Enqueue(standard.GetSpectrum(SpecularComponent.SCE));
                }
            }
            else
            {
                if (standard.SpecularComponent == SpecularComponent.SCI ||
                    standard.SpecularComponent == SpecularComponent.SCIAndSCE)
                {
                    EnqueueXyz(content, standard.SCI.XYZ(sciFormulas[0]),
                        sciFormulas[0].Illuminant, sciFormulas[0].Observer);
                }
                else if (standard.SpecularComponent == SpecularComponent.Unknown)
                {
                    EnqueueXyz(content, standard.Clr.XYZ(clrFormulas[0]),
                        clrFormulas[0].Illuminant, clrFormulas[0].Observer);
                }
                else //if (standard.SpecularComponent == SpecularComponent.SCE)
                {
                    EnqueueXyz(content, standard.SCE.XYZ(sceFormulas[0]),
                        sceFormulas[0].Illuminant, sceFormulas[0].Observer);
                }
            }

            if ((standard as StandardSpecimen)?.ToleranceSetting?.PrimaryTolerance != null)
            {
                var tolance = (standard as StandardSpecimen).ToleranceSetting.PrimaryTolerance;
                content.Enqueue((float)tolance[ChromaticityField.dEab].Upper.GetValueOrDefault(1.0));
                content.Enqueue((float)tolance[ChromaticityField.dL].Lower.GetValueOrDefault(1.0));
                content.Enqueue((float)tolance[ChromaticityField.dL].Upper.GetValueOrDefault(1.0));
                content.Enqueue((float)tolance[ChromaticityField.da].Lower.GetValueOrDefault(1.0));
                content.Enqueue((float)tolance[ChromaticityField.da].Upper.GetValueOrDefault(1.0));
                content.Enqueue((float)tolance[ChromaticityField.db].Lower.GetValueOrDefault(1.0));
                content.Enqueue((float)tolance[ChromaticityField.db].Upper.GetValueOrDefault(1.0));
            }
            else
            {
                content.Enqueue(1.0f);
                content.Enqueue(-1.0f);
                content.Enqueue(1.0f);
                content.Enqueue(-1.0f);
                content.Enqueue(1.0f);
                content.Enqueue(-1.0f);
                content.Enqueue(1.0f);
            }

            var result = SendCommand((byte)Command.WriteStandard, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(
                    CommunicationError.ExecuteCommandFailed,
                    string.Format("[0]Write standard failed: {0}", result.Ack));
            }

            // 如果同時有SCI和SCE數據,接著發送SCE的反射數據
            if (standard.SpecularComponent == SpecularComponent.SCIAndSCE)
            {
                content.Clear();
                content.Enqueue((byte)1);
                content.Enqueue(flags.Flags);
                if (flags.IsReflectance)
                {
                    content.Enqueue(standard.GetSpectrum(SpecularComponent.SCE));
                }
                else
                {
                    EnqueueXyz(content, standard.SCE.XYZ(sceFormulas[0]),
                       sceFormulas[0].Illuminant, sceFormulas[0].Observer);
                }

                result = SendCommand((byte)Command.WriteStandard, content);

                if (!result.IsAckOk)
                {
                    throw new CommunicationException(
                        CommunicationError.ExecuteCommandFailed,
                        string.Format("[1]Write standard failed: {0}", result.Ack));
                }
            }
        }

        public void WriteStandards(IEnumerable<Specimen> standards)
        {
            foreach (var standard in standards)
            {
                WriteStandard(standard);
            }
        }


        public Tolerance GetTolerance()
        {
            ChromaticityOperations operations = GetChromaticityOperations();
            return operations.Tolerance;
        }

        public void SetTolerance(Tolerance tolerance)
        {
            ChromaticityOperations operations = GetChromaticityOperations();
            operations.Tolerance = tolerance;
            SetChromaticityOperations(operations);
        }

        public ChromaticityOperations GetChromaticityOperations()
        {
            var result = SendCommand((byte)Command.GetChromaticitySetting);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(
                    CommunicationError.ExecuteCommandFailed,
                    string.Format("Get chromaticity operations failed: {0}", result.Ack));
            }
            ChromaticityOperations operations = null;
            result.PacketContent.Dequeue(out operations);
            return operations;
        }

        public void SetChromaticityOperations(ChromaticityOperations operations)
        {
            var content = new PacketContent();
            content.Enqueue(operations);
            var result = SendCommand((byte)Command.SetChromaticitySetting, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(
                    CommunicationError.ExecuteCommandFailed,
                    string.Format("Set chromaticity operations failed: {0}", result.Ack));
            }
        }

        /// <summary>
        /// 写宾松光栅感应器的系数
        /// </summary>
        /// <param name="major">主感应器系数，大小为6的数组</param>
        /// <param name="minor">辅感应器的系数，大小为6的数组</param>
        public void SetRasterSensorCoefficients(double[] major, double[] minor)
        {
            if (major == null || minor == null)
            {
                throw new ArgumentNullException();
            }
            if (major.Length != 6 || minor.Length != 6)
            {
                throw new ArgumentException("major.Length and minor.Length must be 6");
            }

            PacketContent content = new PacketContent();
            for (int i = 0; i < 6; i++)
                content.Enqueue((float)major[i]);
            for (int i = 0; i < 6; i++)
                content.Enqueue((float)minor[i]);
            var result = SendCommand((byte)Command.SetRasterSensorCoefficients, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    string.Format("设置宾松光栅感应器系数失败：{0}", result.Ack));
            }
        }

        /// <summary>
        /// 获取宾松光栅感应器的系数
        /// </summary>
        /// <param name="major">返回主感应器的系数</param>
        /// <param name="minor">返回辅感应器的系数</param>
        public void GetRasterSensorCoefficients(out double[] major, out double[] minor)
        {
            var result = SendCommand((byte)Command.GetRasterSensorCoefficients);
            if (result.IsAckOk)
            {
                major = new double[6];
                minor = new double[6];

                float tmp;
                for (int i = 0; i < 6; i++)
                {
                    result.PacketContent.Dequeue(out tmp);
                    major[i] = tmp;
                }
                for (int i = 0; i < 6; i++)
                {
                    result.PacketContent.Dequeue(out tmp);
                    minor[i] = tmp;
                }
            }
            else
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("获取宾松光栅感应器系数失败：{0}", result.Ack));
            }
        }

        /// <summary>
        /// 设置仪器日期时间
        /// </summary>
        /// <param name="datetime">日期时间</param>
        public void SetDateTime(DateTime datetime)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)datetime.Hour);
            content.Enqueue((byte)datetime.Minute);
            content.Enqueue((byte)(datetime.Year - 2000));
            content.Enqueue((byte)datetime.Month);
            content.Enqueue((byte)datetime.Day);
            var result = SendCommand((byte)Command.SetDateTime, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("Set date time failed: {0}", result.Ack));
            }
        }


        public void RefreshInstrumentInfo()
        {
            Trace.TraceInformation("准备获取仪器信息...");
            DeviceInfo info = GetDeviceInfo();
            Model = info.ModelName;
            Trace.TraceInformation("准备获取测量参数...");
            MeasurementParameter para = GetMeasurementParameter();
            _aperture = Swith_KS(para.MeasurementCaliber);
            _specularComponent = para.SpecularComponent;
            _standardIlluminant = para.PrimaryIlluminant;
            _standardObserver = para.Observer;
            _uvMode = para.UV;

            //            _colorIndex = para.

            WhiteBoardNumber = info.WhiteBoardOuterNumber;
            SN = info.SN;
            FirmwareRevision = info.SoftwareVersion;
            HardwareRevision = info.HardwareVersion;
            OpticalStructure = info.OpticalStruct;
            Trace.TraceInformation("刷新设备信息成功");

            // 临时错施，用于判断仪器口径,(也是临时措施,用于判断设备内部型号,如果设备名称跟设备内部型号不一致，那就没有办法)
            if (info.ModelName.ToUpper() == "YS4580")
            {
                _aperture = MeasurementCaliber.Phi7; // 20mm
            }
            else if (info.ModelName.ToUpper() == "YS4510")
            {
                if (_aperture == MeasurementCaliber.Phi1By3)
                {
                    _aperture = MeasurementCaliber.Phi2; // 2mm
                }
            }
            if (info.OpticalStruct == "45/0")
            {
                _specularComponent = SpecularComponent.Unknown;
                IsSpecularComponentModifiable = false;
                InternalModelType = InternalModelType.YS45XX;
            }
            else
            {
                IsSpecularComponentModifiable = true;
                InternalModelType = InternalModelType.YS30XX;
            }
            InitializeWhiteBoardData();
        }

        public void RefreshCaches()
        {
            RefreshInstrumentInfo();
            var chromaticityOperations = GetChromaticityOperations();
            _defaultTolerance = chromaticityOperations.Tolerance;
            _colorSpace = chromaticityOperations.ColorSpace;
            _colorDiffFormula = chromaticityOperations.ColorDiffFormula;
        }

        /// <summary>
        /// KS系列需要转换口径
        /// </summary>
        /// <param name="aperture"></param>
        /// <returns></returns>
        private MeasurementCaliber Swith_KS(MeasurementCaliber aperture)
        {
            if (IsKSSeries())
            {
                switch (aperture)
                {
                    case MeasurementCaliber.Unknown:
                        return MeasurementCaliber.Phi11;
                    case MeasurementCaliber.Phi4:
                        return MeasurementCaliber.Phi11 + 1;
                    case MeasurementCaliber.Phi8:
                        return MeasurementCaliber.Phi11 + 2;
                    case MeasurementCaliber.Phi15:
                        return MeasurementCaliber.Phi11 + 3;
                    case MeasurementCaliber.Phi1in:
                        return MeasurementCaliber.Phi11 + 4;
                    default:
                        return aperture;
                }
            }
            return aperture;
        }

        public void RestoreFactorySettings()
        {
            var result = SendCommand((byte)Command.RestoreFactorySettings);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public void SetInstrumentLanguage(InstrumentLanguage lang)
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)lang);
            var result = SendCommand((byte)Command.SetLanguage, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public bool GetInstrumentModel(out ushort model, out short subModel)
        {
            var result = SendCommand((byte)Command.GetModel);
            if (!result.IsAckOk)
            {
                model = 0;
                subModel = 0;
                return false;
            }
            result.PacketContent.Dequeue(out model);
            result.PacketContent.Dequeue(out subModel);
            return true;
        }

        public void SetInstrumentModel(ushort model, ushort subModel)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(model);
            content.Enqueue(subModel);
            var result = SendCommand((byte)Command.SetModel, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }




        /// <summary>
        /// 获取蓝牙名字
        /// </summary>
        /// <returns>反回蓝牙名字</returns>
        public string GetBluetoothName()
        {
            var result = SendCommand((byte)Command.GetBluetoothName);
            if (result.IsAckOk)
            {
                return result.PacketContent.DequeueString();
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }
        /// <summary>
        /// 设置蓝牙名字
        /// </summary>
        /// <param name="newName">新的蓝牙名字</param>
        public void SetBluetoothName(string newName)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(newName);
            var result = SendCommand((byte)Command.SetBluetoothName);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        /// <summary>
        /// 获取蓝牙配对码
        /// </summary>
        /// <returns>蓝牙配对码</returns>
        public string GetBluetoothPairingCode()
        {
            var result = SendCommand((byte)Command.GetBluetoothPairingCode);
            if (result.IsAckOk)
            {
                return result.PacketContent.DequeueString();
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        /// <summary>
        /// 设置蓝牙配对码
        /// </summary>
        /// <param name="pairingCode"></param>
        public void SetBluetoothPairingCode(string pairingCode)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(pairingCode);
            var result = SendCommand((byte)Command.SetBluetoothPairingCode);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }


        // 获取积分时间
        public ushort GetIntegrationTime()
        {
            var result = SendCommand((byte)Command.GetIntegrationTime);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            return result.PacketContent.DequeueUInt16();
        }

        public byte GetIntegrationTimeNRH()
        {
            var result = SendCommand((byte)Command.GetIntegration);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            return result.PacketContent.DequeueByte();
        }

        public void SetIntegrationTimeNRH(byte time)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(time);
            var result = SendCommand((byte)Command.SetIntegration, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        public byte GetGain()
        {
            var result = SendCommand((byte)Command.GetGain);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
            return result.PacketContent.DequeueByte();
        }

        public void SetGain(byte time)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(time);
            var result = SendCommand((byte)Command.SetGain, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }



        // 设置积分时间
        public void SetIntegrationTime(ushort time)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(time);
            var result = SendCommand((byte)Command.SetIntegrationTime, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        /// <summary>
        /// 采样
        /// </summary>
        /// <returns>采样数据</returns>
        public SamplingData TakeSample()
        {
            var result = SendCommand((byte)Command.TakeSample);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            var data = result.PacketContent;
            var samplingData = new SamplingData();
            for (int i = 0; i < 256; i++)
            {
                samplingData.MajorLightOn[i] = data.DequeueUInt16();
            }
            for (int i = 0; i < 256; i++)
            {
                samplingData.MinorLightOn[i] = data.DequeueUInt16();
            }
            for (int i = 0; i < 256; i++)
            {
                samplingData.MajorLightOff[i] = data.DequeueUInt16();
            }
            for (int i = 0; i < 256; i++)
            {
                samplingData.MinorLightOff[i] = data.DequeueUInt16();
            }

            return samplingData;
        }

        /// <summary>
        /// 获取采样结果
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CommunicationException"></exception>
        public SamplingDataResult GetSamplingDataResult()
        {
            var result = SendCommand((byte)Command.SamplingDataResult);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            var data = result.PacketContent;
            var dataResult = new SamplingDataResult();
            dataResult.Result = data.DequeueByte();
            dataResult.Small = data.DequeueUInt16();
            dataResult.Normal = data.DequeueUInt16();
            dataResult.Large = data.DequeueUInt16();
            dataResult.Huge = data.DequeueUInt16();
            return dataResult;
        }

        public bool AutoRegulate()
        {
            var result = SendCommand((byte)Command.AutoRegulate);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
            var data = result.PacketContent;
            return data.DequeueByte() == 0x01;

        }
        /// <summary>
        /// 旋转马达
        /// </summary>
        /// <param name="direction">方向，可以为SCI或SCE</param>
        /// <param name="durationTime">持续时间</param>
        public void RotateMotor(SpecularComponent direction, ushort durationTime)
        {
            if (direction != SpecularComponent.SCI && direction != SpecularComponent.SCE)
            {
                throw new CommunicationException(CommunicationError.InvalidParameter);
            }
            PacketContent content = new PacketContent();
            content.Enqueue((byte)(direction == SpecularComponent.SCI ? 0 : 1));
            content.Enqueue(durationTime);
            var result = SendCommand((byte)Command.RotateMotor, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        // 设置SCI/SCE切换时马达转动时间
        public void SetMotorDuration(ushort durationTime)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(durationTime);
            var result = SendCommand((byte)Command.SetMotorDuration, content);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }
        }

        // 获取马达转动持续时间
        public ushort GetMotorDuration()
        {
            var result = SendCommand((byte)Command.GetMotorDuration);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
            }

            return result.PacketContent.DequeueUInt16();
        }

        public void Open(object commDevice = null)
        {
            Connect(commDevice);
        }

        public void Close()
        {
            Disconnect();
        }

        public int TotalStandards
        {
            get { return GetDeviceInfo().StandardCount; }
        }
        public int TotalSamples
        {
            get { return GetDeviceInfo().TestCount; }
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
            return GetStandards(standardIndex, 1)[0] as StandardSpecimen;
        }

        public StandardSpecimen UploadStandardById(int standardId)
        {
            throw new NotImplementedException();
        }

        public StandardSpecimen[] UploadStandards(int startIndex, int count)
        {
            return Array.ConvertAll(GetStandards(startIndex, count), _ => (StandardSpecimen)_);
        }


        public Specimen UploadSample(int standardIndex, int sampleIndex)
        {
            throw new NotImplementedException();
        }

        public Specimen UploadSampleById(int standardId, int sampleIndex)
        {
            return GetTests(standardId, sampleIndex, 1)[0];
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
            DeleteAllSpecimens();
        }

        public void RemoveAllSamples()
        {
            DeleteTests(-1);
        }

        public void RemoveAllSamples(int standardIndex)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllSamplesById(int standardId)
        {
            DeleteTests(standardId);
        }

        public void DeleteStandard(int standardIndex)
        {
            throw new NotImplementedException();
        }

        public void DeleteStandardById(int standardId)
        {
            throw new NotImplementedException();
        }

        public void DeleteSample(int standardIndex, int sampleIndex)
        {
            throw new NotImplementedException();
        }

        public void DeleteSampleById(int standardIndex, int sampleId)
        {
            throw new NotImplementedException();
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

        public event EventHandler<MeasurementNotificationArgs> MeasurementReceived;

        #region Optional Settings //Added by Liguanghai 20/03/27


        public IEnumerable<ValueDescription> OptionalMeasurementCalibers
        {
            get
            {
                List<MeasurementCaliber> list = new List<MeasurementCaliber>();
                switch (Model)
                {
                    case "PS500":
                    case "PS410":
                    case "PS411":
                    case "PS420":
                    case "PS430":
                    case "KS-100":
                    case "KS-200":
                    case "KS-220":
                    case "KS-210":
                    case "KS-500":
                    case "KS-510":
                    case "KS-520":
                        list.Add(MeasurementCaliber.Phi11);
                        list.Add(MeasurementCaliber.Phi10);
                        list.Add(MeasurementCaliber.Phi6);
                        list.Add(MeasurementCaliber.Phi5);
                        list.Add(MeasurementCaliber.Phi3);
                        list.Add(MeasurementCaliber.Phi1By3);
                        break;
                    default:
                        list.Add(MeasurementCaliber.Phi4);
                        list.Add(MeasurementCaliber.Phi8);
                        list.Add(MeasurementCaliber.Phi1By3);
                        break;
                }

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
                //return null;
            }
        }

        public IEnumerable<ValueDescription> OptionalLensPositions
        {
            get
            {
                //List<LensPosition> list = new List<LensPosition>();
                //switch (InternalModelType)
                //{
                //    case InternalModelType.YS30XX:
                //        list.Add(LensPosition.Pos4);
                //        break;
                //    case InternalModelType.YS45XX:
                //        list.Add(LensPosition.Pos8);
                //        list.Add(LensPosition.Pos4);
                //        break;
                //}
                //return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
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
                switch (InternalModelType)
                {
                    case InternalModelType.YS30XX:
                        list.Add(SpecularComponent.SCI);
                        list.Add(SpecularComponent.SCE);
                        list.Add(SpecularComponent.SCIAndSCE);
                        break;
                    case InternalModelType.YS45XX:
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
                //List<SpectrumMeasurementMode> list = new List<SpectrumMeasurementMode>();
                //list.Add(SpectrumMeasurementMode.Reflective);
                //list.Add(SpectrumMeasurementMode.Transmissive);
                //return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
                return null;
            }
        }

        public IEnumerable<ValueDescription> OptionalUVFlags
        {
            get
            {
                List<UVFlag> list = new List<UVFlag>();
                switch (InternalModelType)
                {
                    case InternalModelType.YS30XX:
                        list.Add(UVFlag.None);
                        list.Add(UVFlag.On);
                        break;
                    case InternalModelType.YS45XX:
                        list.Add(UVFlag.None);
                        list.Add(UVFlag.On);
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
                switch (InternalModelType)
                {
                    case InternalModelType.YS30XX:
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
                    case InternalModelType.YS45XX:
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
                switch (InternalModelType)
                {
                    case InternalModelType.YS30XX:
                        list.Add(StandardObserver.CIE1931);
                        list.Add(StandardObserver.CIE1964);
                        break;
                    case InternalModelType.YS45XX:
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
                List<Optical.ColorSpace> list = new List<Optical.ColorSpace>();
                switch (InternalModelType)
                {
                    case InternalModelType.YS30XX:
                        list.Add(Optical.ColorSpace.CIE_LAB);
                        list.Add(Optical.ColorSpace.CIE_LCH);
                        list.Add(Optical.ColorSpace.CIE_XYZ);
                        list.Add(Optical.ColorSpace.CIE_Yxy);
                        list.Add(Optical.ColorSpace.CIE_LUV);
                        list.Add(Optical.ColorSpace.HUNTER_LAB);
                        list.Add(Optical.ColorSpace.sRGB);
                        break;
                    case InternalModelType.YS45XX:
                        list.Add(Optical.ColorSpace.CIE_LAB);
                        list.Add(Optical.ColorSpace.CIE_LCH);
                        list.Add(Optical.ColorSpace.CIE_XYZ);
                        list.Add(Optical.ColorSpace.CIE_Yxy);
                        list.Add(Optical.ColorSpace.CIE_LUV);
                        list.Add(Optical.ColorSpace.HUNTER_LAB);
                        list.Add(Optical.ColorSpace.sRGB);
                        list.Add(Optical.ColorSpace.BETA_xy);
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
                switch (InternalModelType)
                {
                    case InternalModelType.YS30XX:
                        list.Add(ColorDiffFormula.CIE1976);
                        list.Add(ColorDiffFormula.CIE1994);
                        list.Add(ColorDiffFormula.DHunterE);
                        list.Add(ColorDiffFormula.CMC11);
                        list.Add(ColorDiffFormula.CMC21);
                        list.Add(ColorDiffFormula.CMC);
                        list.Add(ColorDiffFormula.CIE2000);
                        break;
                    case InternalModelType.YS45XX:
                        list.Add(ColorDiffFormula.CIE1976);
                        list.Add(ColorDiffFormula.CIE1994);
                        list.Add(ColorDiffFormula.DHunterE);
                        list.Add(ColorDiffFormula.CMC11);
                        list.Add(ColorDiffFormula.CMC21);
                        list.Add(ColorDiffFormula.CMC);
                        list.Add(ColorDiffFormula.CIE2000);
                        break;
                }
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorIndexs
        {
            get
            {
                //List<Optical.ColorIndex> list = new List<Optical.ColorIndex>();
                //switch (InternalModelType)
                //{
                //    case InternalModelType.YS30XX:
                //        list.Add(Optical.ColorIndex.None);
                //        list.Add(Optical.ColorIndex.Yellowness);
                //        list.Add(Optical.ColorIndex.Whiteness);
                //        list.Add(Optical.ColorIndex.Opacity);
                //        list.Add(Optical.ColorIndex.Strength);
                //        list.Add(Optical.ColorIndex.ColorFastness);
                //        list.Add(Optical.ColorIndex.StainingFastness);
                //        list.Add(Optical.ColorIndex.MetamerismIndiex);
                //        break;
                //    case InternalModelType.YS45XX:
                //        list.Add(Optical.ColorIndex.None);
                //        list.Add(Optical.ColorIndex.Yellowness);
                //        list.Add(Optical.ColorIndex.Whiteness);
                //        list.Add(Optical.ColorIndex.Opacity);
                //        list.Add(Optical.ColorIndex.Strength);
                //        list.Add(Optical.ColorIndex.ColorFastness);
                //        list.Add(Optical.ColorIndex.StainingFastness);
                //        list.Add(Optical.ColorIndex.MetamerismIndiex);

                //        break;
                //}
                //return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
                return null;
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
        #endregion


    }
}
