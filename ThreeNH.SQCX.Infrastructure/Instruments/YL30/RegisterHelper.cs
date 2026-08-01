using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30
{
    public  class RegisterHelper
    {
        public static ushort GetHoldingRgtCount(HoldingRegisterCommand command)
        { 
           switch (command) 
            {
              case HoldingRegisterCommand.SlaveID:
              case HoldingRegisterCommand.RaudRate:
              case HoldingRegisterCommand.CommInterface:
              case HoldingRegisterCommand.DAC:
              case HoldingRegisterCommand.MeasureSpeed:
              case HoldingRegisterCommand.Calibrate:
              case HoldingRegisterCommand.Language:
              case HoldingRegisterCommand.Buzzer:
              case HoldingRegisterCommand.BackBright:
              case HoldingRegisterCommand.Backlight:
              case HoldingRegisterCommand.Illuminant:
              case HoldingRegisterCommand.Observer:
              case HoldingRegisterCommand.ColorSpace:
              case HoldingRegisterCommand.DifferenceFormula:
              case HoldingRegisterCommand.CalibrationValidTime:
              case HoldingRegisterCommand.TimeYear:
              case HoldingRegisterCommand.TimeMonth: 
              case HoldingRegisterCommand.TimeDay:
              case HoldingRegisterCommand.TimeHour:
              case HoldingRegisterCommand.TimeMinute:
              case HoldingRegisterCommand.TimeSecond:
              case HoldingRegisterCommand.MeasureControlType:
              case HoldingRegisterCommand.CalibrationType:
              case HoldingRegisterCommand.TCPPort:
              case HoldingRegisterCommand.IsShowOKNG:
              case HoldingRegisterCommand.SampleAverageCount:
              case HoldingRegisterCommand.StdAverageCount:
              case HoldingRegisterCommand.WriteStandardStatus:
              case HoldingRegisterCommand.WriteStandardMode:
              case HoldingRegisterCommand.WriteStandardSpecularComponent:
              case HoldingRegisterCommand.WriteStandardUV:
              case HoldingRegisterCommand.WriteStandardCaliber:
              case HoldingRegisterCommand.WriteStandardLens:
              case HoldingRegisterCommand.WriteStandardDataType:
              case HoldingRegisterCommand.WriteStandardLockStatus:
              case HoldingRegisterCommand.WriteStandardObserver:
              case HoldingRegisterCommand.WriteStandardIlluminant:
              case HoldingRegisterCommand.FacotoryReset:
              case HoldingRegisterCommand.ProductionReset:
              case HoldingRegisterCommand.ToolFactoryReset:
              case HoldingRegisterCommand.IntegrationTime:
              case HoldingRegisterCommand.LED:
              case HoldingRegisterCommand.InstrumentInternalType:
              case HoldingRegisterCommand.ToolLanguage:
              case HoldingRegisterCommand.Decryption:
                    return 1;
              case HoldingRegisterCommand.TCPIP:
              case HoldingRegisterCommand.TCPClientIP:
              case HoldingRegisterCommand.ProductionPassword:
              case HoldingRegisterCommand.ServoPosition:
                    return 2;
              case HoldingRegisterCommand.WriteStandardColorSpace:
              case HoldingRegisterCommand.CPUID:
                    return 6;
              case HoldingRegisterCommand.WriteStandardName:
              case HoldingRegisterCommand.PrimarySensorSN:
              case HoldingRegisterCommand.AuxiliarySensorSN:
              case HoldingRegisterCommand.WhiteBoardExternalNo:
              case HoldingRegisterCommand.HardwareVersion:
              case HoldingRegisterCommand.InstrumentName:
              case HoldingRegisterCommand.InstrumentSN:
                    return 8;
              case HoldingRegisterCommand.PrimaryCoefficient:
              case HoldingRegisterCommand.AuxiliaryCoefficient:
                    return 12;
              case HoldingRegisterCommand.WriteStandardTolerance:
              case HoldingRegisterCommand.SystemTolerance:
              case HoldingRegisterCommand.StandardTolerance:
                    return 14;
              case HoldingRegisterCommand.CorrectionFactorSCI:
              case HoldingRegisterCommand.CorrectionFactorSCE:
              case HoldingRegisterCommand.WriteStandardReflectanceSCE:
              case HoldingRegisterCommand.WriteStandardReflectanceSCI:
              case HoldingRegisterCommand.WhiteBoardReflectanceSCE:
              case HoldingRegisterCommand.WhiteBoardReflectanceSCI:
              case HoldingRegisterCommand.FactoryReflectanceSCI:
                    return 31;
               default: 
                    return 1;

            }
        
        }
        public static ushort GetInputingRgtCount(ReadInputingRegisterCommand command)
        {
            switch (command)
            {
                case ReadInputingRegisterCommand.CalibratingStatus:
                case ReadInputingRegisterCommand.ExpirationTime:
                case ReadInputingRegisterCommand.StdTimeYear:
                case ReadInputingRegisterCommand.StdTimeMonth:
                case ReadInputingRegisterCommand.StdTimeDay:
                case ReadInputingRegisterCommand.StdTimeHour:
                case ReadInputingRegisterCommand.StdTimeMinute:
                case ReadInputingRegisterCommand.StdTimeSecond:
                case ReadInputingRegisterCommand.SampleTimeYear:
                case ReadInputingRegisterCommand.SampleTimeMonth:
                case ReadInputingRegisterCommand.SampleTimeDay:
                case ReadInputingRegisterCommand.SampleTimeHour:
                case ReadInputingRegisterCommand.SampleTimeMinute:
                case ReadInputingRegisterCommand.SampleTimeSecond:

                    return 1;
                case ReadInputingRegisterCommand.SampleID:
                case ReadInputingRegisterCommand.StdID:
                case ReadInputingRegisterCommand.SampleStdID:
                    return 2;
                case ReadInputingRegisterCommand.CustomType:
                case ReadInputingRegisterCommand.SN:
                case ReadInputingRegisterCommand.SoftwareVersion:
                case ReadInputingRegisterCommand.HardwareVersion:
                case ReadInputingRegisterCommand.StdName:
                case ReadInputingRegisterCommand.SampleName:
                    return 8;
                case ReadInputingRegisterCommand.StdColorSpaceValue:
                    return 12;
                case ReadInputingRegisterCommand.SampleColorSpaceValue:
                    return 16;
                case ReadInputingRegisterCommand.StdSCI:
                case ReadInputingRegisterCommand.StdSCE:
                case ReadInputingRegisterCommand.SampleSCI:
                case ReadInputingRegisterCommand.SampleSCE:
                    return 31;                 
                default:
                    return 1;
            }
        }
    }
}
