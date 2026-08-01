
using System.Threading.Tasks;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30.Model;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30Comm.Result;

namespace ThreeNH.YL30.CommunicationContract
{
    public interface ISpectrometerCom
    {
        /// <summary>
        /// 获取系统容差值
        /// </summary>
        /// <returns></returns>
        Task<RequestResult<ToleranceForYL30>> GetSystemToleranceAsync();

        /// <summary>
        /// 获取校准数据
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="isCorr"></param>
        /// <returns></returns>

        Task<RequestResult<ColorSpecimen.Specimen>> ExecuteCalibrationAndMeasureAsync(CaliberMeasure mode, ushort startAddress, bool isCorr);

        /// <summary>
        /// 获取仪器基本信息
        /// </summary>
        /// <returns></returns>

        Task<RequestResult<SystemInfo>> GetSystemInfoAsync();
    }
}
