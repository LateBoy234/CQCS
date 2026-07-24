namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>透射模式黑/白校准。</summary>
    internal sealed class TransmissionCalibrationStrategy : ICalibrationStrategy
    {
        public static TransmissionCalibrationStrategy Instance { get; } = new TransmissionCalibrationStrategy();

        public CalibrationKind Kind => CalibrationKind.Transmission;

        public bool Execute(CalibrationContext context)
            => BlackWhiteCalibrationRunner.Run(context, isTransmission: true);
    }
}
