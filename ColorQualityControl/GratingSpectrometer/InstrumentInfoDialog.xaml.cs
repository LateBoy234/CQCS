using System;
using System.ComponentModel;
using System.Windows;
using ThreeNH.ColorQualityControl;

namespace ThreeNH.GratingSpectrometer
{
    /// <summary>
    /// InstrumentInfoDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InstrumentInfoDialog : Window, INotifyPropertyChanged
    {
        private Spectrometer _spectrometer;

        public InstrumentInfoDialog(Spectrometer spectrometer)
        {
            _spectrometer = spectrometer;

            InitializeComponent();
            DataContext = this;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_spectrometer != null)
            {
                try
                {
                    DeviceInfo = _spectrometer.GetDeviceInfo();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(string.Format(S._("Format_GetDeviceInfoFailed_0"), ex.Message));
                }
            }
        }

        private DeviceInfo info;

        public event PropertyChangedEventHandler PropertyChanged;

        public DeviceInfo DeviceInfo
        {
            get { return info; }
            set
            {
                info = value;
                if (info == null)
                {
                    Connection = S._("Label_Disconnected");
                }
                else
                {
                    Connection = S._("Label_Connected");
                }
                OnPropertyChanged("DeviceInfo");
            }
        }

        string connection;
        public string Connection
        {
            get { return connection; }
            set { connection = value;
                OnPropertyChanged("Connection");
            }
        }

        void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
