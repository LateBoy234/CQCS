using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.YL30.CommunicationContract;
using ThreeNH.YL30.DataHandle;
using ThreeNH.YL30.Extensions;
using YL30;
using YL30.Model;
using YL30Comm.Result;

namespace ThreeNH.YL30.CommunicaImplementation
{
    internal class ModbusCommunication : ISpectrometerCom
    {
        public ModbusCommunication(IModbusMaster LanMaster, ushort slaveid)
        {
            _lanMaster = LanMaster;
            SlaveID = slaveid;
        }
        private IModbusMaster _lanMaster;
        private ushort SlaveID = 1;
        public async Task<RequestResult<ColorSpecimen.Specimen>> ExecuteCalibrationAndMeasureAsync(CaliberMeasure mode, ushort startAddress, bool isCorr)
        {
            try
            {
                //先写入测量命令
                await _lanMaster.WriteSingleRegisterAsync((byte)SlaveID, (ushort)HoldingRegisterNewCommand.CalibrateMeasure, ((ushort)mode));
                if (isCorr)
                {
                    return new RequestResult<ColorSpecimen.Specimen>(ResultType.OperationSuccess);
                }
                //再读取测量结果
                var RequestResult = await _lanMaster.ReadInputRegistersAsync((byte)SlaveID, startAddress, (mode == CaliberMeasure.Standard ? (ushort)Constants.STANDARD_COUNT : (ushort)Constants.SAMPLE_COUNT));
                if (RequestResult != null && RequestResult.Length > 0)
                {
                    var result = RequestResult.SelectMany(BitConverter.GetBytes).ToList();
                    if (startAddress == 40036)
                    {
                        var standard = StructConverter.BytesToStruct<StandardStruct>(result);
                        var toleranceTask = GetSystemToleranceAsync();
                        var toleranceResult = toleranceTask;
                        ColorSpecimen.Specimen spec = standard.ToSpecimen(toleranceResult.Result.Obj);
                        return new RequestResult<ColorSpecimen.Specimen>(ResultType.OperationSuccess, "", spec);
                    }
                    else
                    {
                        var standard = StructConverter.BytesToStruct<SampleStruct>(result);
                        var toleranceTask = GetSystemToleranceAsync();
                        var toleranceResult = toleranceTask;
                        ColorSpecimen.Specimen spec = standard.ToSpecimen();
                        return new RequestResult<ColorSpecimen.Specimen>(ResultType.OperationSuccess, "", spec);
                    }
                }
                return new RequestResult<ColorSpecimen.Specimen>(ResultType.OperationFail, "");
            }
            catch (Exception ex)
            {
                return new RequestResult<ColorSpecimen.Specimen>(ResultType.OperationFail, ex.Message);
            }
        }


        public async Task<RequestResult<SystemInfo>> GetSystemInfoAsync()
        {
            var value = await _lanMaster.ReadInputRegistersAsync((byte)SlaveID, (ushort)Command.SystemInfo, Constants.SYSTEM_INFO_REGISTER_COUNT);
            if (value != null && value.Length > 0)
            {
                var result = value.ToBytes();
                var sample = PacketContentConverter.ToSystemInfo(result.ToList());
                if (sample != null)
                {
                    return new RequestResult<SystemInfo>(ResultType.OperationSuccess, "OperationSuccessfully", sample);
                }
                return new RequestResult<SystemInfo>(ResultType.OperationFail);
            }
            else
            {
                return new RequestResult<SystemInfo>(ResultType.OperationFail);
            }
        }

        public async Task<RequestResult<ToleranceForYL30>> GetSystemToleranceAsync()
        {
            var value = await _lanMaster.ReadHoldingRegistersAsync((byte)SlaveID, (ushort)Command.SystemTolerance, Constants.SYSTEM_TOLERANCE);
            if (value != null && value.Length > 0)
            {
                var result = value.ToBytes();
                //var result = value.ToBigEndianBytes();
                var condition = PacketContentConverter.ToSystemTolerance(result.ToList());

                return new RequestResult<ToleranceForYL30>(ResultType.OperationSuccess, "OperationSuccessfully", condition);
            }
            else
            {
                return new RequestResult<ToleranceForYL30>(ResultType.OperationFail);
            }
        }
    }
}
