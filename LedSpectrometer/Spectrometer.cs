using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommunicationBase;
using ThreeNH.Optical;
using System.Linq;
using ThreeNH.Helper;
using ThreeNH.DataModel;
using System.IO;


namespace ThreeNH.LedSpectrometer
{
    /// <summary>
    /// NS810、NS800的通讯接口: InstrumentBase, IColorimeter, IMetrology, ISpecimenClearing, IInstrument
    /// </summary>
    public class Spectrometer : InstrumentBase, IColorimeter, IMetrology, ISpecimenClearing,  IRecordManagement, ISystemSetting
    {
        // 支持的波长范围
        public static readonly WavelengthRange WAVELENGTHRANGE =
            new WavelengthRange(400, 700, 10);
        int _stopUpload = 0;    // 停止异𨓿上传标志

        public Spectrometer()
        {

        }

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

                RefreshInstrumentInfo();
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }
        }

        public LensPosition LensPosition
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public MeasurementCaliber Aperture
        {
            get { return _aperture; }
            set
            {
                var param = GetMeasurementParameter();
                param.MeasurementCaliber = value;
                SetMeasurementParameter(param);
                _aperture = value;
            }
        }

        public StandardIlluminant StandardIlluminant
        {
            get { return _standardIlluminant; }
            set
            {
                var param = GetMeasurementParameter();
                param.PrimaryIlluminant = value;
                SetMeasurementParameter(param);
                _standardIlluminant = value;
            }
        }

        public StandardObserver StandardObserver
        {
            get { return _standardObserver; }
            set
            {
                var param = GetMeasurementParameter();
                param.Observer = value;
                SetMeasurementParameter(param);
                _standardObserver = value;
            }
        }

        public ColorSpace ColorSpace
        {
            get { return _colorSpace; }
            set
            {
                var c = GetChromaticitySetting();
                c.ColorSpace = value;
                SetChromaticitySetting(c);
                _colorSpace = value;
            }
        }

        public ColorDiffFormula ColorDiffFormula
        {
            get { return _colorDiffFormula; }
            set
            {
                var c = GetChromaticitySetting();
                c.ColorDiffFormula = value;
                SetChromaticitySetting(c);
                _colorDiffFormula = value;
            }
        }

        public ColorIndex ColorIndex
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        public UVFlag UvMode
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        public DateTime DateTime
        {
            get { throw new NotSupportedException(); }
            set { SetDateTime(value); }
        }

        public ThreeNH.ColorimeterInterface.Tolerance SystemDefaultTolerance
        {
            get { return _tolerance; }
            set
            {
                var c = GetChromaticitySetting();
                c.Tolerance = value;
                SetChromaticitySetting(c);
                _tolerance = value;
            }
        }
        public CultureInfo Language
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        public object GetSetting(string propertyName)
        {
            return typeof(Spectrometer).GetProperty(propertyName)?.GetValue(this, null);
        }

        public void SetSetting(string propertyName, object value)
        {
            typeof(Spectrometer).GetProperty(propertyName)?.SetValue(this, value, null);
        }

        public bool IsUvModifiable { get; }


        private SpecularComponent _specularComponent = SpecularComponent.SCE;
        private MeasurementCaliber _aperture;
        private StandardIlluminant _standardIlluminant;
        private StandardObserver _standardObserver;
        private ColorSpace _colorSpace;
        private ColorDiffFormula _colorDiffFormula;

        public SpectrumMeasurementMode SpectrumMeasurementMode { get; set; }

        public SpecularComponent SpecularComponent
        {
            get { return _specularComponent; }
            set { throw new NotSupportedException(); }
        }

        public LensSwitchMode LensSwitchMode { get; set; }

        public UVFlag UV
        {
            get { return UVFlag.None; }
            set { throw new NotSupportedException(); }
        }

        public WavelengthRange WavelengthRange
        {
            get
            {
                return WAVELENGTHRANGE;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsSpecularComponentModifiable
        {
            get
            {
                return false;
            }
        }

        public bool IsSpectrumMeasurementModeModifiable
        {
            get { return false; }
        }
        public void RefreshCaches()
        {
            RefreshInstrumentInfo();
            var c = GetChromaticitySetting();
            _colorSpace = c.ColorSpace;
            _colorDiffFormula = c.ColorDiffFormula;
            _tolerance = c.Tolerance;
        }


        public string Model { get; private set; }
        public string SN { get; private set; }
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

        public Specimen Measure()
        {
            PacketContent content = new PacketContent();
            content.Enqueue((byte)0);

            var result = SendCommand((byte)Command.Measure, content);

            if (result != null && result.IsAckOk)
            {
                float L, a, b, C, h;
                // 弹出LabCH
                result.PacketContent.Dequeue(out L);
                result.PacketContent.Dequeue(out a);
                result.PacketContent.Dequeue(out b);
                result.PacketContent.Dequeue(out C);
                result.PacketContent.Dequeue(out h);

                Spectrum spectrum = new Spectrum(WAVELENGTHRANGE);
                result.PacketContent.Dequeue(spectrum);
                ColorData color = new ColorData(spectrum);

                Specimen specimen = new Specimen();

                if (SpecularComponent == SpecularComponent.SCI)
                    specimen.SCI = color;
                else if (SpecularComponent == SpecularComponent.SCE)
                    specimen.SCE = color;
                else
                    specimen.Clr = color;
                specimen.DateTime = DateTime.Now;
                specimen.MeasurementCaliber = Aperture;
                specimen.UV = UVFlag.None;
                specimen.Instrument = Model;
                specimen.OpticalStruct = OpticalStructure;

                return specimen;
            }
            return null;
        }

        public Specimen MeasureStandard()
        {
            return Measure();
        }

        public Specimen MeasureSample()
        {
            return Measure();
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

        public MeasurementParameter GetMeasurementParameter()
        {
            var result = SendCommand((byte)Command.GetMeasurementParamter);
            if (result != null && result.IsAckOk)
            {
                MeasurementParameter para;
                result.PacketContent.Dequeue(out para);
                return para;
            }
            return null;
        }

        public bool SetMeasurementParameter(MeasurementParameter parameter)
        {
            PacketContent content = new PacketContent();
            content.Enqueue(parameter);

            var result = SendCommand((byte)Command.SetMeasurementParameter);
            return (result != null && result.IsAckOk);
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

            if ((hfrom + hcount) > totalCount || hcount == 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            var specimens = GetSpecimens(Instrument.SampleType.Standard, 0, ref hfrom, ref hcount);
            List<StandardSpecimen> list = new List<StandardSpecimen>();
            foreach (var item in specimens)
            {
                list.Add(new StandardSpecimen(item));
            }
            return list.ToArray();
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

            if ((hfrom + hcount) > totalCount || hcount == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return GetSpecimens(Instrument.SampleType.Test, (ushort)standard, ref hfrom, ref hcount);
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
            if (result == null)
                return null;

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
                DequeueSpecimens(result, list);

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

        private void DequeueSpecimens(DataPacket result, List<Specimen> list)
        {
            while (result.PacketContent.Length > 0)
            {
                Instrument.SampleData sample = new Instrument.SampleData();
                result.PacketContent.Dequeue(sample);

                Specimen specimen = new Specimen();

                if (sample.SpecularComponent == SpecularComponent.SCI)
                    specimen.SCI = new ColorData(sample.Spectrum);
                else if (sample.SpecularComponent == SpecularComponent.SCE)
                    specimen.SCE = new ColorData(sample.Spectrum);
                else
                    specimen.Clr = new ColorData(sample.Spectrum);
                specimen.ID = sample.Index;
                specimen.Name = sample.Name;
                specimen.DateTime = sample.Time.ToSystemDateTime();
                specimen.MeasurementCaliber = Aperture;
                specimen.Instrument = Model;
                specimen.OpticalStruct = OpticalStructure;


                list.Add(specimen);
            }
        }


        void ReceiveSpecimensThreadFunc(object obj)
        {
            object[] parameters = (object[])obj;
            EventHandler<SpecimenReceivedEventArgs> specimenReceivedHandler = (EventHandler<SpecimenReceivedEventArgs>)parameters[0];

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
                    DequeueSpecimens(result, list);
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
            if (standard < 0)
                content.Enqueue((ushort)0xFFFF);
            else
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
        /// <returns>白板参数</returns>
        public WhiteBoardParameter GetWhiteBoardParameter()
        {
            var result = SendCommand((byte)Command.ReadWhiteBoardData);
            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    string.Format("Get white board parameter failed: {0}", result.Ack));
            }
            WhiteBoardParameter para = new WhiteBoardParameter();
            string str;
            result.PacketContent.Dequeue(out str);
            para.InnerNumber = str;
            result.PacketContent.Dequeue(out str);
            para.OuterNumber = str;
            Spectrum spectrum = new Spectrum(new WavelengthRange(400, 700, 10));
            result.PacketContent.Dequeue(spectrum);
            // 由于白板参数的反射率是百分比，全部除以100
            for (int i = 0; i < spectrum.Data.Length; i++)
                spectrum.Data[i] /= 100.0;
            para.Spectrum = spectrum;

            return para;
        }

        /// <summary>
        /// 设置白板参数
        /// </summary>
        /// <param name="para">白板参数</param>
        public void SetWhiteBoardParameter(WhiteBoardParameter para)
        {
            PacketContent content = new PacketContent();

            content.Enqueue(para.InnerNumber);
            content.Enqueue(para.OuterNumber);

            // 由于白板参数接收的反射率是百分比，所以将反射率剩以100
            float[] reflectances = new float[WAVELENGTHRANGE.Count];
            for (int i = 0; i < WAVELENGTHRANGE.Count; i++)
            {
                int wavelength = i * WAVELENGTHRANGE.Step + WAVELENGTHRANGE.Min;
                reflectances[i] = (float)(para.Spectrum[wavelength] * 100.0);
            }
            content.Enqueue(reflectances);
            var result = SendCommand((byte)Command.WriteWhiteBoardData, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    string.Format("Set white board parameter failed: {0}", result.Ack));
            }
        }

        public void WriteStandard(Specimen standard)
        {
            ColorData data = null;
            if (SpecularComponent == SpecularComponent.SCI)
            {
                if ((data = standard.SCI) == null)
                {
                    throw new CommunicationException(CommunicationError.InvalidParameter,
                        string.Format("The instrument only accept SCI data, but no SCI data."));
                }
            }
            else if (SpecularComponent == SpecularComponent.SCE)
            {
                if ((data = standard.SCE) == null)
                {
                    throw new CommunicationException(CommunicationError.InvalidParameter,
                        string.Format("The instrument only accept SCE data, but no SCE data."));
                }
            }

            byte[] name = new byte[9];
            byte[] bytes = Encoding.ASCII.GetBytes(standard.Name);
            for (int i = 0; i < 9; i++)
            {
                name[i] = i < bytes.Length ? bytes[i] : (byte)0;
            }

            PacketContent content = new PacketContent();
            if (data.Spectrum != null)
            {
                content.Enqueue((byte)Instrument.DataType.Reflectance);
                content.Enqueue((byte)StandardIlluminant.D65.ToInstrumentForm());
                content.Enqueue((byte)StandardObserver.CIE1964.ToInstrumentForm());
                content.Enqueue((byte)SpecularComponent.ToInstrumentForm());
                content.Enqueue(name);
                content.Enqueue(data.Spectrum);
            }
            else
            {
                content.Enqueue((byte)Instrument.DataType.XYZ);
                var formulas = data.Formulas;
                if (formulas.Length == 1)
                {
                    content.Enqueue((byte)formulas[0].Illuminant.ToInstrumentForm());
                    content.Enqueue((byte)formulas[0].Observer.ToInstrumentForm());
                    content.Enqueue((byte)SpecularComponent.ToInstrumentForm());
                    content.Enqueue(name);
                    CIEXYZ xyz = data.XYZ(formulas[0]);
                    content.Enqueue(xyz.X);
                    content.Enqueue(xyz.Y);
                    content.Enqueue(xyz.Z);
                }
            }

            var result = SendCommand((byte)Command.WriteStandard, content);

            if (!result.IsAckOk)
            {
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                    string.Format("Write standard failed: {0}", result.Ack));
            }
        }

        public void WriteStandards(IEnumerable<Specimen> standards)
        {
            foreach (var standard in standards)
            {
                WriteStandard(standard);
            }
        }

        // 仪器光学结构字典
        static readonly Dictionary<string, List<string>> _instrumentOpticalStructureDict =
            new Dictionary<string, List<string>>
            {
                {"d/8", new List<string> {"810", "820" } },
                {"45/0", new List<string> { "800", "808" } },
            };
        // 由于某些仪器的口径信息不正确且其有固定口径，将其放在口径字典中
        static readonly Dictionary<Instrument.MeasurementCaliber, List<string>> _instrumentCaliberDict =
            new Dictionary<Instrument.MeasurementCaliber, List<string>>
            {
                { Instrument.MeasurementCaliber.Phi4, new List<string> { "820", "800-S"} },
            };

        private ThreeNH.ColorimeterInterface.Tolerance _tolerance;

        static string DetectOpticalStruct(string deviceName)
        {
            deviceName = deviceName.ToUpper();
            foreach (var item in _instrumentOpticalStructureDict)
            {
                foreach (var modle in item.Value)
                {
                    if (deviceName.Contains(modle))
                    {
                        return item.Key;
                    }
                }
            }
            return null;
        }

        static Instrument.MeasurementCaliber DetectCaliber(string deviceName)
        {
            deviceName = deviceName.ToUpper();
            foreach (var item in _instrumentCaliberDict)
            {
                foreach (var modle in item.Value)
                {
                    if (deviceName.Contains(modle))
                    {
                        return item.Key;
                    }
                }
            }
            return Instrument.MeasurementCaliber.Phi8;
        }

        public void RefreshInstrumentInfo()
        {
            Trace.TraceInformation("获取仪器信息...");
            DeviceInfo info = GetDeviceInfo();
            Model = info.DeviceName;
            Trace.TraceInformation("获取测量参数...");
            MeasurementParameter measurementParameter = GetMeasurementParameter();
            _aperture = Model == "MCR10" ? MeasurementCaliber.Phi1 : (Model == "BTS20" ? MeasurementCaliber.Phi50 : measurementParameter.MeasurementCaliber);
            WhiteBoardNumber = info.WhiteBoardOuterNumber;
            FirmwareRevision = info.FirmwareVersion;
            _specularComponent = SpecularComponent.SCI;
            //_specularComponent = measurementParameter.SpecularComponent;
            OpticalStructure = DetectOpticalStruct(info.DeviceName);
            Trace.TraceInformation("刷新仪器信息成功");
        }

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

        public void Open(object commDevice = null)
        {
            Connect(commDevice);
        }

        public void Close()
        {
            Disconnect();
        }

        public ChromaticitySetting GetChromaticitySetting()
        {
            var result = SendCommand((byte)Command.GetChromaticitySetting);
            if (result.IsAckOk)
            {
                try
                {
                    ChromaticitySetting setting = new ChromaticitySetting();
                    setting.ColorSpace = ((Instrument.ColorSpace)result.PacketContent.DequeueByte()).Uniform();
                    setting.ColorDiffFormula =
                        ((Instrument.ColorDiffFormula)result.PacketContent.DequeueByte()).Uniform();
                    var upper = result.PacketContent.DequeueFloat();
                    var lower = result.PacketContent.DequeueFloat();
                    setting.Tolerance.dL = new Tuple<double, double>(lower, upper);

                    upper = result.PacketContent.DequeueFloat();
                    lower = result.PacketContent.DequeueFloat();
                    setting.Tolerance.da = new Tuple<double, double>(lower, upper);

                    upper = result.PacketContent.DequeueFloat();
                    lower = result.PacketContent.DequeueFloat();
                    setting.Tolerance.db = new Tuple<double, double>(lower, upper);

                    setting.Tolerance.dE = result.PacketContent.DequeueFloat();
                    return setting;
                }
                catch
                {
                    throw new CommunicationException(CommunicationError.ParsedBadData);
                }
            }
            throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }

        public void SetChromaticitySetting(ChromaticitySetting setting)
        {
            var dataContent = new PacketContent();
            dataContent.Enqueue((byte)setting.ColorSpace.ToInstrumentForm());
            dataContent.Enqueue((byte)setting.ColorDiffFormula.ToInstrumentForm());
            dataContent.Enqueue((float)setting.Tolerance.dL.Item2);
            dataContent.Enqueue((float)setting.Tolerance.dL.Item1);
            dataContent.Enqueue((float)setting.Tolerance.da.Item2);
            dataContent.Enqueue((float)setting.Tolerance.da.Item1);
            dataContent.Enqueue((float)setting.Tolerance.db.Item2);
            dataContent.Enqueue((float)setting.Tolerance.db.Item1);
            dataContent.Enqueue((float)setting.Tolerance.dE);
            var result = SendCommand((byte)Command.SetChromaticitySetting, dataContent);
            if (!result.IsAckOk)
                throw new CommunicationException(CommunicationError.ExecuteCommandFailed);
        }
        #region IRecordManagement


        public int TotalStandards
        {
            get { return GetStandardCount(); }
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

        #endregion

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
            var result = SendCommand((byte)Command.ReadSpectrumEight, content);
            if (result.IsAckOk)
            {
                uint n = result.PacketContent.Length / 4;
                for (int i = 0; i < n; i++)
                {
                    list.Add(result.PacketContent.DequeueFloat());
                }
                return list;
            }
            return null;
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
            List<float> dataList = new List<float>();
            byte index = 0;
            for (int i = 0; i < rows; i++)
            {
                 index = (byte)i;
                PacketContent packet = new PacketContent();
                packet.Enqueue(index);
                for (int j = 0; j < cols; j++)
                {
                    packet.Enqueue(datas[i][j]);
                }
                var result = SendCommand((byte)Command.WriteSVD, packet);
                if (!result.IsAckOk)
                {

                    throw new CommunicationException(CommunicationError.ExecuteCommandFailed,
                        string.Format("SVD写入失败：{0}", result.PacketContent));
                }
            }
            return true;
        }


        /// <summary>
        /// 读取算法参数矩阵
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <returns></returns>
        public List<List<float>> ReadSVDs(MeasureType? measureType = null,int rows = 31, int cols = 16)
        {
            float[,] arry2D = new float[rows, cols];
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
                        for (int j = 0; j < cols; j++)
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
                list.Add(MeasurementCaliber.Phi4);
                list.Add(MeasurementCaliber.Phi8);
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalLensPositions
        {
            get
            {
                //List<LensPosition> list = new List<LensPosition>();
                //list.Add(LensPosition.Pos4);
                //list.Add(LensPosition.Pos8);
                //list.Add(LensPosition.Pos15);
                //list.Add(LensPosition.Pos1in);
                //return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
                return null;
            }
        }

        public IEnumerable<ValueDescription> OptionalLensSwitchModes
        {
            get
            {
                //List<LensSwitchMode> list = new List<LensSwitchMode>();
                //list.Add(LensSwitchMode.Auto);
                //list.Add(LensSwitchMode.Manual);
                //return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
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
                //List<UVFlag> list = new List<UVFlag>();
                //list.Add(UVFlag.NoCut);
                //list.Add(UVFlag.Cut400nm);
                //list.Add(UVFlag.Cut420nm);
                //list.Add(UVFlag.Cut460nm);
                //return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
                return null;
            }
        }

        public IEnumerable<ValueDescription> OptionalStandardIlluminants
        {
            get
            {
                List<StandardIlluminant> list = new List<StandardIlluminant>();
                list.Add(StandardIlluminant.D65);
                list.Add(StandardIlluminant.D50);
                list.Add(StandardIlluminant.A);
                list.Add(StandardIlluminant.C);
                list.Add(StandardIlluminant.D55);
                list.Add(StandardIlluminant.D75);
                list.Add(StandardIlluminant.F2);
                list.Add(StandardIlluminant.F7);
                list.Add(StandardIlluminant.F11);
                list.Add(StandardIlluminant.F1);
                list.Add(StandardIlluminant.F3);
                list.Add(StandardIlluminant.F4);
                list.Add(StandardIlluminant.F5);
                list.Add(StandardIlluminant.F6);
                list.Add(StandardIlluminant.F8);
                list.Add(StandardIlluminant.F9);
                list.Add(StandardIlluminant.F10);
                list.Add(StandardIlluminant.F12);
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
                List<ThreeNH.Optical.ColorSpace> list = new List<ThreeNH.Optical.ColorSpace>();
                list.Add(ThreeNH.Optical.ColorSpace.CIE_LCH);
                list.Add(ThreeNH.Optical.ColorSpace.CIE_LAB);
                list.Add(ThreeNH.Optical.ColorSpace.CIE_XYZ);
                list.Add(ThreeNH.Optical.ColorSpace.HUNTER_LAB);
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorDiffFormulas
        {
            get
            {
                List<ColorDiffFormula> list = new List<ColorDiffFormula>();
                list.Add(ColorDiffFormula.CIE1976);
                list.Add(ColorDiffFormula.CIE1994);
                list.Add(ColorDiffFormula.CIE2000);
                list.Add(ColorDiffFormula.CMC11);
                list.Add(ColorDiffFormula.CMC21);
                list.Add(ColorDiffFormula.CMC);
                list.Add(ColorDiffFormula.DIN99);
                list.Add(ColorDiffFormula.DHunterE);
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorIndexs
        {
            get
            {
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
