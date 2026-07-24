namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>
    /// 阶段 3-T05：反射 / 透射 / 雾度 / 清晰度等校准流程策略。
    /// </summary>
    internal interface ICalibrationStrategy
    {
        CalibrationKind Kind { get; }

        /// <summary>执行校准；成功返回 true，用户取消或失败返回 false。</summary>
        bool Execute(CalibrationContext context);
    }
}
