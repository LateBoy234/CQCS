using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Instrument;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 阶段 1 目标：仪器连接生命周期（现由 MainWindow.Instrument 中 DoConnectionWork 等承担）。
    /// </summary>
    internal interface IInstrumentConnectionService
    {
        IInstrument Colorimeter { get; }

        global::ThreeNH.ColorQualityControl.InstrumentType InstrumentType { get; }

        bool IsConnecting { get; }

        /// <summary>当前连接状态机状态（阶段 3-T02）。</summary>
        InstrumentConnectionState ConnectionState { get; }

        string DeviceSN { get; }
    }
}
