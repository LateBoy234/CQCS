using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// SelectBlueToothDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SelectBlueToothDialog : Window, INotifyPropertyChanged
    {
        public SelectBlueToothDialog(EhMc17AtInterface.SlaveDevice[] devices)
        {
            _BlueToothDevices = devices;

            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private EhMc17AtInterface.SlaveDevice _currentSlaveDevice;
        private EhMc17AtInterface.SlaveDevice[] _BlueToothDevices;

        public EhMc17AtInterface.SlaveDevice CurrentBlueToothDevice { 
            get => _currentSlaveDevice;
            set
            {
                if (value != _currentSlaveDevice)
                {
                    _currentSlaveDevice = value;
                    RaisePropertyChanged(nameof(CurrentBlueToothDevice));

                    btnOK.IsEnabled = _currentSlaveDevice != null;
                }
            }
        }

        public EhMc17AtInterface.SlaveDevice[] BlueToothDevices
        {
            get => _BlueToothDevices;
            set
            {
                _BlueToothDevices = value;
                RaisePropertyChanged(nameof(BlueToothDevices));
            }
        }
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ListViewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConnectToBluetoothSerialPortDevice();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            ConnectToBluetoothSerialPortDevice();
        }

        private void ConnectToBluetoothSerialPortDevice()
        {
            if (CurrentBlueToothDevice == null)
            {
                MessageBox.Show(S._("s_PleaseSelectedADevice"));
                return;
            }
            DialogResult = true;
        }

        private void cancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
