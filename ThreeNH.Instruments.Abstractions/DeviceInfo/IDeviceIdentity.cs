namespace ThreeNH.Instruments.DeviceInfo
{
    /// <summary>
    /// Stable identity and version information without optical or protocol types.
    /// </summary>
    public interface IDeviceIdentity
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
    }
}
