using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using ThreeNH.ColorimeterInterface;

namespace ThreeNH.ColorQualityControl.Utils
{
    public static class InstrumentExtensions
    {

        public static bool IsColorHazeMeter(this IInstrument instrument)
        {
            return (instrument is ThreeNH.YHComm.USBComm usbComm && usbComm.IsColorHazeMeter)
                || (instrument is ThreeNH.YHComm.BleComm bleComm && bleComm.IsColorHazeMeter);
        }

        public static bool IsClarityValid(this IInstrument instrument)
        {
            return (instrument is ThreeNH.YHComm.USBComm usbComm && usbComm.IsClarityValid)
                || (instrument is ThreeNH.YHComm.BleComm bleComm && bleComm.IsClarityValid);
        }

        public static double MeasureClarity(this IInstrument instrument)
        {
            if (instrument is ThreeNH.YHComm.USBComm usbComm)
                return usbComm.MeasureClarity();
            else if (instrument is ThreeNH.YHComm.BleComm bleComm)
                return bleComm.MeasureClarity();
            return double.NaN;
         
        }
    }
}
