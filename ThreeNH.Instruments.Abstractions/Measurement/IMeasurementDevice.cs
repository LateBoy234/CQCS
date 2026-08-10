using ThreeNH.Instruments.Calibration;

namespace ThreeNH.Instruments.Measurement
{
    /// <summary>
    /// Generic measurement contract. The result type is supplied by an adapter,
    /// out 表示 协变（covariance）：TResult 只能出现在“输出”位置（返回值），不能作为方法参数。
    /// 标 out。含义是：能量测出更具体结果的设备，也可以当成能量测出更抽象结果的设备来用。
    /// keeping this assembly independent from legacy specimen models.
    /// </summary>
    public interface IMeasurementDevice<out TResult> : ICalibrationCapability
    {
        TResult Measure();

        TResult MeasureStandard();

        TResult MeasureSample();
    }
}
