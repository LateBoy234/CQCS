namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>反射模式黑/白校准。</summary>
    internal sealed class ReflectanceCalibrationStrategy : ICalibrationStrategy
    {
        public static ReflectanceCalibrationStrategy Instance { get; } = new ReflectanceCalibrationStrategy();

        public CalibrationKind Kind => CalibrationKind.Reflectance;

        public bool Execute(CalibrationContext context)
            => BlackWhiteCalibrationRunner.Run(context, isTransmission: false);
    }
}
