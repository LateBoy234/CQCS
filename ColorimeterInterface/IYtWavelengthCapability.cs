namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// YT 系列波长能力（是否含 400nm）。机型特例，不放入通用 <see cref="IColorimeter"/>。
    /// </summary>
    public interface IYtWavelengthCapability
    {
        bool YTContain400 { get; set; }
    }
}
