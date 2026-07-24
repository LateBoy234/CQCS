using System.Threading.Tasks;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 阶段 1 目标：测标/测样流程（现由 MeasureStandardExecuted、MeasureTrialExecuted、FinishMeasureOnUiAsync 承担）。
    /// </summary>
    internal interface IMeasurementService
    {
        StandardSpecimen GetBestStandard(Specimen test);

        Task FinishMeasureOnUiAsync(Specimen sample);

        /// <summary>执行试样测量序列（连续/单次），含 Gloss8 与入库编排。</summary>
        Task ExecuteTrialMeasurementsAsync();

        /// <summary>执行标样测量序列（连续/单次），含后处理与入库编排。</summary>
        Task ExecuteStandardMeasurementsAsync();

        /// <summary>雾度模式下测样（阶段1-T09/T10）。</summary>
        Task ExecuteHazeTrialMeasurementAsync();

        /// <summary>雾度模式下测标（阶段1-T09）。</summary>
        Task ExecuteHazeStandardMeasurementAsync();
    }
}
