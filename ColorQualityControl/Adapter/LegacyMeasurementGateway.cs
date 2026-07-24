using System;
using System.Threading.Tasks;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.SQCX.Application.Measurement;
using ThreeNH.SQCX.Domain.Measurement;

namespace ThreeNH.ColorQualityControl.Adapter
{
    internal sealed class LegacyMeasurementGateway : IMeasurementGateway
    {
        private readonly IMeasureCommandHost _host;
        private readonly MeasurementService _measurement;

        public LegacyMeasurementGateway(
            IMeasureCommandHost host,
            MeasurementService measurement)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _measurement = measurement ?? throw new ArgumentNullException(nameof(measurement));
        }

        public bool IsInstrumentConnected { get { return _host.Colorimeter != null; } }
        public bool IsInstrumentCalibrated
        {
            get { return _host.Colorimeter != null && _host.Colorimeter.IsCalibrated; }
        }
        public bool IsHazeEnabled { get { return _host.IsHazeEnabled; } }

        public void RunCalibration() { _host.RunCalibration(); }
        public void ShowError(string message) { _host.ShowError(message); }
        public Task<bool> TryAcquireLockAsync()
        {
            return _measurement.TryAcquireMeasureLockAsync(0);
        }
        public void ReleaseLock() { _measurement.ReleaseMeasureLock(); }
        public void EnsureIdle() { _measurement.EnsureMeasureIdle(); }

        public Task ExecuteMeasurementAsync(MeasurementTarget target)
        {
            return target == MeasurementTarget.Standard
                ? _measurement.ExecuteStandardMeasurementsAsync()
                : _measurement.ExecuteTrialMeasurementsAsync();
        }

        public Task ExecuteHazeMeasurementAsync(MeasurementTarget target)
        {
            return target == MeasurementTarget.Standard
                ? _measurement.ExecuteHazeStandardMeasurementAsync()
                : _measurement.ExecuteHazeTrialMeasurementAsync();
        }
    }
}
