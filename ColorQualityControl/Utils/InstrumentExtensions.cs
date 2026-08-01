using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using ThreeNH.ColorimeterInterface;
using ThreeNH.SQCX.Infrastructure.Instruments.YHComm;

namespace ThreeNH.ColorQualityControl.Utils
{
    public static class InstrumentExtensions
    {

        public static bool IsColorHazeMeter(this IInstrument instrument)
        {
            return (instrument is USBComm usbComm && usbComm.IsColorHazeMeter)
                || (instrument is BleComm bleComm && bleComm.IsColorHazeMeter);
        }

        public static bool IsClarityValid(this IInstrument instrument)
        {
            return (instrument is USBComm usbComm && usbComm.IsClarityValid)
                || (instrument is BleComm bleComm && bleComm.IsClarityValid);
        }

        public static double MeasureClarity(this IInstrument instrument)
        {
            if (instrument is USBComm usbComm)
                return usbComm.MeasureClarity();
            else if (instrument is BleComm bleComm)
                return bleComm.MeasureClarity();
            return double.NaN;
         
        }
    }
}
