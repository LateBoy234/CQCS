using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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
using ThreeNH.ColorQualityControl.MultiHelper;
using ThreeNH.CommunicationBase;
using ThreeNH.PortService;
using static ThreeNH.CommunicationBase.EnumDefines;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// YL30ConnectDlg.xaml 的交互逻辑
    /// </summary>
    public partial class YL30ConnectDlg : Window, INotifyPropertyChanged
    {
        public YL30ConnectDlg()
        {
            
            InitializeComponent();
            this.DataContext = this;
            CurrentYL30Device = YL30Communication.Load("YL30Config.xml");
            InitializeControls();
            
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyOfPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public YL30Communication CurrentYL30Device { get; internal set; }
        private void InitializeControls()
        {
            //CurrentYL30Device = new YL30Communication();
            

            var typelist = EnumExt.GetAllDescriptions<CommInterfaceType>().Select(x => x.Item1).ToList();
            cb_connetionType.ItemsSource = typelist;

            SerialPortList = new ObservableCollection<string>(SerialPortService.SerialPortNames);
            BaudRateList = new ObservableCollection<string>(
                                 EnumExt.GetAllDescriptions<BaudRate>().Select(x => x.Item1));
            DataBitList = new ObservableCollection<string>(
                                 EnumExt.GetAllDescriptions<DataBits>().Select(x => x.Item1));
            StopBitList = new ObservableCollection<string>(
                                 EnumExt.GetAllDescriptions<StopBit>().Select(x => x.Item1));
            ParityList = new ObservableCollection<string>(
                                 EnumExt.GetAllDescriptions<Parity>().Select(x => x.Item1));


            //初始选中值
            BaudRate = EnumDefines.BaudRate._9600.Description();

            SerialComPort = SerialPortList.FirstOrDefault();



            if (CurrentYL30Device != null)
            {
                SelectedCommType = CurrentYL30Device.CommType.Description();
                SerialComPort = CurrentYL30Device.SerialComPort;
                IP = CurrentYL30Device.IP;
                Port = CurrentYL30Device.Port;
                BaudRate = CurrentYL30Device.BaudRate.Description();
                StopBit = CurrentYL30Device.StopBit.Description();
                Parity = CurrentYL30Device.Parity.Description();
                DataBits = CurrentYL30Device.DataBits.Description();
                SlaveID = CurrentYL30Device.LastSlaveID;
            }
            else
            {
                SelectedCommType = EnumDefines.CommInterfaceType.USB.Description();
                IP = "192.168.3.120";
                Port = 3636;
                this.BaudRate = EnumDefines.BaudRate._115200.Description();
                this.StopBit = EnumDefines.StopBit.StopBit1.Description();
                this.DataBits = EnumDefines.DataBits._8.Description();
                this.Parity = EnumDefines.Parity.NoneParity.Description();
                SlaveID = 88;
            }
           
        }

        #region property
        public ObservableCollection<string> SerialPortList { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> BaudRateList { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> DataBitList { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ParityList { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> StopBitList { get; set; } = new ObservableCollection<string>();

        private string _selectedcommType;

        public string SelectedCommType
        {
            get { return _selectedcommType; }
            set
            {
                _selectedcommType = value;
                if(value== CommInterfaceType.Socket.Description())
                {
                    slaveIDPanel.Visibility = Visibility.Collapsed;
                    Port = 3636;
                }
                else
                {
                    slaveIDPanel.Visibility = Visibility.Visible;
                    Port = 8080;
                }
                NotifyOfPropertyChanged();
            }
        }

        private string _serialComPort;

        public string SerialComPort
        {
            get { return _serialComPort; }
            set
            {
                _serialComPort = value;
                NotifyOfPropertyChanged();
            }
        }

        private string _ip;

        public string IP
        {
            get { return _ip; }
            set
            {
                _ip = value;
                NotifyOfPropertyChanged();
            }
        }
        private int _port;

        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                NotifyOfPropertyChanged();
            }
        }
        private string _baurate;

        public string BaudRate
        {
            get { return _baurate; }
            set
            {
                _baurate = value;
                NotifyOfPropertyChanged();
            }
        }
        private string _dataBits;

        public string DataBits
        {
            get { return _dataBits; }
            set
            {
                _dataBits = value;
                NotifyOfPropertyChanged();
            }
        }
        private string _parity;

        public string Parity
        {
            get { return _parity; }
            set
            {
                _parity = value;
                NotifyOfPropertyChanged();
            }
        }

        private string _stopBit;

        public string StopBit
        {
            get { return _stopBit; }
            set
            {
                _stopBit = value;
                NotifyOfPropertyChanged();
            }
        }

        private ushort _slaveID;

        public ushort SlaveID
        {
            get { return _slaveID; }
            set
            {
                _slaveID = value;
                NotifyOfPropertyChanged();
            }
        }
        private bool isConnecting;

        public bool IsConnecting
        {
            get { return isConnecting; }
            set
            {
                isConnecting = value;
                NotifyOfPropertyChanged();
            }
        }
        #endregion



        private async void btOK_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCommType == CommInterfaceType.TCPIP.Description()|| SelectedCommType == CommInterfaceType.Socket.Description())
            {
                if (!CommHelper.ValidateIPv4Regex(IP))
                {
                    MessageBox.Show(S._("DataFormatError")); 
                    return;
                }
                IsConnecting = true;
                bool isIPAvailable = await CommHelper.IsIPReachableAsync(IP);
                IsConnecting = false;
                if (!isIPAvailable)
                {
                    MessageBox.Show(S._("InvalidParameter")); 
                    return;
                }
                CurrentYL30Device.IP = this.IP;
                CurrentYL30Device.Port = this.Port;
            }
            else
            {
                CurrentYL30Device.SerialComPort = this.SerialComPort;
                CurrentYL30Device.BaudRate = EnumExt.GetEnumValueFromDescription<BaudRate>(this.BaudRate);
                CurrentYL30Device.StopBit = EnumExt.GetEnumValueFromDescription<StopBit>(this.StopBit);
                CurrentYL30Device.DataBits = EnumExt.GetEnumValueFromDescription<DataBits>(this.DataBits);
                CurrentYL30Device.Parity = EnumExt.GetEnumValueFromDescription<Parity>(this.Parity);
            }
            CurrentYL30Device.LastSlaveID = this.SlaveID;
            CurrentYL30Device.CommType = EnumExt.GetEnumValueFromDescription<CommInterfaceType>(this.SelectedCommType);
            CurrentYL30Device.Save("YL30Config.xml");
            this.DialogResult = true;   
        }

        private void btCancle_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;  
        }
    }
}
