using ThreeNH.ColorimeterInterface;

namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>
    /// 带 ISystemSetting 的仪器：打开专用校准窗口（菜单「校准」入口）。
    /// </summary>
    internal sealed class InstrumentDialogCalibrationStrategy : ICalibrationStrategy
    {
        public static InstrumentDialogCalibrationStrategy Instance { get; } = new InstrumentDialogCalibrationStrategy();

        public CalibrationKind Kind => CalibrationKind.InstrumentDialog;

        public bool Execute(CalibrationContext context)
        {
            var host = context?.Host;
            var instrument = context?.Instrument;
            if (host == null || instrument == null)
            {
                return false;
            }

            (instrument as ISystemSetting)?.RefreshCaches();
            return host.ShowInstrumentCalibrationDialog(instrument);
        }
    }
}
