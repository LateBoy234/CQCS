using System.Net;
using System.Threading.Tasks;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30.Model;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30Comm.Result;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Interface
{
    public interface ISpectormeter
    {
        Task<RequestResult> OpenAsync();
        Task<RequestResult> CloseAsync();
        Task<RequestResult> GetCommSettingAsync();
        Task<RequestResult> SetCommSettingAsync(ComSetting comSetting);
        Task<RequestResult> GetSlaveIDAsync();
        Task<RequestResult> SetSlaveIDAsync(ushort slaveID);
        Task<RequestResult> GetBaudRateAsync();
        Task<RequestResult> SetBaudRateAsync(BaudRate rate);

        Task<RequestResult> GetCommInterfaceAsync();
        Task<RequestResult> SetCommInterfaceAsync(CommInterfaceType type);

        Task<RequestResult> GetDACAsync();
        Task<RequestResult> SetDACAsync(ushort value);
        Task<RequestResult> GetMeasureSpeedAsync();
        Task<RequestResult> SetMeasureSpeedAsync(MeasureSpeed mode);

        Task<RequestResult> ExecuteCalibrationAsync(Calibration mode);
        Task<RequestResult> ExecuteMeasureAsync(MeasureType mode);
        Task<RequestResult> GetLanguageAsync();
        Task<RequestResult> SetLanguageAsync(Language language);
        Task<RequestResult> GetBuzzerStatusAsync();
        Task<RequestResult> SetBuzzerStatusAsync(Buzzer status);
        Task<RequestResult> GetBackligthTimeAsync();
        Task<RequestResult>SetBackligthTimeAsync(ushort Time);
        Task<RequestResult> GetBackBrightnessAsync();
        Task<RequestResult> SetBackBrightnessAsync(ushort Time);

        Task<RequestResult> GetStandardIlluminantAsync();
        Task<RequestResult> SetStandardIlluminantAsync(StandardIlluminant illuminant);
        Task<RequestResult> GetObserverAsync();
        Task<RequestResult> SetObserverAsync(StandardObserver observer);
        Task<RequestResult> GetColorSpaceAsync();
        Task<RequestResult> SetColorSpaceAsync(ColorSpace space);
        Task<RequestResult> GetColorDifferenceFormulaAsync();
        Task<RequestResult> SetColorDifferenceFormulaAsync(ColorDiffFormula diffFormula);
        Task<RequestResult> GetReflectanceFactorSCIAsync();
        Task<RequestResult> SetReflectanceFactorSCIAsync(ushort[] factors);
        Task<RequestResult> GetReflectanceFactorSCEAsync();
        Task<RequestResult> SetReflectanceFactorSCEAsync(ushort[] factors);

        Task<RequestResult> GetSystemToleranceAsync();
        Task<RequestResult> SetSystemToleranceAsync(ushort[] tolerance);

        Task<RequestResult> GetStandardToleranceAsync();
        Task<RequestResult> SetStandardToleranceAsync(ushort[] tolerance);

        Task<RequestResult> GetExpireTimeAsync();
        Task<RequestResult> SetExpireTimeAsync(ushort time);
        Task<RequestResult> GetSystemTimeAsync();
        Task<RequestResult> SetSystemTimeAsync(DateTime time);

        Task<RequestResult> GetMeasureControlModeAsync();
        Task<RequestResult> SetMeasureControlModeAsync(MeasureControlMode mode);

        Task<RequestResult> GetIPAddressAsync();
        Task<RequestResult> SetIPAddressAsync(IPAddress address);

        Task<RequestResult> GetIPPortAsync();
        Task<RequestResult> SetIPPortAsync(ushort Port);
        Task<RequestResult> GetIPClientAddressAsync();
        Task<RequestResult> SetIPClientAsync(IPAddress address);

        Task<RequestResult> GetShowOKNGStatusAsync();
        Task<RequestResult> SetShowOKNGStatusAsync(ShowOKNG status);

        Task<RequestResult> GetStandardAverageMeasureCountAsync();
        Task<RequestResult> SetStandardAverageMeasureCountAsync(ushort count);

        Task<RequestResult> GetSampleAverageMeasureCountAsync();
        Task<RequestResult> SetSampleAverageMeasureCountAsync(ushort count);
        Task<RequestResult> GetWriteStandardStatusAsync();
        Task<RequestResult> WriteStandardToInstrumentAsync(Specimen specimen);

        Task<RequestResult> SetFactoryResetAsync();

        Task<RequestResult> ReadCalibrationStatusAsync();
        Task<RequestResult> ReadCustomTypeAsync();
        Task<RequestResult> ReadInstrumentSNAsync();
        Task<RequestResult> ReadSoftwareVersionAsync();
        Task<RequestResult> ReadHardwareVersionAsync();
        Task<RequestResult> ReadExpirtTimeAsync();
        Task<RequestResult> ReadStandardAsync();
        Task<RequestResult> ReadSampleAsync();
    }   
}
