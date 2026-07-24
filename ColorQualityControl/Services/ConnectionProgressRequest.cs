namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>
    /// BackgroundWorker 进度回调中请求 MainWindow 弹出设备选择 UI 的类型。
    /// </summary>
    internal enum ConnectionProgressRequest
    {
        DongleSelectSlaveDevice,
        YS30BluetoothSelectDevice,
        YL30SpecialDevice
    }
}
