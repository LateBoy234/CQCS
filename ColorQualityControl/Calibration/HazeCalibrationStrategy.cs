namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>
    /// 雾度测量前确保透射已校准：已校准则跳过，否则跑透射黑/白。
    /// </summary>
    internal sealed class HazeCalibrationStrategy : ICalibrationStrategy
    {
        public static HazeCalibrationStrategy Instance { get; } = new HazeCalibrationStrategy();

        public CalibrationKind Kind => CalibrationKind.Haze;

        public bool Execute(CalibrationContext context)
        {
            var instrument = context?.Instrument;
            if (instrument == null)
            {
                return false;
            }

            if (instrument.IsCalibrated)
            {
                return true;
            }

            return TransmissionCalibrationStrategy.Instance.Execute(context);
        }
    }
}
