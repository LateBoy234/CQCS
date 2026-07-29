﻿﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter;

namespace ThreeNH.ColorQualityControl.YRColorimeter
{
    /// <summary>
    /// InstrumentInfoDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InstrumentInfoDialog : Window
    {
        private ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter.Colorimeter _colorimeter;

        public InstrumentInfoDialog(ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter.Colorimeter colorimeter)
        {
            _colorimeter = colorimeter;

            InitializeComponent();
            DataContext = this;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_colorimeter != null)
            {
                try
                {
                    DeviceInfo = _colorimeter.GetDeviceInfo();
                }
                catch (Exception ex)
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
            set
            {
                connection = value;
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
