using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreeNH.CommDevice;
using ThreeNH.PortService;
using System.Collections.ObjectModel;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// SelectBluetoothDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectSerialPortDialog : Window, INotifyPropertyChanged
    {

        public SerialPortInformation SelectedSerialPort { get; set; }

        public SelectSerialPortDialog()
        {
            InitializeComponent();
            InitPortDevices();
            DataContext = this;
        }

        private void InitPortDevices()
        {
            var ports = SerialPortService.SuitableSerialPorts;
            foreach (var port in ports)
            {
                PortDevices.Add(port);
            }
            SerialPortService.PortsChanged += PortsChanged;
        }

        private void PortsChanged(object sender, PortsChangedArgs e)
        {
            var port = e.SerialPorts[0];
            if (!SerialPortDevice.PortDescriptions.Contains(port.Description)) return;
            switch (e.EventType)
            {
                case EventType.Insertion:
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        PortDevices.Add(port);
                    }));
                    break;
                case EventType.Removal:
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        PortDevices.Remove(port);
                    }));
                    break;
            }
            RaisePropertyChanged(nameof(PortDevices));
        }

        private ObservableCollection<SerialPortInformation> _PortDevices;
        public ObservableCollection<SerialPortInformation> PortDevices
        {
            get
            {
                if (_PortDevices == null)
                    _PortDevices = new ObservableCollection<SerialPortInformation>();
                return _PortDevices;
            }
            set
            {
                _PortDevices = value;
                RaisePropertyChanged(nameof(PortDevices));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var device = SelectSerialPortListView.SelectedItem as SerialPortInformation;
            if (device != null)
            {
                SelectedSerialPort = device;
                DialogResult = true;
            }
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            var device = SelectSerialPortListView.SelectedItem as SerialPortInformation;
            if (device != null)
            {
                SelectedSerialPort = device;
                DialogResult = true;
            }
        }

        private void cancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }


}
