using Newtonsoft.Json.Linq;
using NModbus;
using NModbus.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;
using ThreeNH.Helper;
using ThreeNH.Optical;
using ThreeNH.Spectrophotometer;
using ThreeNH.YL30.CommunicaImplementation;
using ThreeNH.YL30.CommunicationContract;
using ThreeNH.YL30.Extensions;
using ThreeNH.YL30.Model;
using YL30;
using YL30.Model;
using YL30.StructHelper;
using YL30Comm.Result;

namespace ThreeNH.YL30
{
    public class SpectrometerForYL30 : IDisposable, IInstrument, IMeasurementNotification, ISystemSetting
    {

        public SpectrometerForYL30(YL30Communication slaveIDCommunication, CommDevice.SerialPortDevice serial = null)
        {
            if (serial != null)
            {
                modbusMaster = new ModbusRtuMaster(serial, slaveIDCommunication.LastSlaveID);
            }
            SlaveID = slaveIDCommunication.LastSlaveID;
            communication = slaveIDCommunication;
            if (!IsOpen)
            {
                Open();
            }
            InitalizeSpectro();
            var InfoResult = spectrometer?.GetSystemInfoAsync().Result;
            if (InfoResult.ResultType == ResultType.OperationSuccess)
            {
                SystemInfo = InfoResult.Obj;
            }
            //获取系统信息失败，可能是校正过程中断开连接了，所以直接关闭连接，并抛出异常
            else
            {
                Close();
                throw new Exception("Failed to get system info");
            }
            SpecularComponent = Optical.SpecularComponent.SCI;
        }

        ISpectrometerCom spectrometer;
        public YL30Communication communication = new YL30Communication();

        public ushort SlaveID { get; set; }
        public bool IsYLLanConnected { get; set; }

        private TcpClient TcpClient { get; set; }

        public SystemInfo SystemInfo { get; set; } = new SystemInfo();

        ModbusRtuMaster modbusMaster;
        IModbusMaster LanMaster;

        public event EventHandler<MeasurementNotificationArgs> MeasurementReceived;

        private void InitalizeSpectro()
        {
            if (IsYLLanConnected)
            {
                spectrometer = new ModbusCommunication(LanMaster, SlaveID);
            }
            else
            {
                spectrometer = new SerialCommunication(modbusMaster, SlaveID);
            }
        }

        #region IInstrument接口的实现
        public string DeviceName => SystemInfo.Name;

        public bool IsOpen => GetConnection();

        private bool GetConnection()
        {
            if (communication.CommType == EnumDefines.CommInterfaceType.TCPIP)
            {
                return IsYLLanConnected;
            }
            else
            {
                return modbusMaster.IsOpen;
            }
        }
        public bool IsCalibrated => (int)SystemInfo.CalibrationStatus == (int)CalibrationNewStatus.AllFinished;

        public string Model => SystemInfo.Name;

        public string SN => SystemInfo.SN;



        public string HardwareRevision => SystemInfo.HardwareVersion;

        public string FirmwareRevision { get; }

        public string SoftwareRevision => SystemInfo.HardwareVersion;

        public string Manufacturer { get; }

        public string OpticalStructure => "d/8";

        public string WhiteBoardNumber { get; set; }

        public DeviceType DeviceType => DeviceType.Spectrophotometer;

        public WavelengthRange WavelengthRange {
            get => new WavelengthRange(Constants.VALID_WAVE_BEGIN, Constants.VALID_WAVE_END, Constants.VALID_WAVE_STEP);
            set => throw new NotSupportedException();
        }



        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (IsYLLanConnected)
            {
                if (TcpClient != null)
                {
                    TcpClient?.Close();
                }
                IsYLLanConnected = false;
            }
            try
            {
                modbusMaster?.Close();
            }
            catch (Exception ex)

            {
                throw ex;
            }
        }

        public void DoBlackCalibration()
        {
            var mode = CaliberMeasure.Black;
            MeasureResult(mode, true);
            var InfoResult = spectrometer.GetSystemInfoAsync().Result;
            if (InfoResult.ResultType == ResultType.OperationSuccess)
            {
                SystemInfo = InfoResult.Obj;
                if (SystemInfo.CalibrationStatus == CalibrationStatus.BlackCalibrationUnFinished)
                {
                    throw new Exception("BlackCalibrationPrompt");
                }
            }
            //获取系统信息失败，可能是校正过程中断开连接了，所以直接关闭连接，并抛出异常
            else
            {
                Close();
                throw new Exception("Failed to get system info");
            }
        }

        public void DoWhiteCalibration()
        {
            var mode = CaliberMeasure.White;
            MeasureResult(mode, true);
            var InfoResult = spectrometer.GetSystemInfoAsync().Result;
            if (InfoResult.ResultType == ResultType.OperationSuccess)
            {
                SystemInfo = InfoResult.Obj;
               if( SystemInfo.CalibrationStatus== CalibrationStatus.WhiteCalibrationUnfinished)
                {
                    throw new Exception("WhiteCalibrationPrompt");
                }
            }
            //获取系统信息失败，可能是校正过程中断开连接了，所以直接关闭连接，并抛出异常
            else
            {
                Close();
                throw new Exception("Failed to get system info");
            }
        }
        /// <summary>
        /// 潘通色卡匹配测量，需要异步跨线程
        /// </summary>
        /// <returns></returns>
        public ColorSpecimen.Specimen Measure()
        {
            var mode = CaliberMeasure.Standard;
            return MeasureResult(mode);
        }

        public ColorSpecimen.Specimen MeasureSample()
        {
            var mode = CaliberMeasure.Sample;
            return MeasureResult(mode);
        }

        public ColorSpecimen.Specimen MeasureStandard()
        {
            var mode = CaliberMeasure.Standard;
            return MeasureResult(mode);
        }
        /// <summary>
        /// 校正+测量
        /// </summary>
        /// <param name="mode">0001黑校，0002白校，0003标样测量，0004试样测量，0005,绿校，0006触发一次自动校准</param>
        /// <param name="isCorr">区分是校正还是测量</param>
        /// <returns></returns>
        public ColorSpecimen.Specimen MeasureResult(CaliberMeasure mode, bool isCorr = false)
        {
            try
            {
                ushort startAddress = 0;
                switch (mode)
                {
                    case CaliberMeasure.Black:
                    case CaliberMeasure.White:
                        startAddress = (ushort)HoldingRegisterNewCommand.CalibrateMeasure;
                        break;
                    case CaliberMeasure.Standard:
                        startAddress = (ushort)Command.Standard;
                        break;
                    case CaliberMeasure.Sample:
                        startAddress = (ushort)Command.Sample;
                        break;
                    default:
                        break;
                }
                // 将整个操作封装到Task.Run中，完全脱离UI线程上下文
                var specimen = Task.Run(async () =>
                {
                    var task = spectrometer.ExecuteCalibrationAndMeasureAsync(mode, startAddress, isCorr);
                    var requestResult = await task.ConfigureAwait(false);

                    if (isCorr)
                    {
                        return new ColorSpecimen.Specimen();
                    }
                    ColorSpecimen.Specimen spec = requestResult.Obj;

                    spec.Name = null;
                    spec.ID = 0;

                    return spec;
                }).GetAwaiter().GetResult();

                return specimen;
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        private NetworkConnectivityMonitor monitor;
        public void Open(object commDevice = null)
        {
            try
            {
                if (communication.CommType == EnumDefines.CommInterfaceType.TCPIP)
                {
                    TcpClient = new TcpClient(communication.IP, Convert.ToInt32(communication.Port)) { SendTimeout = 1000, ReceiveTimeout = 5000 };
                    var factory = new ModbusFactory();
                    LanMaster = factory.CreateMaster(TcpClient);
                    if (LanMaster != null)
                    {
                        monitor = new NetworkConnectivityMonitor();
                        // connectivityMonitor.StartMonitoring(communication.IP, Convert.ToInt32(communication.Port));

                        IsYLLanConnected = true;

                        // 异步启动主站模式（不阻塞 UI）
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await monitor.StartMonitoTCP(communication.IP, Convert.ToInt32(communication.Port));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[SocketCommProtocol] Failed to start MasterMode: {ex.Message}");
                            }
                        });
                    }
                }
                else
                {
                    modbusMaster.Open(commDevice);
                    IsYLLanConnected = false;
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        #endregion

        #region ISystemSetting接口的实现
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
                List<Optical.SpecularComponent> list = new List<Optical.SpecularComponent>();
                list.Add(Optical.SpecularComponent.SCI);
                list.Add(Optical.SpecularComponent.SCE);
                list.Add(Optical.SpecularComponent.SCIAndSCE);
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
                List<Optical.StandardIlluminant> list = new List<Optical.StandardIlluminant>();
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalStandardObservers
        {
            get
            {
                List<Optical.StandardObserver> list = new List<Optical.StandardObserver>();
                list.Add(Optical.StandardObserver.CIE1931);
                list.Add(Optical.StandardObserver.CIE1964);
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
                List<Optical.ColorDiffFormula> list = new List<Optical.ColorDiffFormula>();
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }

        public IEnumerable<ValueDescription> OptionalColorIndexs
        {
            get
            {
                List<ThreeNH.Optical.ColorIndex> list = new List<ThreeNH.Optical.ColorIndex>();
                return list.Select(o => new ValueDescription() { Value = o, Description = o.Description() });
            }
        }
        //SpectrumMeasurementMode.Reflective
        private SpectrumMeasurementMode _spectrumMeasurementMode;

        public SpectrumMeasurementMode SpectrumMeasurementMode
        {
            get { return _spectrumMeasurementMode; }
            set
            {
                _spectrumMeasurementMode = value;
            }
        }
        // public Optical.SpecularComponent SpecularComponent => Optical.SpecularComponent.SCI;
        private Optical.SpecularComponent _specularComponent;

        public Optical.SpecularComponent SpecularComponent
        {
            get { return _specularComponent; }
            set
            {
                _specularComponent = value;
            }
        }
        public LensSwitchMode LensSwitchMode { get; set; }
        public LensPosition LensPosition { get; set; }
        public MeasurementCaliber Aperture { get; set; }
        public Optical.StandardIlluminant StandardIlluminant { get; set; }
        public Optical.StandardObserver StandardObserver { get; set; }
        public Optical.ColorSpace ColorSpace { get; set; }
        public Optical.ColorDiffFormula ColorDiffFormula { get; set; }
        public ColorIndex ColorIndex { get; set; }
        public UVFlag UvMode { get; set; }
        public System.DateTime DateTime { get; set; }
        public ColorimeterInterface.Tolerance SystemDefaultTolerance
        {
            get
            {
                var t = spectrometer.GetSystemToleranceAsync().Result.Obj;
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
                var t = new ToleranceForYL30
                {
                    dE = (float)value.dE,
                    dL_Lower = (float)value.dL.Item1,
                    dL_Upper = (float)value.dL.Item2,
                    da_Lower = (float)value.da.Item1,
                    da_Upper = (float)value.da.Item2,
                    db_Lower = (float)value.db.Item1,
                    db_Upper = (float)value.db.Item2
                };
                _ = SetSystemToleranceAsync(t);
            }
        }
        /// <summary>
        /// 设置容差值
        /// </summary>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public async Task<RequestResult> SetSystemToleranceAsync(ToleranceForYL30 tolerance)
        {
            if (IsYLLanConnected)
            {
                try
                {
                    List<ushort> array = new List<ushort>();
                    var content = tolerance.ToUnshortArray();
                    array.AddRange(content);
                    await LanMaster.WriteMultipleRegistersAsync((byte)SlaveID, (ushort)Command.SystemTolerance, array.ToArray());
                    return new RequestResult(ResultType.OperationSuccess);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                try
                {
                    List<byte> bytes = new List<byte>();
                    var content = tolerance.ToBytes().ToArray();
                    bytes.AddRange(content);
                    return await modbusMaster.WriteMultiRegistersAsync(SlaveID, (ushort)Command.SystemTolerance, bytes.ToArray());

                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }

        }

        public CultureInfo Language { get; set; }

        public bool IsLensSwitchModeModifiable
        {
            get { return false; }
        }

        public bool IsLensPositionModifiable
        {
            get { return false; }
        }

        public bool IsMeasurementCaliberModifiable
        {
            get { return false; }
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


        public object GetSetting(string propertyName)
        {
            return typeof(Spectrometer).GetProperty(propertyName)?.GetValue(this, null);
        }

        public void SetSetting(string propertyName, object value)
        {
            typeof(Spectrometer).GetProperty(propertyName)?.SetValue(this, value, null);
        }

        public void RefreshCaches()
        {

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

        public void SetDateTime(System.DateTime datetime)
        {
            this.DateTime = datetime;
        }
        #endregion
    }
}
