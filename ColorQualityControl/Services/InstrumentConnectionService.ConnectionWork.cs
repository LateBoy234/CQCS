using System.ComponentModel;
using ThreeNH.ColorQualityControl.ConnectStrategies;

namespace ThreeNH.ColorQualityControl.Services
{
    internal sealed partial class InstrumentConnectionService
    {
        private void DoConnectionWork(object sender, DoWorkEventArgs args)
        {
            var worker = sender as BackgroundWorker;
            if (worker is null || _host is null)
            {
                return;
            }

            // 阶段 3-T01：按 ConnectionMethod 分派策略，避免 DoConnectionWork 继续膨胀
            var context = new InstrumentConnectContext(worker, args, _host, this);
            var strategy = InstrumentConnectStrategyRegistry.Resolve(_host.ConnectionMethod);
            strategy?.Connect(context);
        }
    }
}
