using ThreeNH.SQCX.Application.Measurement;
using ThreeNH.SQCX.Application.QualityControl;
using ThreeNH.SQCX.Domain.QualityControl;
using ThreeNH.SQCX.Application.StandardSelection;
using ThreeNH.SQCX.Domain.StandardSelection;
using ThreeNH.SQCX.Application.JobDocuments;
using ThreeNH.SQCX.Infrastructure.Persistence;

namespace ThreeNH.SQCX.Infrastructure.DependencyInjection
{
    public sealed class ApplicationServices
    {
        private ApplicationServices(IEvaluateQualityUseCase evaluateQuality)
        {
            EvaluateQuality = evaluateQuality;
        }

        public IEvaluateQualityUseCase EvaluateQuality { get; private set; }

        public static ApplicationServices Create()
        {
            var judgementService = new QualityJudgementService();
            return new ApplicationServices(
                new EvaluateQualityUseCase(judgementService));
        }

        public static IExecuteMeasurementUseCase CreateMeasurement(
            IMeasurementGateway gateway)
        {
            return CreateMeasurement(gateway, gateway, gateway, gateway);
        }

        public static IExecuteMeasurementUseCase CreateMeasurement(
            IInstrumentMeasurementState instrument,
            IMeasurementExecution execution,
            IMeasurementConcurrency concurrency,
            IMeasurementFeedback feedback)
        {
            return new ExecuteMeasurementUseCase(
                instrument,
                execution,
                concurrency,
                feedback);
        }

        public static ISelectStandardUseCase<TStandard, TSample>
            CreateStandardSelection<TStandard, TSample>(
                IStandardSelectionGateway<TStandard, TSample> gateway)
            where TStandard : class
        {
            return new SelectStandardUseCase<TStandard, TSample>(
                gateway,
                new StandardSelectionPolicy());
        }

        public static IJobDocumentPersistence CreateJobDocumentPersistence()
        {
            return new JobDocumentPersistence(new FileSystemJobDocumentStore());
        }
    }
}
