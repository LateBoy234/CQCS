using System;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Calibration;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.CommunicationBase;

namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>
    /// 仪器校准编排入口；具体流程由 <see cref="ICalibrationStrategy"/> 承担（阶段 3-T05）。
    /// </summary>
    internal sealed class InstrumentCalibrationService
    {
        private IInstrumentCalibrationHost _host;

        public void AttachHost(IInstrumentCalibrationHost host)
        {
            _host = host;
        }

        public bool TryRefreshCaches(IInstrument instrument, out string errorMessage)
            => BlackWhiteCalibrationRunner.TryRefreshCaches(instrument, out errorMessage);

        public bool RunReflectanceCalibration()
            => Execute(CalibrationKind.Reflectance);

        public bool RunTransmissionCalibration()
            => Execute(CalibrationKind.Transmission);

        /// <summary>菜单「校准」入口：无 ISystemSetting 时走反射黑/白，否则打开校准窗口。</summary>
        public bool RunCalibration()
        {
            var instrument = _host?.Colorimeter;
            if (instrument == null)
            {
                return false;
            }

            return CalibrationStrategyResolver.ExecuteMenuCalibration(
                new CalibrationContext(_host, instrument));
        }

        /// <summary>雾度测量前：已校准或执行透射校准。</summary>
        public bool EnsureTransmissionCalibrated()
            => Execute(CalibrationKind.Haze);

        public bool TryHandleMeasurementAbnormal(Exception ex)
        {
            if (ex is CommunicationException communicationException
                && communicationException.ErrorCode == CommunicationError.MeasurementAbnormal)
            {
                if (_host.ConfirmCalibrateAfterMeasurementAbnormal())
                {
                    return RunCalibration();
                }

                return true;
            }

            return false;
        }

        /// <summary>YH 清晰度校准（Loading 仍由 MainWindow 编排）。</summary>
        public void CalibrateClarity(IInstrument instrument)
        {
            var context = new CalibrationContext(_host, instrument);
            ClarityCalibrationStrategy.Instance.Execute(context);
        }

        private bool Execute(CalibrationKind kind)
        {
            if (_host?.Colorimeter == null)
            {
                return false;
            }

            return CalibrationStrategyResolver.Resolve(kind)
                .Execute(new CalibrationContext(_host));
        }
    }
}
