using ThreeNH.SQCX.Application.Measurement;
using ThreeNH.SQCX.Application.QualityControl;
using ThreeNH.SQCX.Domain.QualityControl;
using ThreeNH.SQCX.Application.StandardSelection;
using ThreeNH.SQCX.Domain.StandardSelection;

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
            return new ExecuteMeasurementUseCase(gateway);
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
    }
}
