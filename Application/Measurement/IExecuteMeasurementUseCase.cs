using System.Threading.Tasks;
using ThreeNH.SQCX.Domain.Measurement;

namespace ThreeNH.SQCX.Application.Measurement
{
    public interface IExecuteMeasurementUseCase
    {
        Task ExecuteAsync(MeasurementTarget target);
    }
}
