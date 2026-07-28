using System;
using System.Threading.Tasks;
using ThreeNH.SQCX.Domain.Measurement;

namespace ThreeNH.SQCX.Application.Measurement
{
    public sealed class ExecuteMeasurementUseCase : IExecuteMeasurementUseCase
    {
        private readonly IInstrumentMeasurementState _instrument;
        private readonly IMeasurementExecution _execution;
        private readonly IMeasurementConcurrency _concurrency;
        private readonly IMeasurementFeedback _feedback;

        public ExecuteMeasurementUseCase(
            IInstrumentMeasurementState instrument,
            IMeasurementExecution execution,
            IMeasurementConcurrency concurrency,
            IMeasurementFeedback feedback)
        {
            _instrument = instrument ?? throw new ArgumentNullException(nameof(instrument));
            _execution = execution ?? throw new ArgumentNullException(nameof(execution));
            _concurrency = concurrency ?? throw new ArgumentNullException(nameof(concurrency));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        public async Task ExecuteAsync(MeasurementTarget target)
        {
            try
            {
                if (!_instrument.IsInstrumentConnected)
                {
                    _feedback.ShowError("色差仪未连接");
                    return;
                }

                if (_instrument.IsHazeEnabled)
                {
                    await _execution.ExecuteHazeMeasurementAsync(target).ConfigureAwait(true);
                    return;
                }

                if (!_instrument.IsInstrumentCalibrated)
                {
                    _instrument.RunCalibration();
                    _feedback.EnsureIdle();
                    return;
                }

                if (!await _concurrency.TryAcquireLockAsync().ConfigureAwait(true))
                    return;

                try
                {
                    await _execution.ExecuteMeasurementAsync(target).ConfigureAwait(true);
                }
                finally
                {
                    _concurrency.ReleaseLock();
                }
            }
            catch (Exception ex)
            {
                _feedback.ShowError(ex.Message);
                _feedback.EnsureIdle();
            }
        }
    }
}
