using System;
using System.Threading.Tasks;
using ThreeNH.SQCX.Domain.Measurement;

namespace ThreeNH.SQCX.Application.Measurement
{
    public sealed class ExecuteMeasurementUseCase : IExecuteMeasurementUseCase
    {
        private readonly IMeasurementGateway _gateway;

        public ExecuteMeasurementUseCase(IMeasurementGateway gateway)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public async Task ExecuteAsync(MeasurementTarget target)
        {
            try
            {
                if (!_gateway.IsInstrumentConnected)
                {
                    _gateway.ShowError("色差仪未连接");
                    return;
                }

                if (_gateway.IsHazeEnabled)
                {
                    await _gateway.ExecuteHazeMeasurementAsync(target).ConfigureAwait(true);
                    return;
                }

                if (!_gateway.IsInstrumentCalibrated)
                {
                    _gateway.RunCalibration();
                    _gateway.EnsureIdle();
                    return;
                }

                if (!await _gateway.TryAcquireLockAsync().ConfigureAwait(true))
                    return;

                try
                {
                    await _gateway.ExecuteMeasurementAsync(target).ConfigureAwait(true);
                }
                finally
                {
                    _gateway.ReleaseLock();
                }
            }
            catch (Exception ex)
            {
                _gateway.ShowError(ex.Message);
                _gateway.EnsureIdle();
            }
        }
    }
}
