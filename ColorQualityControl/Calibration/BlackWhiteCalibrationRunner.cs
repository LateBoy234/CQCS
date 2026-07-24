using System;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;

namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>
    /// 黑/白校准共用步骤（反射与透射仅提示文案不同）。
    /// </summary>
    internal static class BlackWhiteCalibrationRunner
    {
        internal static bool Run(CalibrationContext context, bool isTransmission)
        {
            var host = context?.Host;
            var instrument = context?.Instrument;
            if (host == null || instrument == null)
            {
                return false;
            }

            if (!TryRefreshCaches(instrument, out var refreshError))
            {
                host.ShowRefreshInstrumentInfoFailed(refreshError);
                return false;
            }

            if (!host.ConfirmBlackCalibration(isTransmission))
            {
                return false;
            }

            try
            {
                instrument.DoBlackCalibration();
            }
            catch (Exception ex)
            {
                host.ShowBlackCalibrationFailed(ex.Message);
                return false;
            }

            if (!host.ConfirmWhiteCalibration(isTransmission))
            {
                return false;
            }

            try
            {
                instrument.DoWhiteCalibration();
            }
            catch (Exception ex)
            {
                host.ShowWhiteCalibrationFailed(ex.Message);
                return false;
            }

            host.ShowCalibrationCompleted();
            return true;
        }

        internal static bool TryRefreshCaches(IInstrument instrument, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                (instrument as ISystemSetting)?.RefreshCaches();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
