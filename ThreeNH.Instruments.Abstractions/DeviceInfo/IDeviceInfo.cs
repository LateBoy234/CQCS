namespace ThreeNH.Instruments.DeviceInfo
{
    /// <summary>
    /// 波长范围（托管值对象，零依赖原生 ThreeNH.ColorAlgorithm）。
    /// 仅承载区间信息供领域/端口层使用；需对接原生算法时由适配器映射为 ThreeNH.ColorAlgorithm.WavelengthRange。
    /// </summary>
    public struct WavelengthBand
    {
        /// <summary>最小波长。</summary>
        public int Min { get; }
        /// <summary>最大波长。</summary>
        public int Max { get; }
        /// <summary>波长步长。</summary>
        public int Step { get; }


        public WavelengthBand(int min, int max, int step = 0)
        {
            Min = min;
            Max = max;
            Step = step;
        }
    }

    /// <summary>
    /// 设备品类（按测量原理划分）。
    /// 注意：透射/反射是测量模式而非品类，见 <see cref="MeasurementMode"/>。
    /// </summary>
    public enum DeviceType
    {
        Colorimeter,        // 色差仪
        Spectrophotometer,  // 分光光度计
        DensityMeter,       // 密度仪
    }

    /// <summary>
    /// 测量模式，不属于设备品类。透射型仪器仍是 Spectrophotometer，仅测量模式不同。
    /// </summary>
    public enum MeasurementMode
    {
        Reflectance,    // 反射
        Transmittance,  // 透射
    }
    /// <summary>
    /// 稳定的设备身份与版本信息（型号、序列号、各版本号、制造商、光学结构、波长范围）。
    /// 对具体光学/协议实现保持零依赖；波长范围以托管值对象 <see cref="WavelengthBand"/> 暴露，
    /// 实现层（Infrastructure）在需要对接原生 ThreeNH.ColorAlgorithm 算法时自行映射。
    /// </summary>
    public interface IDeviceInfo
    {
        /// <summary>
        /// 仪器型号
        /// </summary>
        string Model { get; }

        /// <summary>
        /// 仪器序列号
        /// </summary>
        string SN { get; }

        /// <summary>
        /// 设备硬件版本号
        /// </summary>
        string HardwareRevision { get; }

        /// <summary>
        /// 设备固件版本号
        /// </summary>
        string FirmwareRevision { get; }

        /// <summary>
        /// 设备软件版本号
        /// </summary>
        string SoftwareRevision { get; }

        /// <summary>
        /// 设备制造商
        /// </summary>
        string Manufacturer { get; }

        /// <summary>
        /// 设备光学结构
        /// </summary>
        string OpticalStructure { get; }

        /// <summary>
        /// 配套白板编号
        /// </summary>
        string WhiteBoardNumber { get; }

        /// <summary>
        /// 设备类型
        /// </summary>
        DeviceType DeviceType { get; }

        /// <summary>
        /// 测量波长范围
        /// </summary>
        WavelengthBand WavelengthRange { get; }
    }
}
