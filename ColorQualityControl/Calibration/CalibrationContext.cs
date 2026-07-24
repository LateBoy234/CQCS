using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;

namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>校准策略执行上下文。</summary>
    internal sealed class CalibrationContext
    {
        public CalibrationContext(IInstrumentCalibrationHost host, IInstrument instrument = null)
        {
            Host = host;
            Instrument = instrument ?? host?.Colorimeter;
        }

        public IInstrumentCalibrationHost Host { get; }

        public IInstrument Instrument { get; }
    }
}
