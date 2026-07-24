using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Services;

namespace ThreeNH.ColorQualityControl.ConnectStrategies
{
    internal sealed class Bluetooth4InstrumentConnectStrategy : IInstrumentConnectStrategy
    {
        public ConnectionMethod Method => ConnectionMethod.ByBluettooth_4;

        public void Connect(InstrumentConnectContext context)
        {
            context.Host.SetConnectWay("Bluetooth");
            context.ReportProgress("Trying connecting to instrument by BLE...");
            var result = InstrumentConnectSupport.TryConnectBtsBle();
            if (result != null)
            {
                context.SetResult(result);
                return;
            }

            var address = context.Address;
            if (address == null)
            {
                context.ReportProgress("Waiting for selecting a bluetooth device...");
                context.ResetDeviceSelectionWait();
                context.ReportProgress(ConnectionProgressRequest.YS30BluetoothSelectDevice);
                context.WaitForDeviceSelection();
                if (context.SelectedDevice == null)
                {
                    context.SetResult(null);
                    return;
                }

                address = context.SelectedDevice;
            }

            context.ReportProgress("Trying connecting to selected bluetooth device...");
            context.SetResult(InstrumentConnectSupport.TryConnectYS30Bluetooth(address));
        }
    }
}
