using System;

namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>YH 清晰度校准（USB / BLE）。</summary>
    internal sealed class ClarityCalibrationStrategy : ICalibrationStrategy
    {
        public static ClarityCalibrationStrategy Instance { get; } = new ClarityCalibrationStrategy();

        public CalibrationKind Kind => CalibrationKind.Clarity;

        public bool Execute(CalibrationContext context)
        {
            var instrument = context?.Instrument;
            if (instrument is ThreeNH.YHComm.USBComm usbComm)
            {
                usbComm.Calibrate(ThreeNH.YHComm.CalibrationType.CLARITY);
                return true;
            }

            if (instrument is ThreeNH.YHComm.BleComm bleComm)
            {
                bleComm.Calibrate(ThreeNH.YHComm.CalibrationType.CLARITY);
                return true;
            }

            throw new NotSupportedException();
        }
    }
}
