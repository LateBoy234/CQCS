using System.Windows;
using ThreeNH.ColorimeterInterface;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// DeviceInfoDlg.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceInfoDlg : Window
    {
        public DeviceInfoDlg(IDeviceInfo deviceInfo)
        {
            InitializeComponent();
            (deviceInfo as IColorimeter)?.RefreshInstrumentInfo();
            DataContext = deviceInfo;
        }
    }
}
