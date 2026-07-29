﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using  param($mt) $map[$mt.Value] ;
using  param($mt) $map[$mt.Value] ;
using YL30Comm;
using  param($mt) $map[$mt.Value] ;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Legacy
{
    public class Spectrometer:ISpectormeter
    {
        public Spectrometer()
        {

        }
        ModbusRtuMaster modbusMaster = new ModbusRtuMaster();
        
        public ComSetting ComSetting { get; set; }=new ComSetting();
        public ushort DAC {  get; set; }
        public MeasureSpeed MeasureSpeed { get; set; }
        public SystemSetting SystemSetting { get; set; }=new SystemSetting();
        public SystemInfo SystemInfo { get; set; } = new SystemInfo();
        public bool IsCalibration => SystemInfo.CalibrationStatus == CalibrationStatus.AllFinished;
        public ushort SlaveID=>modbusMaster.SlaveID;
        public async Task<RequestResult> OpenAsync()
        {
            try
            {
                modbusMaster.Open();
                //if (modbusMaster.IsOpen)
                //{
                //    await modbusMaster.GetSlaveID();
                //    //await modbusMaster.WriteSingleRegistersAsync(SlaveID, (ushort)HoldingRegisterCommand.CommInterface, (ushort)CommInterfaceType.USB);
                //}
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return new RequestResult(ResultType.OperationSuccess);
        }

        public async Task<RequestResult> CloseAsync()
        {
            try
            {
                modbusMaster.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return new RequestResult(ResultType.OperationSuccess);
        }
        public async Task<RequestResult> GetCommSettingAsync()
        {
            var result=await modbusMaster.ReadHoldingRegistersAsync(88, (ushort)Command.ComSetting, 3);
            // await modbusMaster.GetCommSetting();
            var results=PacketContentConverter.ToComSetting(result);
            // return new RequestResult(ResultType.OperationSuccess,"OK",results);
             return new RequestResult(ResultType.OperationSuccess,"OK"); 
        }

        public Task<RequestResult> ExecuteCalibrationAsync(Calibration mode)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ExecuteMeasureAsync(MeasureType mode)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetBackBrightnessAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetBackligthTimeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetBaudRateAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetBuzzerStatusAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetColorDifferenceFormulaAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetColorSpaceAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetCommInterfaceAsync()
        {
            throw new NotImplementedException();
        }

      
        public Task<RequestResult> GetDACAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetExpireTimeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetIPAddressAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetIPClientAddressAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetIPPortAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetLanguageAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetMeasureControlModeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetMeasureSpeedAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetObserverAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetReflectanceFactorSCEAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetReflectanceFactorSCIAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetSampleAverageMeasureCountAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetShowOKNGStatusAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetSlaveIDAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetStandardAverageMeasureCountAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetStandardIlluminantAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetStandardToleranceAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetSystemTimeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetSystemToleranceAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> GetWriteStandardStatusAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ReadCalibrationStatusAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ReadCustomTypeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ReadExpirtTimeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ReadHardwareVersionAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ReadInstrumentSNAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ReadSampleAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ReadSoftwareVersionAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> ReadStandardAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetBackBrightnessAsync(ushort Time)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetBackligthTimeAsync(ushort Time)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetBaudRateAsync(BaudRate rate)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetBuzzerStatusAsync(Buzzer status)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetColorDifferenceFormulaAsync(ColorDiffFormula diffFormula)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetColorSpaceAsync(ColorSpace space)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetCommInterfaceAsync(CommInterfaceType type)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetCommSettingAsync(ComSetting comSetting)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetDACAsync(ushort value)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetExpireTimeAsync(ushort time)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetFactoryResetAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetIPAddressAsync(IPAddress address)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetIPClientAsync(IPAddress address)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetIPPortAsync(ushort Port)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetLanguageAsync(Language language)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetMeasureControlModeAsync(MeasureControlMode mode)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetMeasureSpeedAsync(MeasureSpeed mode)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetObserverAsync(StandardObserver observer)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetReflectanceFactorSCEAsync(ushort[] factors)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetReflectanceFactorSCIAsync(ushort[] factors)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetSampleAverageMeasureCountAsync(ushort count)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetShowOKNGStatusAsync(ShowOKNG status)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetSlaveIDAsync(ushort slaveID)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetStandardAverageMeasureCountAsync(ushort count)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetStandardIlluminantAsync(StandardIlluminant illuminant)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetStandardToleranceAsync(ushort[] tolerance)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetSystemTimeAsync(DateTime time)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> SetSystemToleranceAsync(ushort[] tolerance)
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult> WriteStandardToInstrumentAsync(Specimen specimen)
        {
            throw new NotImplementedException();
        }

       
    }
}
