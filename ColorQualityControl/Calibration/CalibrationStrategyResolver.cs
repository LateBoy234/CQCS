using System;
using ThreeNH.ColorimeterInterface;
using ThreeNH.CommunicationBase;

namespace ThreeNH.ColorQualityControl.Calibration
{
    /// <summary>
    /// 按校准场景解析策略；菜单「校准」按是否 ISystemSetting 分流。
    /// </summary>
    internal static class CalibrationStrategyResolver
    {
        internal static ICalibrationStrategy Resolve(CalibrationKind kind)
        {
            switch (kind)
            {
                case CalibrationKind.Reflectance:
                    return ReflectanceCalibrationStrategy.Instance;
                case CalibrationKind.Transmission:
                    return TransmissionCalibrationStrategy.Instance;
                case CalibrationKind.Haze:
                    return HazeCalibrationStrategy.Instance;
                case CalibrationKind.Clarity:
                    return ClarityCalibrationStrategy.Instance;
                case CalibrationKind.InstrumentDialog:
                    return InstrumentDialogCalibrationStrategy.Instance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// <summary>菜单校准：有系统设置界面的仪器开对话框，否则走反射黑/白。</summary>
        internal static ICalibrationStrategy ResolveForMenuCalibration(IInstrument instrument)
        {
            return instrument is ISystemSetting
                ? (ICalibrationStrategy)InstrumentDialogCalibrationStrategy.Instance
                : ReflectanceCalibrationStrategy.Instance;
        }

        /// <summary>执行菜单校准并处理通信异常提示。</summary>
        internal static bool ExecuteMenuCalibration(CalibrationContext context)
        {
            var host = context?.Host;
            var instrument = context?.Instrument;
            if (host == null || instrument == null)
            {
                return false;
            }

            var strategy = ResolveForMenuCalibration(instrument);
            if (strategy.Kind == CalibrationKind.Reflectance)
            {
                try
                {
                    return strategy.Execute(context);
                }
                catch (CommunicationException ex)
                {
                    host.ShowCommunicationError(ex.ErrorCode.ToString());
                    return false;
                }
            }

            return strategy.Execute(context);
        }
    }
}
