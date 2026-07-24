namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>校准策略类型（阶段 3-T05）。</summary>
    internal enum CalibrationKind
    {
        Reflectance = 0,
        Transmission = 1,
        Haze = 2,
        Clarity = 3,
        InstrumentDialog = 4,
    }
}
