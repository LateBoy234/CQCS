using ThreeNH.ColorimeterInterface;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// MeasureStandard/MeasureTrial Command 编排所需的 UI / 校准入口（阶段4-T01）。
    /// </summary>
    internal interface IMeasureCommandHost
    {
        IInstrument Colorimeter { get; }

        bool IsHazeEnabled { get; }

        /// <summary>未校准时走校准流程；返回值保留原 DoCalibration 语义。</summary>
        bool RunCalibration();

        void ShowError(string message);
    }
}
