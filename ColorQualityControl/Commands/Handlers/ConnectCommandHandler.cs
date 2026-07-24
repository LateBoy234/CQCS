using System;
using System.Windows.Input;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Services;

// 文件夹为 Commands/Handlers；命名空间避开与静态类 Commands 冲突（阶段4-T01）
namespace ThreeNH.ColorQualityControl.CommandHandlers
{
    /// <summary>
    /// 连接/断开 RoutedCommand 编排；业务已在 InstrumentConnectionService（阶段4-T01）。
    /// </summary>
    internal sealed class ConnectCommandHandler
    {
        private readonly IConnectCommandHost _host;
        private readonly InstrumentConnectionService _instrument;

        public ConnectCommandHandler(IConnectCommandHost host, InstrumentConnectionService instrument)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _instrument = instrument ?? throw new ArgumentNullException(nameof(instrument));
        }

        public void ExecuteConnect(object sender, ExecutedRoutedEventArgs e)
        {
            _instrument.ConnectFromUserCommand();
        }

        public void CanExecuteConnect(object sender, CanExecuteRoutedEventArgs e)
        {
            var colorimeter = _host.Colorimeter;
            e.CanExecute = (colorimeter == null || !colorimeter.IsOpen)
                && !_instrument.IsWorkerBusy
                && !_instrument.IsConnecting;
        }

        public void ExecuteDisconnect(object sender, ExecutedRoutedEventArgs e)
        {
            _instrument.Disconnect();
        }

        /// <summary>
        /// Disconnect / Measure 等依赖「已连接」的共享 CanExecute；断开时刷新状态栏避免残留「已连接」。
        /// </summary>
        public void CanExecuteWhenConnected(object sender, CanExecuteRoutedEventArgs e)
        {
            var colorimeter = _host.Colorimeter;
            e.CanExecute = colorimeter != null && colorimeter.IsOpen;
            if (!e.CanExecute && _host.IsWindowLoaded)
            {
                _host.UpdateConnectionStatus();
            }
        }
    }
}
