namespace ThreeNH.Instruments.Calibration
{
    /// <summary>
    /// Calibration capability shared by measuring instruments.
    /// </summary>
    public interface ICalibrationCapability
    {
        bool IsCalibrated { get; }

        void DoWhiteCalibration();

        void DoBlackCalibration();
    }
}
