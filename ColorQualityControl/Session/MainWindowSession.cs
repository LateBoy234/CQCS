using System;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Session
{
    /// <summary>
    /// 阶段 2：聚合 Job / 仪器连接 / 测量 / 选中状态四个 Service，MainWindow 仅作 Facade 与 Host。
    /// </summary>
    internal sealed class MainWindowSession : IMainWindowSession
    {
        private IJobDocumentService _jobFacade;

        public MainWindowSession(
            JobDocumentService jobDocument = null,
            InstrumentConnectionService instrument = null,
            MeasurementService measurement = null,
            SpecimenSelectionState selection = null)
        {
            JobDocument = jobDocument ?? new JobDocumentService();
            Instrument = instrument ?? new InstrumentConnectionService();
            Measurement = measurement ?? new MeasurementService();
            Selection = selection ?? new SpecimenSelectionState();
        }

        public JobDocumentService JobDocument { get; }

        public InstrumentConnectionService Instrument { get; }

        public MeasurementService Measurement { get; }

        public SpecimenSelectionState Selection { get; }

        /// <summary>MainWindow 实现 IJobDocumentService 后注入，供仪器域经 Session 入库（阶段2-T02）。</summary>
        internal void AttachJobFacade(IJobDocumentService facade)
        {
            _jobFacade = facade ?? throw new ArgumentNullException(nameof(facade));
        }

        public void AddStandard(StandardSpecimen standard, bool createNewId = true, bool isRemoveSame = true)
            => _jobFacade.AddStandard(standard, createNewId, isRemoveSame);

        public void AddTest(TestSpecimen test, StandardSpecimen standard, bool isSelectedNewTest = false)
            => _jobFacade.AddTest(test, standard, isSelectedNewTest);
    }
}
