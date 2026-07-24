using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.CommunicationBase;
using ThreeNH.PortService;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// ConnectByBluetooth5ManualDlg.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectByBluetooth5ManualDlg : Window, INotifyPropertyChanged
    {
        //private System.Threading.SynchronizationContext _synContext;
        private SerialPort _serialPort;
        private EhMc17AtInterface _dongle;

        public EhMc17AtInterface.SlaveDevice CurrentBlueToothDevice { get; internal set; }

        private bool isSearching = false;
        public string PortName { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyOfPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public ConnectByBluetooth5ManualDlg(BthAutoConnectConfig config)
        {
            InitializeComponent();
            this.DataContext = this;
            isSearching = false;
            isConnecting = false;
            var ports = SerialPortService.SerialPorts.Where(x => IsBluetoothDonglePort(x.Description)).Select(o => o.PortName);
            cbbSerialPorts.ItemsSource = ports;
            cbbSerialPorts.SelectedItem = ports.FirstOrDefault();
            IsAutoConnectBth = config.IsAutoConnect;
            IsAutoConnectOtherInstrument = config.IsAutoConnectOtherInstrument;
            DeviceName = config.SlaveName;
        }
        private string _deviceName;

        public string DeviceName
        {
            get { return _deviceName; }
            set
            {
                _deviceName = value;
                NotifyOfPropertyChanged();
            }
        }
        public bool isConnecting { get; set; }
        private bool _isAutoConnect;
        public bool IsAutoConnectBth
        {
            get { return _isAutoConnect; }
            set
            {
                _isAutoConnect = value;
                if (value)
                {
                    if (this.deviceList != null && this.deviceList.SelectedItem != null)
                    {
                        DeviceName = ((EhMc17AtInterface.SlaveDevice)this.deviceList.SelectedItem).Name;
                    }
                }
                NotifyOfPropertyChanged();
            }
        }
        private bool _isAutoConnectOtherInstrument;
        public bool IsAutoConnectOtherInstrument
        {
            get { return _isAutoConnectOtherInstrument; }
            set
            {
                _isAutoConnectOtherInstrument = value;
                NotifyOfPropertyChanged();
            }
        }
        private bool IsBluetoothDonglePort(string description)
        {
            return string.Compare(description, "USB-SERIAL CH340", StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        private void StartSearch(object sender, RoutedEventArgs e)
        {
            try
            {
                ClosePortAndDongle();
                isSearching = true;
                _serialPort = new SerialPort(cbbSerialPorts.SelectedItem as string, 115200);
                _serialPort.Open();
                _dongle = new EhMc17AtInterface(_serialPort);
                _dongle.DeviceDiscovered += DeviceDiscovered;
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
                deviceList.Items.Clear();
                Task.Factory.StartNew(() =>
                {
                    if (_dongle.InitDongle())
                        _dongle.BeginScan();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
            }
        }

        private void ClosePortAndDongle()
        {
            try
            {
                if (_dongle != null)
                {
                    _dongle.DeviceDiscovered -= DeviceDiscovered;
                    _dongle.Dispose();
                }
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
            }
            catch
            {

            }
        }

        private void DeviceDiscovered(EhMc17AtInterface.SlaveDevice obj)
        {
            //_synContext.Send(x => { deviceList.Items.Add(obj); }, null);
            Dispatcher.Invoke(new Action(() =>
            {
                deviceList.Items.Add(obj);

                // 动态调整 Name 列的宽度
                AdjustNameColumnWidth();
            }));
        }

        /// <summary>
        /// 动态调整 Name 列的宽度，根据所有项中最长的文本
        /// </summary>
        private void AdjustNameColumnWidth()
        {
            if (deviceList.Items.Count == 0) return;

            try
            {
                // 获取 GridView 和 Name 列
                var gridView = deviceList.View as GridView;
                if (gridView == null || gridView.Columns.Count < 2) return;

                var nameColumn = gridView.Columns[1]; // Name 列是第 2 列（索引从 0 开始）

                // 计算表头宽度
                double headerWidth = MeasureTextWidth(nameColumn.Header?.ToString() ?? "", nameColumn);

                // 计算所有项中 Name 字段的最大宽度
                double maxContentWidth = 0;
                foreach (var item in deviceList.Items)
                {
                    if (item is EhMc17AtInterface.SlaveDevice device && !string.IsNullOrEmpty(device.Name))
                    {
                        double contentWidth = MeasureTextWidth(device.Name, nameColumn);
                        if (contentWidth > maxContentWidth)
                        {
                            maxContentWidth = contentWidth;
                        }
                    }
                }

                // 设置列宽为最大值（加上一些边距）
                double newWidth = Math.Max(headerWidth, maxContentWidth) + 10; // 20px 边距

                // 限制最小和最大宽度
                newWidth = Math.Max(180, Math.Min(newWidth, 400));

                nameColumn.Width = newWidth;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"AdjustNameColumnWidth error: {ex.Message}");
            }
        }

        /// <summary>
        /// 测量文本在指定列中的实际显示宽度
        /// </summary>
        private double MeasureTextWidth(string text, GridViewColumn column)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            // 创建用于测量的 TextBlock
            var textBlock = new TextBlock
            {
                Text = text,
                FontFamily = SystemFonts.CaptionFontFamily,
                FontSize = SystemFonts.CaptionFontSize,
                FontWeight = FontWeights.Normal,
                FontStyle = FontStyles.Normal,
                Margin = new Thickness(0),
                Padding = new Thickness(4, 0, 4, 0) // 单元格内边距
            };

            // 测量所需宽度
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.DesiredSize.Width;
        }

        private void StopSearch(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dongle != null)
                    _dongle.StopScan();
                btnStart.IsEnabled = true;
                btnStop.IsEnabled = false;
                isSearching = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void deviceList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (isConnecting)
            {
                return;
            }

            ConnectToSlaveDevice();
        }

        private void ConnectToSlaveDevice()
        {
            isConnecting = true;
            if (isSearching)
            {
                StopSearch(null, null);
                Thread.Sleep(500);
            }
            if (!btnStart.IsEnabled)
            {
                MessageBox.Show(S._("StopSearchingFirst"));
                return;
            }
            var device = deviceList.SelectedItem as EhMc17AtInterface.SlaveDevice;
            if (device == null)
            {
                MessageBox.Show(S._("AtLeastSelectedABluetoothDevice"));
                return;
            }
            if (!_dongle.Connect(device.Address, out var msg))
            {
                MessageBox.Show(string.Format(S._("ConnectFailed$0$"), msg));
                return;
            }
            CurrentBlueToothDevice = device;
            PortName = _serialPort.PortName;
            DialogResult = true;
            isConnecting = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            ClosePortAndDongle();
            base.OnClosed(e);
        }

        private void ConnectToSlaveDevice(object sender, RoutedEventArgs e)
        {
            ConnectToSlaveDevice();
        }
        private void devicelist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.deviceList != null && this.deviceList.SelectedItem != null)
            {
                DeviceName = ((EhMc17AtInterface.SlaveDevice)this.deviceList.SelectedItem).Name;
                CurrentBlueToothDevice = (EhMc17AtInterface.SlaveDevice)this.deviceList.SelectedItem;
            }
        }

        private void ckIsAutoConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DeviceName))
            {
                MessageBox.Show(S._("SelectedDeviceFirst"));
                return;
            }
        }
    }
}
