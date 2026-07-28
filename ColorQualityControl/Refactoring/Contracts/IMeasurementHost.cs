using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 试样测量完成后 MainWindow 需提供的 UI / 数据上下文（阶段 1 从 FinishMeasureOnUiAsync 抽出）。
    /// </summary>
    internal interface IMeasurementHost
    {
        StandardSpecimen CurrentStandard { get; }

        /// <summary>单标样视图正在显示的标样；手动选标样时优先于此。</summary>
        StandardSpecimen SingleStandardInShowing { get; }

        bool IsAutoSelectStandard { get; }

        bool IsAutoNaming { get; }

        SpecimenDatabase Database { get; }

        SpecularComponent SpecularComponent { get; }

        StandardObserver Observer { get; }

        StandardIlluminant Illuminant { get; }

        Formula ColorFormula { get; }

        Tolerance AutoSelectStandardLimits { get; }

        /// <summary>非自动命名时弹出试样命名对话框；false 表示用户取消。</summary>
        bool TryPromptTestName(TestSpecimen test, StandardSpecimen standard);

        /// <summary>入库并刷新 TreeView / 图表 / 当前选中试样。</summary>
        void OnTestMeasurementCompleted(TestSpecimen test, StandardSpecimen standard);

        /// <summary>试样测量模式与次数（阶段1-T07 从 MeasureTrialExecuted 抽出）。</summary>
        MeasurementSchedule GetTrialMeasurementSchedule();

        /// <summary>标样测量模式与次数（阶段1-T08 从 MeasureStandardExecuted 抽出）。</summary>
        MeasurementSchedule GetStandardMeasurementSchedule();

        string FormatMeasureProgressMessage(string modeLabel, int current, int total);

        /// <summary>弹出清晰度测量；取消或失败时返回 NaN。</summary>
        double PromptAndMeasureClarity();

        /// <summary>非自动命名时弹出标样命名对话框；false 表示用户取消。</summary>
        bool TryPromptStandardName(StandardSpecimen standard);

        /// <summary>标样入库并刷新 TreeView / 图表 / 当前选中。</summary>
        void OnStandardMeasurementCompleted(StandardSpecimen standard);

        void ShowMeasureError(string message);

        /// <summary>切换透射模式；不支持时弹窗并返回 false。</summary>
        bool EnsureTransmissiveModeForHaze();

        /// <summary>透射校准；失败或取消返回 false。</summary>
        bool EnsureTransmissionCalibratedForHaze();

        /// <summary>雾度对话框采集；取消返回 null。</summary>
        Specimen CaptureHazeSpecimen();

        /// <summary>测量异常是否已处理（如提示重新校准）。</summary>
        bool TryHandleMeasurementAbnormal(System.Exception ex);

        void SetMeasureUiActive(bool isMeasuring);
    }
}
