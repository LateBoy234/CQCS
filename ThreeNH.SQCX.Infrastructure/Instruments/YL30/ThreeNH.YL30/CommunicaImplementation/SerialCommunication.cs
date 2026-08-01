using System;
using System.Threading.Tasks;
using ThreeNH.SQCX.Infrastructure.Instruments.ThreeNH.YL30;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30.Model;
using ThreeNH.SQCX.Infrastructure.Instruments.YL30Comm.Result;
using ThreeNH.YL30.CommunicationContract;
using ThreeNH.YL30.DataHandle;
using ModbusRtuMaster = ThreeNH.SQCX.Infrastructure.Instruments.ThreeNH.YL30.ModbusRtuMaster;

namespace ThreeNH.YL30.CommunicaImplementation
{
    internal class SerialCommunication : ISpectrometerCom
    {
        public SerialCommunication(ModbusRtuMaster modbusMaster, ushort slaveID)
        {
            _modbusMaster = modbusMaster;
            SlaveID = slaveID;
        }

        private ModbusRtuMaster _modbusMaster;
        private ushort SlaveID = 1;

        public async Task<RequestResult<ColorSpecimen.Specimen>> ExecuteCalibrationAndMeasureAsync(CaliberMeasure mode, ushort startAddress, bool isCorr)
        {
            try
            {
                //先写入测量命令
                await _modbusMaster.WriteSingleRegistersAsync(SlaveID, (ushort)HoldingRegisterNewCommand.CalibrateMeasure, ((ushort)mode).ToNetworkOrderBytes());
                if (isCorr)
                {
                    return new RequestResult<ColorSpecimen.Specimen>(ResultType.OperationSuccess);
                }
                //再读取测量结果
               
                var RequestResult = await _modbusMaster.ReadInputRegistersAsync(SlaveID, startAddress, (mode==CaliberMeasure.Standard?(ushort) Constants.STANDARD_COUNT:(ushort) Constants.SAMPLE_COUNT));
                if (RequestResult.ResultType == ResultType.OperationSuccess)
                {
                    if(startAddress==40036)
                    {
                        var standard = StructConverter.BytesToStruct<StandardStruct>(ComBuffer.BigEndianToLittleEndian(RequestResult.Obj));
                        var toleranceTask = GetSystemToleranceAsync();
                        var toleranceResult =  toleranceTask;
                        ColorSpecimen.Specimen spec = standard.ToSpecimen(toleranceResult.Result.Obj);
                        return new RequestResult<ColorSpecimen.Specimen>(RequestResult.ResultType, RequestResult.Msg, spec);
                    }
                    else
                    {
                        var standard = StructConverter.BytesToStruct<SampleStruct>(ComBuffer.SanmpleBigEndianToLittleEndian(RequestResult.Obj));
                        var toleranceTask = GetSystemToleranceAsync();
                        var toleranceResult = toleranceTask;
                        ColorSpecimen.Specimen spec = standard.ToSpecimen();
                        return new RequestResult<ColorSpecimen.Specimen>(RequestResult.ResultType, RequestResult.Msg, spec);
                       // return new RequestResult<SampleStruct>(RequestResult.ResultType, RequestResult.Msg, standard);
                    }
                }
                return new RequestResult<ColorSpecimen.Specimen>(RequestResult.ResultType, RequestResult.Msg);
            }
            catch (Exception ex)
            {
                return new RequestResult<ColorSpecimen.Specimen>(ResultType.OperationFail, ex.Message);
            }
        }

        public async Task<RequestResult<SystemInfo>> GetSystemInfoAsync()
        {
            try
            {
                var RequestResult = await _modbusMaster.ReadInputRegistersAsync(SlaveID, (ushort)Command.SystemInfo, Constants.SYSTEM_INFO_REGISTER_COUNT);
                if (RequestResult.ResultType == ResultType.OperationSuccess)
                {
                    var sample = PacketContentConverter.ToSystemInfo(RequestResult.Obj);
                    return new RequestResult<SystemInfo>(RequestResult.ResultType, RequestResult.Msg, sample);
                }
                return new RequestResult<SystemInfo>(RequestResult.ResultType, RequestResult.Msg);
            }
            catch
            {
                return new RequestResult<SystemInfo>(ResultType.OperationFail, "exception");
            }
        }

        public async Task<RequestResult<ToleranceForYL30>> GetSystemToleranceAsync()
        {
            var result = await _modbusMaster.ReadHoldingRegistersAsync(SlaveID, (ushort)Command.SystemTolerance, Constants.SYSTEM_TOLERANCE);
            if (result.ResultType == ResultType.OperationSuccess)
            {
                return new RequestResult<ToleranceForYL30>(result.ResultType, result.Msg, PacketContentConverter.ToSystemTolerance(result.Obj));
            }
            return new RequestResult<ToleranceForYL30>(result.ResultType, result.Msg);
        }
    }
}
