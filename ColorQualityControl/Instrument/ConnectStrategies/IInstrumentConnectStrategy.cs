namespace ThreeNH.ColorQualityControl.ConnectStrategies
{
    /// <summary>
    /// 阶段 3-T01：按连接方式拆分 DoConnectionWork 分支，便于新增连接类型与单测。
    /// </summary>
    internal interface IInstrumentConnectStrategy
    {
        ConnectionMethod Method { get; }

        void Connect(InstrumentConnectContext context);
    }
}
