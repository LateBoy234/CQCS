
using ThreeNH.ColorAlgorithm;
using ThreeNH.Instruments.DeviceInfo;

namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 设备类型
    /// </summary>
    public  enum DeviceType
    {
        Colorimeter,        // 色差仪
        Spectrophotometer,  // 分光光度计
        DensityMeter,       // 密度仪
        transmissivity,
    }

    public interface IDeviceInfo : IDeviceIdentity
    {
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
        WavelengthRange WavelengthRange { get; }
    }
}
