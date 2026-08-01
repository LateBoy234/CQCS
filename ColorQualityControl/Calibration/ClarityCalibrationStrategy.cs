using System;
using ThreeNH.SQCX.Infrastructure.Instruments.YHComm;

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
            if (instrument is USBComm usbComm)
            {
                usbComm.Calibrate(CalibrationType.CLARITY);
                return true;
            }

            if (instrument is BleComm bleComm)
            {
                bleComm.Calibrate(CalibrationType.CLARITY);
                return true;
            }

            throw new NotSupportedException();
        }
    }
}
