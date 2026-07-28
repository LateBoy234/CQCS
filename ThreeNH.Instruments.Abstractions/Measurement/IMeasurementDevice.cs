using ThreeNH.Instruments.Calibration;

namespace ThreeNH.Instruments.Measurement
{
    /// <summary>
    /// Generic measurement contract. The result type is supplied by an adapter,
    /// keeping this assembly independent from legacy specimen models.
    /// </summary>
    public interface IMeasurementDevice<out TResult> : ICalibrationCapability
    {
        TResult Measure();

        TResult MeasureStandard();

        TResult MeasureSample();
    }
}
