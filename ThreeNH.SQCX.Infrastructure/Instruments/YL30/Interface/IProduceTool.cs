using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30Comm.Result;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Interface
{
    public  interface IProduceTool
    {
        Task<RequestResult> Internal_SetProductionResetAsync();
        Task<RequestResult> Internal_SetFactoryResetAsync();
        Task<RequestResult> Internal_GetIntegrationTimeAsync();
        Task<RequestResult> Internal_SetIntegrationTimeAsync();
        Task<RequestResult> Internal_GetPrimarySensorSNAsync();
        Task<RequestResult> Internal_SetPrimarySensorSNAsync(string SN);
        Task<RequestResult> Internal_GetPrimaryCoefficientAsync();
        Task<RequestResult> Internal_SetPrimaryCoefficientAsync(List<float>Coefficients);
        Task<RequestResult> Internal_GetAuxiliarySensorSNAsync();
        Task<RequestResult> Internal_SetAuxiliarySensorSNAsync(string SN);
        Task<RequestResult> Internal_GetAuxiliaryCoefficientAsync();
        Task<RequestResult> Internal_SetAuxiliaryCoefficientAsync(List<float> Coefficients);
        Task<RequestResult> Internal_GetWhiteboardExternalNOAsync();
        Task<RequestResult> Internal_SetWhiteboardExternalNOAsync(string NO);

        Task<RequestResult> Internal_GetWhiteboardInternalNOAsync();
        Task<RequestResult> Internal_SetWhiteboardInternalNOAsync(string NO);
        Task<RequestResult> Internal_GetWhiteboardReflectanceSCIAsync();
        Task<RequestResult> Internal_SetWhiteboardReflectanceSCIAsync(List<int> reflectances);
        Task<RequestResult> Internal_GetWhiteboardReflectanceSCEAsync();
        Task<RequestResult> Internal_SetWhiteboardReflectanceSCEAsync(List<int> reflectances);
        Task<RequestResult> Internal_GetFactoryReflectanceSCIAsync();
        Task<RequestResult> Internal_SetFactoryReflectanceSCIAsync(List<int> reflectances);
        Task<RequestResult> Internal_SetLEDAsync(LED status);
        Task<RequestResult> Internal_GetInstrumentInternalTypeAsync();
        Task<RequestResult> Internal_SetInstrumentInternalTypeAsync(InstrumentInternalType type);
        Task<RequestResult> Internal_GetInstrumentNameAsync();
        Task<RequestResult> Internal_SetInstrumentNameAsync(string Name);
        Task<RequestResult> Internal_GetInstrumentSNAsync();
        Task<RequestResult> Internal_SetInstrumentSNAsync(string Name);

        Task<RequestResult> Internal_GetInstrumentHardwareVersionAsync();
        Task<RequestResult> Internal_SetInstrumentHardwareVersionAsync(string Name);
        Task<RequestResult> Internal_GetInstrumentLanguageAsync();
        Task<RequestResult> Internal_SetInstrumentLanguageAsync(Language language);

        Task<RequestResult> Internal_GetProductionPasswordAsync();
        Task<RequestResult> Internal_SetProductionPasswordAsync(string password);
        Task<RequestResult> Internal_DecryptionInstrumentAsync();
        Task<RequestResult> Internal_GetCPUIDAsync();
        Task<RequestResult> Internal_GetServoPositionAsync();
        Task<RequestResult> Internal_SetServoPositionAsync(int pos);
    }
}
