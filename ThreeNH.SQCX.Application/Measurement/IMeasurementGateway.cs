using System.Threading.Tasks;
using ThreeNH.SQCX.Domain.Measurement;

namespace ThreeNH.SQCX.Application.Measurement
{
    public interface IInstrumentMeasurementState
    {
        bool IsInstrumentConnected { get; }
        bool IsInstrumentCalibrated { get; }
        bool IsHazeEnabled { get; }
        void RunCalibration();
    }

    public interface IMeasurementExecution
    {
        Task ExecuteMeasurementAsync(MeasurementTarget target);
        Task ExecuteHazeMeasurementAsync(MeasurementTarget target);
    }

    public interface IMeasurementConcurrency
    {
        Task<bool> TryAcquireLockAsync();
        void ReleaseLock();
    }

    public interface IMeasurementFeedback
    {
        void ShowError(string message);
        void EnsureIdle();
    }

    /// <summary>
    /// Compatibility facade for composition roots that still provide one adapter.
    /// New use cases depend on the focused interfaces above.
    /// </summary>
    public interface IMeasurementGateway :
        IInstrumentMeasurementState,
        IMeasurementExecution,
        IMeasurementConcurrency,
        IMeasurementFeedback
    {
    }
}
