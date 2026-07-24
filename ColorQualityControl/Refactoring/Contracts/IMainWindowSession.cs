using ThreeNH.ColorQualityControl.Services;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 阶段 2：Mediator，协调 Job / 仪器 / 测量 / 选择（见 <see cref="Session.MainWindowSession"/>）。
    /// </summary>
    internal interface IMainWindowSession
    {
        JobDocumentService JobDocument { get; }

        InstrumentConnectionService Instrument { get; }

        MeasurementService Measurement { get; }

        SpecimenSelectionState Selection { get; }

        /// <summary>标样入库（含 UI 呈现），由 MainWindow 实现 <see cref="IJobDocumentService"/> 后注入。</summary>
        void AddStandard(StandardSpecimen standard, bool createNewId = true, bool isRemoveSame = true);

        /// <summary>试样入库（含 UI 呈现），仪器测量路径应经 Session 调用而非直调 MainWindow。</summary>
        void AddTest(TestSpecimen test, StandardSpecimen standard, bool isSelectedNewTest = false);
    }
}
