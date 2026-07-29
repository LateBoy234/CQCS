﻿namespace ThreeNH.ColorQualityControl.Instrument
{
    /// <summary>
    /// 仪器连接生命周期状态（阶段 3-T02），替代分散的 _isConnecting / _isSearching。
    /// 命名避开 System 等已有 ConnectionState，避免歧义。
    /// </summary>
    internal enum InstrumentConnectionState
    {
        /// <summary>未连接，可发起新连接。</summary>
        Disconnected = 0,

        /// <summary>BackgroundWorker 正在探测/打开仪器。</summary>
        Connecting = 1,

        /// <summary>Worker 阻塞等待用户选择蓝牙/ThreeNH.SQCX.Infrastructure.Instruments.YL30.Legacy 设备。</summary>
        WaitingForDeviceSelection = 2,

        /// <summary>蓝牙 5 自动扫描从设备中。</summary>
        Searching = 3,

        /// <summary>仪器已打开可用。</summary>
        Connected = 4,
    }
}
