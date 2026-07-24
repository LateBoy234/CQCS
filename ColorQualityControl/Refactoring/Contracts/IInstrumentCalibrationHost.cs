using ThreeNH.ColorimeterInterface;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 仪器校准流程中需由 MainWindow 提供的 UI（阶段1-T23）。
    /// </summary>
    internal interface IInstrumentCalibrationHost
    {
        IInstrument Colorimeter { get; }

        bool ConfirmBlackCalibration(bool isTransmission);

        bool ConfirmWhiteCalibration(bool isTransmission);

        void ShowCalibrationCompleted();

        void ShowBlackCalibrationFailed(string exMessage);

        void ShowWhiteCalibrationFailed(string exMessage);

        void ShowRefreshInstrumentInfoFailed(string message);

        void ShowCommunicationError(string errorCodeKey);

        /// <summary>带 ISystemSetting 的仪器使用专用校准窗口。</summary>
        bool ShowInstrumentCalibrationDialog(IInstrument colorimeter);

        /// <summary>测量异常时询问是否重新校准。</summary>
        bool ConfirmCalibrateAfterMeasurementAbnormal();
    }
}
