using System.Threading.Tasks;
using ThreeNH.SQCX.Domain.Measurement;

namespace ThreeNH.SQCX.Application.Measurement
{
    public interface IMeasurementGateway
    {
        bool IsInstrumentConnected { get; }
        bool IsInstrumentCalibrated { get; }
        bool IsHazeEnabled { get; }
        void RunCalibration();
        void ShowError(string message);
        Task<bool> TryAcquireLockAsync();
        void ReleaseLock();
        void EnsureIdle();
        Task ExecuteMeasurementAsync(MeasurementTarget target);
        Task ExecuteHazeMeasurementAsync(MeasurementTarget target);
    }
}
