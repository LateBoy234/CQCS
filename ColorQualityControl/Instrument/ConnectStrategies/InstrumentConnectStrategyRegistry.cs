namespace ThreeNH.ColorQualityControl.ConnectStrategies
{
    /// <summary>
    /// 按 <see cref="ConnectionMethod"/> 解析连接策略实例。
    /// </summary>
    internal static class InstrumentConnectStrategyRegistry
    {
        private static readonly IInstrumentConnectStrategy[] All =
        {
            new UsbInstrumentConnectStrategy(),
            new Bluetooth4InstrumentConnectStrategy(),
            new Bluetooth5InstrumentConnectStrategy(),
            new Yl30InstrumentConnectStrategy(),
        };

        internal static IInstrumentConnectStrategy Resolve(ConnectionMethod method)
        {
            foreach (var strategy in All)
            {
                if (strategy.Method == method)
                {
                    return strategy;
                }
            }

            return null;
        }
    }
}
