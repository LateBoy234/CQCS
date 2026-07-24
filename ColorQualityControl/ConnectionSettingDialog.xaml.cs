using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.PortService;

namespace ThreeNH.ColorQualityControl
{
    public enum InstrumentCatalog {
        Tnh,
        CR400,
    }

    /// <summary>
    /// ConnectionSettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectionSettingDialog : Window, INotifyPropertyChanged
    {
        private string _portName;
        private int _baudRate = 9600;
        private Parity _parity = Parity.None;
        private StopBits _stopBits = StopBits.One;
        private InstrumentCatalog _instrumentCatalog = InstrumentCatalog.Tnh;

        public ConnectionSettingDialog()
        {
            PortNames = SerialPort.GetPortNames();
            BaudRates = new int[]
            {
                4800,
                9600,
                14400,
                19200,
                115200,
            };
            Parities = new Parity[]
            {
                Parity.None,
                Parity.Odd,
                Parity.Even,
                Parity.Mark,
                Parity.Space
            };
            StopBitsItems = new StopBits[]
            {
                StopBits.None,
                StopBits.One,
                StopBits.OnePointFive,
                StopBits.Two
            };


            InitializeComponent();
            DataContext = this;
        }

        public InstrumentCatalog InstrumentCatalog
        {
            get => _instrumentCatalog;
            set
            {
                if (value == _instrumentCatalog) return;
                _instrumentCatalog = value;
                OnPropertyChanged();
            }
        }

        public StopBits[] StopBitsItems { get; set; }

        public Parity[] Parities { get; set; }

        public string[] PortNames { get; private set; }

        public int[] BaudRates { get; private set; }

        public string PortName
        {
            get => _portName;
            set
            {
                if (value == _portName) return;
                _portName = value;
                OnPropertyChanged();
            }
        }

        public int BaudRate
        {
            get => _baudRate;
            set
            {
                if (value == _baudRate) return;
                _baudRate = value;
                OnPropertyChanged();
            }
        }

        public Parity Parity
        {
            get => _parity;
            set
            {
                if (value == _parity) return;
                _parity = value;
                OnPropertyChanged();
            }
        }

        public StopBits StopBits
        {
            get => _stopBits;
            set
            {
                if (value == _stopBits) return;
                _stopBits = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
