namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>标/试样连续测量调度参数（来自 SystemDefaultConfig.TestModelParameter）。</summary>
    internal sealed class MeasurementSchedule
    {
        public int Times { get; set; }

        public double DelaySeconds { get; set; }

        public string ModeLabel { get; set; }
    }
}
