namespace ThreeNH.Instruments.Connection
{
    /// <summary>
    /// Owns the transport-independent connection lifecycle of an instrument.
    /// The connection descriptor is intentionally opaque so protocol details stay
    /// in infrastructure adapters.
    /// </summary>
    public interface IInstrumentConnection
    {
        /// <summary>
        /// 仪器是否已连接
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// 打开仪器连接
        /// </summary>
        /// <param name="commDevice">使用的通讯设备</param>
        void Open(object commDevice = null);

        /// <summary>
        /// 关闭仪器连接
        /// </summary>
        void Close();
    }
}
