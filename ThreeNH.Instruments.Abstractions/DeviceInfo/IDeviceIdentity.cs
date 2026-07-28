namespace ThreeNH.Instruments.DeviceInfo
{
    /// <summary>
    /// Stable identity and version information without optical or protocol types.
    /// </summary>
    public interface IDeviceIdentity
    {
        string Model { get; }
        string SN { get; }
        string HardwareRevision { get; }
        string FirmwareRevision { get; }
        string SoftwareRevision { get; }
        string Manufacturer { get; }
    }
}
