namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 仪器连接后台线程需要 MainWindow 提供的 UI / 配置能力（阶段 1 从 DoConnectionWork 抽出）。
    /// </summary>
    internal interface IInstrumentConnectionHost
    {
        ConnectionMethod ConnectionMethod { get; }

        void SetConnectWay(string way);

        void ShowDriverDownloadDialog();

        void SetDeviceSn(string deviceSn);

        /// <summary>启动连接时传入 BackgroundWorker 的参数（如记住的上次设备）。</summary>
        object GetConnectionStartupArgument();

        /// <summary>断开前取消仪器测量通知订阅。</summary>
        void DetachMeasurementNotification(global::ThreeNH.ColorimeterInterface.IInstrument instrument);

        /// <summary>仪器 Close 完成后的 UI 收尾（状态栏、白板缓存等）。</summary>
        void OnInstrumentDisconnected();

        void ShowConnectionError(string message);
    }
}
