using System;
using System.Windows.Input;
using ThreeNH.SQCX.Application.Measurement;
using ThreeNH.SQCX.Domain.Measurement;

namespace ThreeNH.ColorQualityControl.CommandHandlers
{
    /// <summary>
    /// 测标/测样 RoutedCommand 编排；循环与入库在 MeasurementService（阶段4-T01）。
    /// </summary>
    internal sealed class MeasureCommandHandler
    {
        private readonly IExecuteMeasurementUseCase _useCase;

        public MeasureCommandHandler(IExecuteMeasurementUseCase useCase)
        {
            _useCase = useCase ?? throw new ArgumentNullException(nameof(useCase));
        }

        public async void ExecuteMeasureStandard(object sender, ExecutedRoutedEventArgs e)
        {
            await _useCase.ExecuteAsync(MeasurementTarget.Standard);
        }

        public async void ExecuteMeasureTrial(object sender, ExecutedRoutedEventArgs e)
        {
            await _useCase.ExecuteAsync(MeasurementTarget.Trial);
        }
    }
}
