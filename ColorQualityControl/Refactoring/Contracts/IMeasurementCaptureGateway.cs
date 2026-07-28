using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 仪器测量与结果计算端口。只暴露采集能力，不包含命名、提示或视图刷新。
    /// </summary>
    internal interface IMeasurementCaptureGateway
    {
        string GetInstrumentModel();

        Task<Specimen> CaptureTrialSampleAsync(string progressMessage, double delaySeconds);

        Task<Specimen> CaptureStandardSampleAsync(string progressMessage, double delaySeconds);

        int? CalculateEightDegreesGloss(ColorData sci, ColorData sce);

        bool IsInstrumentClarityValid();

        bool ShouldMarkSampleAsTransmission();
    }
}
