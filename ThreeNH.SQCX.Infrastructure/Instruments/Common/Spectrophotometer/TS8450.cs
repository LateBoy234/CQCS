using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ThreeNH.ColorimeterInterface;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;
using Newtonsoft.Json.Linq;
using System.IO;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;
using System.Globalization;
using ThreeNH.Helper;
using ThreeNH.ColorAlgorithm;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using ThreeNH.SQCX.Infrastructure.Instruments.Spectrophotometer.ProtocolContract;

namespace ThreeNH.SQCX.Infrastructure.Instruments.Spectrophotometer
{
    public enum ErrorCode
    {
        OK,             // 没有错误
        FAULT,          // 执行失败
        HARDWARE,       // 硬件错误
        NO_MEMORY,      // 分配不到内存
        IO,             // IO操作失败
        INVALID_PARAM,        // 无效参数
        OUT_OF_RANGE,   // 参数超过范围
        BUF_TOO_SMALL,  // 提供的缓存空间不足
        NOT_SUPPORT,    // 不支持的操作
        STORAGE_FULL,   // 存储区已满
        STORAGE_NOT_ENOUGH, // 存储不足
        BUSY,           // 忙碌状态
        CRC,            // 校验码不对
        FORMAT,         // 格式错误
        TIMEOUT,        // 超时
        CALIBRATION_EXPIRED, // 校正过期
    }

    public class ProductInformation
    {
        /// <summary>
        /// 内部型号名称
        /// </summary>
        public string InternalModel { get; set; }
        /// <summary>
        /// 定制型号名称
        /// </summary>
        public string ModelName { get; set; }
        /// <summary>
        /// SN 码
        /// </summary>
        public string SN { get; set; }

        /// <summary>
        /// 硬件版本
        /// </summary>
        public string HardwareVersion { get; set; }

        /// <summary>
        /// 默认语言
        /// </summary>
        public string Language { get; set; }
    }

    /// <summary>
    /// 光谱仪参数
    /// </summary>
    public class SpectrometerParameter
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string SN;
        /// <summary>
        /// 波长系数
        /// </summary>
        public double[] Coefficients;
    }

    public class TS8450 :  ISystemSetting, IRecordManagement, IColorimeter, IMetrology, ISpecimenClearing, IMeasurementNotification
    {
        // JsonCommProtocol _protocol = new JsonCommProtocol();
        private readonly IJsonCommProtocol _protocol;   // 面向接口
        public TS8450(IJsonCommProtocol protocol)
        {
            _protocol = protocol;
            _protocol.NotificationReceived += OnNotificationReceived;
        }
        #region IInstrument
        public string DeviceName => _protocol.CommDevice?.DeviceName;

        public bool IsOpen => _protocol.IsOpen;

        public bool IsCalibrated
        {
            get
            {
                try
                {
                    var result = _protocol.send("CVR");
                    if (!result.IsOk)
                    {
                        throw ErrorCodeToException(result.ErrorCode);
                    }
                    var jobj = JObject.Parse(result.Json);
                    return jobj["sec"].ToObject<int>() > 0;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

            }
        }

        public string Model { private set; get; }

        public string SN { private set; get; }

        public string HardwareRevision
        {
            private set; get;
        }

        public string FirmwareRevision
        {
            private set; get;
        }

        public string SoftwareRevision
        {
            private set; get;
        }

        public string Manufacturer
        {
            private set; get;
        }

        public string OpticalStructure
        {
            private set; get;
        }

        public string WhiteBoardNumber
        {
            private set; get;
        }

        public DeviceType DeviceType => DeviceType.Spectrophotometer;


        WavelengthRange _wavelengthRange = new ColorAlgorithm.WavelengthRange(400, 700, 10);
        public WavelengthRange WavelengthRange
        {
            get => _wavelengthRange;
            set => throw new InvalidOperationException();
        }

        public void Close()
        {
            _protocol.Close();
        }

        public void DoBlackCalibration()
        {
            var result = _protocol.send("CAL", "0", 30000);
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        public void DoWhiteCalibration()
        {
            var result = _protocol.send("CAL", "1", 30000);
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        public Specimen Measure()
        {
            try
            {
                Thread.Sleep(500);
                var result = _protocol.send("MEA", null, 10000);
                if (!result.IsOk)
                {
                    throw ErrorCodeToException(result.ErrorCode);
                }

                var jobj = JObject.Parse(result.Json);
                return ParseMeasureResult(jobj);
            }
            catch
            {
                throw;
            }
        }

        public Specimen MeasureSample()
        {
            try
            {
                Thread.Sleep(500);
                var result = _protocol.send("MEA", "smp", 10000);
                if (!result.IsOk)
                {
                    throw ErrorCodeToException(result.ErrorCode);
                }
                var jobj = JObject.Parse(result.Json);
                return jobj.ToSpecimen();
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public Specimen MeasureStandard()
        {
            try
            {
                Thread.Sleep(500);
                var result = _protocol.send("MEA", "std", 10000);
                if (!result.IsOk)
                {
                    throw ErrorCodeToException(result.ErrorCode);
                }
                var jobj = JObject.Parse(result.Json);
                return jobj.ToStandard();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        List<string> parts = new List<string>();
        public void Open(object commDevice = null)
        {
            if (commDevice is ICommDevice)
            {
                _protocol.CommDevice = (ICommDevice)commDevice;
            }
            else
            {
                _protocol.Open(commDevice as string);
                parts = (commDevice as string).Split(':').ToList();
            }
            GetInstrumentInfo();
        }

        #endregion

        #region ISystemSettings

        public IEnumerable<ValueDescription> OptionalMeasurementCalibers
        {
            get
            {
                GetSupportedArgs(out var calibers, out var positions);
                if (calibers == null)
                {
                    List<MeasurementCaliber> list = new List<MeasurementCaliber>();
                    list.Add(Aperture);
                    calibers = list.ToArray();
                }
                return calibers?.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalLensPositions
        {
            get
            {
                GetSupportedArgs(out var calibers, out var positions);
                return positions?.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalLensSwitchModes
        {
            get
            {
                List<LensSwitchMode> list = new List<LensSwitchMode>();

                list.Add(LensSwitchMode.Manual);
                list.Add(LensSwitchMode.Auto);

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
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
                List<SpectrumMeasurementMode> list = new List<SpectrumMeasurementMode>();
                list.Add(SpectrumMeasurementMode.Reflective);
                list.Add(SpectrumMeasurementMode.Transmissive);
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalUVFlags
        {
            get
            {
                List<UVFlag> list = new List<UVFlag>();
                list.Add(UVFlag.NoCut);
                list.Add(UVFlag.Cut400nm);
                list.Add(UVFlag.Cut420nm);
                list.Add(UVFlag.Cut460nm);
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
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

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalStandardObservers
        {
            get
            {
                List<StandardObserver> list = new List<StandardObserver>();
                //list.Add(StandardObserver.CIE1931);
                //list.Add(StandardObserver.CIE1964);
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorSpaces
        {
            get
            {
                List<Optical.ColorSpace> list = new List<Optical.ColorSpace>();
                list.Add(Optical.ColorSpace.CIE_LAB);
                list.Add(Optical.ColorSpace.CIE_XYZ);
                list.Add(Optical.ColorSpace.CIE_Yxy);
                list.Add(Optical.ColorSpace.CIE_LCH);
                list.Add(Optical.ColorSpace.HUNTER_LAB);
                list.Add(Optical.ColorSpace.CIE_LUV);
                list.Add(Optical.ColorSpace.sRGB);
                list.Add(Optical.ColorSpace.MUNSELL);
                list.Add(Optical.ColorSpace.BETA_xy);
                list.Add(Optical.ColorSpace.DIN99_LAB);
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
                List<Optical.ColorIndex> list = new List<Optical.ColorIndex>();

                list.Add(Optical.ColorIndex.Yellowness);
                list.Add(Optical.ColorIndex.Whiteness);
                list.Add(Optical.ColorIndex.Strength);
                list.Add(Optical.ColorIndex.Opacity);
                list.Add(Optical.ColorIndex.ColorFastness);
                list.Add(Optical.ColorIndex.StainingFastness);
                list.Add(Optical.ColorIndex.MetamerismIndiex);
                list.Add(Optical.ColorIndex.Shade555);

                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public bool IsLensSwitchModeModifiable => true;
        public bool IsLensPositionModifiable => true;
        public bool IsMeasurementCaliberModifiable => true;
        /// <summary>
        /// 测量方式
        /// </summary>
        private SpectrumMeasurementMode _spectrumMeasurementMode = SpectrumMeasurementMode.Reflective;
        public SpectrumMeasurementMode SpectrumMeasurementMode
        {
            get => _spectrumMeasurementMode;
            set
            {
                SetMeasureCondition("t", value == SpectrumMeasurementMode.Transmissive);
                _spectrumMeasurementMode = value;
            }
        }
        /// <summary>
        /// 测量方式
        /// </summary>
        private SpecularComponent _specularComponent;
        public SpecularComponent SpecularComponent
        {
            get => _specularComponent;
            set
            {
                SetMeasureCondition("sc", DefineConveter.ToString(value));
                _specularComponent = value;
            }
        }
        /// <summary>
        /// 透镜方式：手动，自动
        /// </summary>
        private LensSwitchMode _lensSwitchMode;
        public LensSwitchMode LensSwitchMode
        {
            get => _lensSwitchMode;
            set
            {
                SetMeasureCondition("lens_mode", DefineConveter.ToString(value));
                _lensSwitchMode = value;
            }
        }
        /// <summary>
        /// 透镜位置
        /// </summary>
        private LensPosition _lensPosition;
        public LensPosition LensPosition
        {
            get => _lensPosition;
            set
            {
                SetMeasureCondition("lens", DefineConveter.ToString(value));
                _lensPosition = value;
            }
        }
        /// <summary>
        /// 口径
        /// </summary>
        private MeasurementCaliber _aperture;
        public MeasurementCaliber Aperture
        {
            get => _aperture;
            set
            {
                SetMeasureCondition("ap", DefineConveter.ToString(value));
                SetMeasureCondition("lens", DefineConveter.ToString(value));
                _aperture = value;
            }
        }
        public StandardIlluminant StandardIlluminant { get; set; }
        public StandardObserver StandardObserver { get; set; }
        public ColorSpace ColorSpace { get; set; }
        public ColorDiffFormula ColorDiffFormula { get; set; }
        public ColorIndex ColorIndex { get; set; }

        private UVFlag _uvFlag;

        public event EventHandler<MeasurementNotificationArgs> MeasurementReceived;

        public UVFlag UvMode
        {
            get => _uvFlag;
            set
            {
                SetMeasureCondition("uv", DefineConveter.ToString(value));
                _uvFlag = value;
            }
        }

        public DateTime DateTime
        {
            get
            {
                return GetDateTime();
            }
            set
            {
                SetDateTime(value);
            }
        }
        public Tolerance SystemDefaultTolerance { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public CultureInfo Language { get; set; }


        public bool IsUvModifiable => true;

        public bool IsSpecularComponentModifiable => true;

        public bool IsSpectrumMeasurementModeModifiable => true;

        #endregion

        #region Private Methods

        private Specimen ParseMeasureResult(JObject jobj)
        {

            var specimen = new Specimen();

            specimen.UV = jobj["uv"].ToUvFlag();
            specimen.IsTransmission = jobj["is_trans"]?.ToObject<bool>() ?? false;
            specimen.OpticalStruct = OpticalStructure;
            specimen.InstrumentSN = SN;
            specimen.Instrument = Model;
            specimen.MeasurementCaliber = Aperture;
            specimen.OpticalStruct = OpticalStructure;
            if (jobj.ContainsKey("sci"))
            {
                specimen.SCI = new ColorData(jobj["sci"].ToSpectrum());
            }
            if (jobj.ContainsKey("sce"))
            {
                specimen.SCE = new ColorData(jobj["sce"].ToSpectrum());
            }
            if (jobj.ContainsKey("clr"))
            {
                specimen.Clr = new ColorData(jobj["clr"].ToSpectrum());
            }


            return specimen;
        }

        private CommunicationException ErrorCodeToException(int errorCode)
        {
            CommunicationError commErr = CommunicationError.UnknownError;
            switch ((ErrorCode)errorCode)
            {
                case ErrorCode.OK:
                    return null;
                case ErrorCode.FAULT:
                    commErr = CommunicationError.ExecuteCommandFailed;
                    break;
                case ErrorCode.HARDWARE:
                    commErr = CommunicationError.SystemException;
                    break;
                case ErrorCode.NO_MEMORY:
                    commErr = CommunicationError.SystemException;
                    break;
                case ErrorCode.IO:
                    commErr = CommunicationError.SystemException;
                    break;
                case ErrorCode.INVALID_PARAM:
                    commErr = CommunicationError.InvalidParameter;
                    break;
                case ErrorCode.OUT_OF_RANGE:
                    commErr = CommunicationError.InvalidParameter;
                    break;
                case ErrorCode.BUF_TOO_SMALL:
                    commErr = CommunicationError.SystemException;
                    break;
                case ErrorCode.NOT_SUPPORT:
                    commErr = CommunicationError.NotSupported;
                    break;
                case ErrorCode.STORAGE_FULL:
                    commErr = CommunicationError.SystemException;
                    break;
                case ErrorCode.STORAGE_NOT_ENOUGH:
                    commErr = CommunicationError.SystemException;
                    break;
                case ErrorCode.BUSY:
                    commErr = CommunicationError.DeviceBusy;
                    break;
                case ErrorCode.CRC:
                    commErr = CommunicationError.ExecuteCommandFailed;
                    break;
                case ErrorCode.FORMAT:
                    commErr = CommunicationError.InvalidParameter;
                    break;
                case ErrorCode.TIMEOUT:
                    commErr = CommunicationError.SystemException;
                    break;
                case ErrorCode.CALIBRATION_EXPIRED:
                    commErr = CommunicationError.CalibrationExpired;
                    break;
                default:
                    commErr = CommunicationError.UnknownError;
                    break;
            }

            return new CommunicationException(commErr);
        }


        private void OnNotificationReceived(object sender, NotificationEventArgs e)
        {
            try
            {
                if (e.Json.StartsWith("+DIS"))
                {
                    Specimen msg = new Specimen();
                    msg.Name = "JsonClosed";
                    MeasurementReceived(this, new MeasurementNotificationArgs(msg));
                    this.Close();
                }
                else
                {

                    JObject jobj = JObject.Parse(e.Json);
                    string str = jobj["isStd"]?.ToString();
                    if (str == "True")
                    {

                        var specimen = jobj.ToStandard();// ParseMeasureResult(stdObj);
                        MeasurementReceived(this, new MeasurementNotificationArgs(specimen));
                    }
                    else if (str == "False")
                    {
                        //var smpObj = (JObject)jobj["smp"];
                        var specimen = ParseMeasureResult(jobj);
                        MeasurementReceived(this, new MeasurementNotificationArgs(specimen));
                    }
                    else
                    {
                        // 兼容旧版或兜底解析
                        if (jobj["typ"].ToString() == "mea")
                        {
                            if (jobj.ContainsKey("std"))
                            {
                                var std = ((JObject)jobj["std"]).ToStandard();
                                std.Instrument = Model;
                                std.InstrumentSN = SN;
                                MeasurementReceived(this, new MeasurementNotificationArgs(std));
                            }
                            else if (jobj.ContainsKey("smp"))
                            {
                                var smp = ((JObject)jobj["smp"]).ToSpecimen();
                                smp.Instrument = Model;
                                smp.InstrumentSN = SN;
                                MeasurementReceived(this, new MeasurementNotificationArgs(smp));
                            }
                            else
                            {
                                var specimen = ParseMeasureResult(jobj);
                                MeasurementReceived(this, new MeasurementNotificationArgs(specimen));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Parse json error: {0}", ex.Message);
            }
        }


        #endregion

        #region Public Methods


        private void GetInstrumentInfo()
        {
            var result = _protocol.send("INF");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }

            var jobj = JObject.Parse(result.Json);
            Model = jobj["model"].ToString();
            SN = jobj["sn"].ToString();
            SoftwareRevision = jobj["sv"].ToString();
            FirmwareRevision = jobj["sv"].ToString();
            HardwareRevision = jobj["hv"].ToString();
            _wavelengthRange = jobj["wave_range"].ToWavelengthRange();
            WhiteBoardNumber = jobj["wb"].ToString();
            OpticalStructure = jobj["ge"].ToString() ?? "45/0";
            DefineConveter.WavelengthRange = _wavelengthRange;
        }

        private void GetMeasureCondition()
        {
            var result = _protocol.send("MCR");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }

            var jobj = JObject.Parse(result.Json);
            _uvFlag = jobj["uv"].ToUvFlag();
            _aperture = jobj["ap"].ToAperture();
            _lensPosition = jobj["lens"].ToLensPosition();
            _lensSwitchMode = jobj["lens_mode"].ToLensSwitchMode();
            _specularComponent = jobj["sc"].ToSpecularComponent();
            _spectrumMeasurementMode = jobj["t"].ToObject<bool>() ? SpectrumMeasurementMode.Transmissive : SpectrumMeasurementMode.Reflective;

        }

        /// <summary>
        /// 设置测量条件
        /// </summary>
        /// <param name="keyName">测量条件名称，可设项包括：
        ///     <list type="bullet">
        ///         <item>
        ///             <term>uv</term> <description>设置UV模式</description>
        ///         </item>
        ///         <item>
        ///             <term>ap</term> <description>设置口径大小</description>
        ///         </item>
        ///     </list>
        /// </param>
        /// <param name="value">键值</param>
        public void SetMeasureCondition(string keyName, object value)
        {
            var jobj = new JObject();
            jobj[keyName] = JToken.FromObject(value);
            var result = _protocol.send("MCS", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        public void GetSupportedArgs(out MeasurementCaliber[] calibers, out LensPosition[] positions)
        {
            calibers = null;
            positions = null;
            var result = _protocol.send("FEA");
            if (!result.IsOk)
            {
                MessageBox.Show(ErrorCodeToException(result.ErrorCode).ErrorCode.ToString());
                return;
                //// if(result.ErrorCode)
                // throw ErrorCodeToException(result.ErrorCode);
            }

            var jobj = JObject.Parse(result.Json);
            calibers = jobj["r_aps"].ToCalibers();
            positions = jobj["t_aps"].ToLenPositions();
        }

        public object GetSetting(string propertyName)
        {
            throw new NotImplementedException();
        }

        public void SetSetting(string propertyName, object value)
        {
            throw new NotImplementedException();
        }

        public void RefreshCaches()
        {
            GetMeasureCondition();
        }

        #endregion

        #region IRecordManagement


        /// <summary>
        /// 标样总数
        /// </summary>
        public int TotalStandards
        {
            get
            {
                var jobj = new JObject();
                jobj["typ"] = "std";

                var result = _protocol.send("RCN", jobj.ToString(Formatting.None));
                if (!result.IsOk)
                {
                    throw ErrorCodeToException(result.ErrorCode);
                }
                jobj = JObject.Parse(result.Json);
                return jobj["count"].ToObject<int>();
            }
        }

        /// <summary>
        /// 试样总数
        /// </summary>
        public int TotalSamples
        {
            get
            {
                var jobj = new JObject();
                jobj["typ"] = "smp";

                var result = _protocol.send("RCN", jobj.ToString(Formatting.None));
                if (!result.IsOk)
                {
                    throw ErrorCodeToException(result.ErrorCode);
                }
                jobj = JObject.Parse(result.Json);
                return jobj["count"].ToObject<int>();
            }
        }

        public ICommDevice CommunicationDevice { get => _protocol.CommDevice; set => _protocol.CommDevice = value; }
        public bool YTContain400 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// 获取与指定标样关联的试样总数
        /// </summary>
        /// <param name="standardIndex">标样的序号</param>
        /// <returns>与相应标样关联的试样个数</returns>
        public int GetSamplesCount(int standardIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取与指定标样ID号关联的试样的个数
        /// </summary>
        /// <param name="standardId">要获取与之关联的试样个数的标样的ID</param>
        /// <returns>相应的试样个数</returns>
        public int GetSamplesCountById(int standardId)
        {
            var jobj = new JObject();
            jobj["typ"] = "smp";
            jobj["std_id"] = standardId;

            var result = _protocol.send("RCN", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            jobj = JObject.Parse(result.Json);
            return jobj["count"].ToObject<int>();
        }

        /// <summary>
        /// 上传指定的标样
        /// </summary>
        /// <param name="standardIndex">要上传的标样的序号</param>
        /// <returns>返回指定的标样</returns>
        public DataModel.StandardSpecimen UploadStandard(int standardIndex)
        {
            var jobj = new JObject();
            jobj["typ"] = "std";
            jobj["index"] = standardIndex;
            var sw = Stopwatch.StartNew();
            var result = _protocol.send("RRD", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            jobj = JObject.Parse(result.Json);
            var std = jobj.ToStandard();
            std.Instrument = Model;
            std.InstrumentSN = SN;
            sw.Stop();
            Console.WriteLine($"索引{standardIndex}耗时: {sw.ElapsedMilliseconds} ms");
            return std;
        }

        /// <summary>
        /// 上传指定ID的标样
        /// </summary>
        /// <param name="standardId">要上传的标样的ID</param>
        /// <returns>返回指定的标样</returns>
        public DataModel.StandardSpecimen UploadStandardById(int standardId)
        {
            var jobj = new JObject();
            jobj["typ"] = "std";
            jobj["id"] = standardId;

            var result = _protocol.send("RRD", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            jobj = JObject.Parse(result.Json);
            var std = jobj.ToStandard();
            std.Instrument = Model;
            std.InstrumentSN = SN;
            return std;
        }

        /// <summary>
        /// 从指定标样序号开始上传若干个标样
        /// </summary>
        /// <param name="startIndex">起始标样序号</param>
        /// <param name="count">要上传的标样数；如果为-1上传起始序号开始的所有标样</param>
        /// <returns>上传的标样</returns>
        public DataModel.StandardSpecimen[] UploadStandards(int startIndex, int count)
        {
            List<DataModel.StandardSpecimen> list = new List<DataModel.StandardSpecimen>();
            if (count < 0)
            {
                count = TotalStandards - startIndex;
            }

            for (int i = 0; i < count; i++)
            {
                list.Add(UploadStandard(startIndex + i));
            }
            return list.ToArray();
        }

        /// <summary>
        /// 上传指定的试样
        /// </summary>
        /// <param name="standardIndex">试样关联的标样序号</param>
        /// <param name="sampleIndex">要上传的试样的序号</param>
        /// <returns>返回相应的试样</returns>
        public Specimen UploadSample(int standardIndex, int sampleIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 上传与指定标样ID关联的试样的个数
        /// </summary>
        /// <param name="standardId">要上传的试样关联的标样ID</param>
        /// <param name="sampleIndex">要上传的试样序号</param>
        /// <returns></returns>
        public Specimen UploadSampleById(int standardId, int sampleIndex)
        {
            var jobj = new JObject();
            jobj["typ"] = "smp";
            jobj["std_id"] = standardId;
            jobj["index"] = sampleIndex;

            var result = _protocol.send("RRD", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            jobj = JObject.Parse(result.Json);
            var smp = jobj.ToSpecimen();
            smp.Instrument = Model;
            smp.InstrumentSN = SN;
            return smp;
        }

        /// <summary>
        /// 批量上传与指定标样ID相关联的试样
        /// </summary>
        /// <param name="standardIndex">要上传的试样关联的标样的序号</param>
        /// <param name="startSampleIndex">起始样品序号</param>
        /// <param name="count">要上传的样品个数；如果为-1上传指定序号开始的所有试样</param>
        /// <returns>上传的试样</returns>
        public Specimen[] UploadSamples(int standardIndex, int startSampleIndex, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 批量上传与指定标样ID关联的试样
        /// </summary>
        /// <param name="standardId">要上传的试样关联的标样ID</param>
        /// <param name="startSampleIndex">起始样品序号</param>
        /// <param name="count">要上传的样品个数；如果为-1上传指定序号开始的所有试样</param>
        /// <returns>上传的试样</returns>
        public Specimen[] UploadSamplesById(int standardId, int startSampleIndex, int count)
        {
            if (count < 0)
            {
                count = GetSamplesCountById(standardId);
                count = count - startSampleIndex;
            }
            List<Specimen> list = new List<Specimen>();
            for (int i = 0; i < count; i++)
            {
                list.Add(UploadSampleById(standardId, startSampleIndex + i));
            }
            return list.ToArray();
        }

        /// <summary>
        /// 异步上传标样数据
        /// </summary>
        /// <param name="handler">接收样品数据的函数</param>
        /// <param name="from">起始序号，从0开始</param>
        /// <param name="count">上传的个数</param>
        //void UploadStandardAsync(SpecimenArrivedHandler handler, int from = 0, int count = -1);
        public void UploadStandardAsync(EventHandler<SpecimenReceivedEventArgs> handler, int from = 0, int count = -1)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 异步上传试样数据
        /// </summary>
        /// <param name="handler">接收样品数据的函数</param>
        /// <param name="standard">所属标样的索引</param>
        /// <param name="from">起始序号，从0开始</param>
        /// <param name="count">上传的个数</param>
        public void UploadTestAsync(EventHandler<SpecimenReceivedEventArgs> handler, int standard, int from = 0, int count = -1)
        {
            throw new NotImplementedException();
        }

        public void StopUpload()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 删除所有标样、试样数据
        /// </summary>
        public void RemoveAllRecords()
        {
            var jobj = new JObject();
            jobj["typ"] = "all";

            var result = _protocol.send("RDE", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 删除所有样品数据
        /// </summary>
        public void RemoveAllSamples()
        {
            var jobj = new JObject();
            jobj["typ"] = "smp";

            var result = _protocol.send("RDE", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 删除与指定序号的标样关联的所有试样
        /// </summary>
        /// <param name="standardIndex">要删除其所有试样的标样的序号；如果标样序号为-1则删除所有试样</param>
        public void RemoveAllSamples(int standardIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 删除与指定ID的标样关联的所有试样
        /// </summary>
        /// <param name="standardId">要删除其所有试样的标样的ID；如果标样序号为-1则删除所有试样</param>
        public void RemoveAllSamplesById(int standardId)
        {
            var jobj = new JObject();
            jobj["typ"] = "smp";
            jobj["std_id"] = standardId;

            var result = _protocol.send("RDE", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 删除指定序号的标样
        /// </summary>
        /// <param name="standardIndex">要删除的标样的序号</param>
        public void DeleteStandard(int standardIndex)
        {
            var jobj = new JObject();
            jobj["typ"] = "std";
            jobj["index"] = standardIndex;

            var result = _protocol.send("RDE", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 删除指定ID的标样
        /// </summary>
        /// <param name="standardId">要删除的标样的ID</param>
        public void DeleteStandardById(int standardId)
        {
            var jobj = new JObject();
            jobj["typ"] = "std";
            jobj["id"] = standardId;

            var result = _protocol.send("RDE", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 删除与指定标样序号关联的指定序号的试样
        /// </summary>
        /// <param name="standardIndex">要删除的试样关联的标样的序号</param>
        /// <param name="sampleIndex">要删除的试样的序号</param>
        public void DeleteSample(int standardIndex, int sampleIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 删除与指定标样ID关联的指定序号的试样
        /// </summary>
        /// <param name="standardId">要删除的试样关联的标样的ID</param>
        /// <param name="sampleIndex">要删除的试样的序号</param>
        public void DeleteSampleById(int standardId, int sampleIndex)
        {
            var jobj = new JObject();
            jobj["typ"] = "smp";
            jobj["std_id"] = standardId;
            jobj["index"] = sampleIndex;

            var result = _protocol.send("RDE", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 向仪器中写入一个标样
        /// </summary>
        /// <param name="standard">要写入仪器中的标样</param>
        public void DownloadStandard(Specimen standard)
        {
            var jobj = new JObject();
            jobj["std"] = DefineConveter.ToJObject(standard);

            var result = _protocol.send("RAD", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 批量向仪器中写入标样
        /// </summary>
        /// <param name="standards"></param>
        public void DownloadStandards(IEnumerable<Specimen> standards)
        {
            foreach (var item in standards)
            {
                DownloadStandard(item);
            }
        }

        /// <summary>
        /// 备份仪器中的数据到指定流
        /// </summary>
        /// <param name="stream">备份数据输出位置</param>
        public void Backup(StreamWriter stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 从指定数据流中恢复备份数据
        /// </summary>
        /// <param name="stream">存储备份数据的输入流</param>
        public void Restory(StreamReader stream)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            Close();
        }

        public void DeleteAllSpecimens()
        {
            RemoveAllRecords();
        }

        public void DeleteTests(int standard)
        {
            RemoveAllSamples(standard);
        }

        public void RefreshInstrumentInfo()
        {
            GetInstrumentInfo();
            RefreshCaches();
        }

        public void SetDateTime(DateTime datetime)
        {
            var jobj = new JObject();
            jobj["year"] = datetime.Year;
            jobj["month"] = datetime.Month;
            jobj["day"] = datetime.Day;
            jobj["hour"] = datetime.Hour;
            jobj["minute"] = datetime.Minute;
            jobj["second"] = datetime.Second;
            var result = _protocol.send("TIM", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        public DateTime GetDateTime()
        {
            var result = _protocol.send("TIM");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            var jobj = JObject.Parse(result.Json);
            return new DateTime(jobj["year"].ToObject<int>(),
                jobj["month"].ToObject<int>(),
                jobj["day"].ToObject<int>(),
                jobj["hour"].ToObject<int>(),
                jobj["minute"].ToObject<int>(),
                jobj["second"].ToObject<int>()
                );
        }

        public void Login(string userName, string password = null)
        {
            //USER DEBUG PASSWORD 
            var result = _protocol.send("USER", password == null ? userName : $"{userName} {password}");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        public void RestoreFactorySettings()
        {
            var result = _protocol.send("RST");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }


        public void SetInstrumentLanguage(InstrumentLanguage lang)
        {
            JObject jobj = new JObject();
            string code = "en";
            switch (lang)
            {
                case InstrumentLanguage.English:
                    code = "en";
                    break;
                case InstrumentLanguage.SimplifiedChinese:
                    code = "zh-cn";
                    break;
                default:
                    throw new NotSupportedException();
            }

            var jinfo = new JObject();
            jinfo["language"] = code;
            jobj["info"] = jinfo;
            var result = _protocol.send("FSR", jobj.ToString(Formatting.None));
            if (!result.IsOk)
                throw ErrorCodeToException(result.ErrorCode);
        }

        /// <summary>
        /// 设置白板参数
        /// </summary>
        /// <param name="sn">白板序号</param>
        /// <param name="nummber">白板内部编号</param>
        /// <param name="spectrum">光谱数据</param>
        public void SetWhitboard(string sn, string nummber, Spectrum spectrum)
        {
            JObject jobj = new JObject();
            jobj["sn"] = sn;
            jobj["number"] = nummber;
            jobj["ref"] = DefineConveter.WhiteBoardReflectanceToJArray(spectrum);
            var result = _protocol.send("WBS", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        public void SetWhitboard(string sn, string number, Spectrum sci, Spectrum sce)
        {
            JObject jobj = new JObject();
            jobj["sn"] = sn;
            jobj["number"] = number;
            if (sci != null)
            {
                jobj["sci"] = DefineConveter.WhiteBoardReflectanceToJArray(sci);
            }
            if (sce != null)
            {
                jobj["sce"] = DefineConveter.WhiteBoardReflectanceToJArray(sce);
            }
            var result = _protocol.send("WBS", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 获取取白板参数
        /// </summary>
        /// <param name="sn">白板SN码</param>
        /// <param name="number">白板编号</param>
        /// <param name="spectrum">白板反射率</param>
        public void GetWhiteboard(out string sn, out string number, out Spectrum spectrum)
        {
            var result = _protocol.send("WBR");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            var jobj = JObject.Parse(result.Json);
            sn = jobj["sn"].ToString();
            number = jobj["number"].ToString();
            if (jobj.ContainsKey("ref"))
            {
                spectrum = DefineConveter.WhiteBoardReflectanceToSpectrum((JArray)jobj["ref"]);
            }
            else if (jobj.ContainsKey("sci"))
            {
                spectrum = DefineConveter.WhiteBoardReflectanceToSpectrum((JArray)jobj["sci"]);
            }
            else
            {
                throw new CommunicationException(CommunicationError.ParsedBadData);
            }
        }

        public void GetWhiteboard(out string sn, out string number, out Spectrum sci, out Spectrum sce)
        {
            var result = _protocol.send("WBR");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            var jobj = JObject.Parse(result.Json);
            sn = jobj["sn"].ToString();
            number = jobj["number"].ToString();
            if (jobj.ContainsKey("ref"))
            {
                sci = DefineConveter.WhiteBoardReflectanceToSpectrum((JArray)jobj["ref"]);
                sce = null;
                return;
            }

            if (jobj.ContainsKey("sci"))
            {
                sci = DefineConveter.WhiteBoardReflectanceToSpectrum((JArray)jobj["sci"]);
            }
            else
            {
                sci = null;
            }

            if (jobj.ContainsKey("sce"))
            {
                sce = DefineConveter.WhiteBoardReflectanceToSpectrum((JArray)jobj["sce"]);
            }
            else
            {
                sce = null;
            }
        }

        public uint GetIntegrationTime()
        {
            var result = _protocol.send("FSR", "led_config");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            var jobj = JObject.Parse(result.Json);
            return jobj["integration"].ToObject<uint>();
        }

        public void SetIntegrationTime(uint integration)
        {
            if (integration < 5 || integration > 1000)
            {
                throw new ArgumentOutOfRangeException();
            }

            var led_config = new JObject();
            led_config["integration"] = integration;
            var jobj = new JObject();
            jobj["led_config"] = led_config;
            var result = _protocol.send("FSS", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        /// <summary>
        /// 设置默认口径
        /// </summary>
        /// <param name="aperture">默认口径</param>
        /// <param name="fix_aperture">是否是固定不可切换口径</param>
        public void SetDefaultAperture(MeasurementCaliber aperture, bool fix_aperture)
        {
            var aperture_name = DefineConveter.ToString(aperture);
            if (string.IsNullOrEmpty(aperture_name))
            {
                throw new ArgumentException($"Not support the {aperture}", nameof(aperture));
            }
            var misc_config = new JObject();
            misc_config["default_aperture"] = aperture_name;
            misc_config["fix_aperture"] = fix_aperture;

            var jobj = new JObject();
            jobj["misc_config"] = misc_config;
            var result = _protocol.send("FSS", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        public MeasurementCaliber GetDefaultAperture()
        {
            var result = _protocol.send("FSR", "misc_config");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            var jobj = JObject.Parse(result.Json);
            return jobj["default_aperture"].ToAperture();
        }

        public ProductInformation GetProductInformation()
        {
            var result = _protocol.send("FSR", "info");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            return JObject.Parse(result.Json).ToProductInformation();
        }

        public void SetProductInformation(ProductInformation info)
        {
            JObject jobj = new JObject();
            jobj["info"] = info.ToJOjbect();
            var result = _protocol.send("FSS", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }


        public void SetSpectrometerParameters(SpectrometerParameter[] spectrometers)
        {
            JObject jobj = new JObject();
            var jarr = new JArray();
            foreach (var item in spectrometers)
            {
                jarr.Add(item.ToJObject());
            }
            jobj["spectrometers"] = jarr;
            var result = _protocol.send("FSS", jobj.ToString(Formatting.None));
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
        }

        public SpectrometerParameter[] GetSpectrometerParameters()
        {
            var result = _protocol.send("FSR", "spectrometers");
            if (!result.IsOk)
            {
                throw ErrorCodeToException(result.ErrorCode);
            }
            var jarr = JArray.Parse(result.Json);
            var parameters = new SpectrometerParameter[jarr.Count];
            for (int i = 0; i < jarr.Count; i++)
            {
                parameters[i] = ((JObject)jarr[i]).ToSpectrometerParameter();
            }
            return parameters;
        }

        public List<float> GetMeasureSpectrumEight(int numbers = 8)
        {
            throw new NotImplementedException();
        }

        public bool SetSVD(List<List<float>> datas, MeasureType? measureType = null, byte caliber = 0)
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

        public bool SetOLSR(List<List<float>> datas)
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
