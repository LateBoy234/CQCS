﻿﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ThreeNH.ColorQualityControl;

namespace ThreeNH.SQCX.Infrastructure.Instruments.LedSpectrometer
{
    internal class CalibrationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? S._("Label_Calibrated") : S._("Label_Uncalibrated");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// InstrumentStatus.xaml 的交互逻辑
    /// </summary>
    public partial class InstrumentStatusDialog : Window
    {
        public static readonly DependencyProperty DeviceInfoProperty =
            DependencyProperty.Register("DeviceInfo", typeof(DeviceInfo), typeof(InstrumentStatusDialog));
        public static readonly DependencyProperty ConnectionProperty =
            DependencyProperty.Register("Connection", typeof(string), typeof(InstrumentStatusDialog));


        Spectrometer _spectrometer;
        public InstrumentStatusDialog(Spectrometer spectrometer)
        {
            _spectrometer = spectrometer;

            InitializeComponent();
            DataContext = this;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_spectrometer == null)
                return;

            try
            {
                DeviceInfo = _spectrometer.GetDeviceInfo();
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format(S._("Format_GetDeviceInfoFailed_0"), ex.Message));
            }

        }

        public DeviceInfo DeviceInfo
        {
            get { return (DeviceInfo)GetValue(DeviceInfoProperty); }
            set
            {
                SetValue(DeviceInfoProperty, value);
                Connection = value == null ? S._("Label_Disconnected") : S._("Label_Connected");
            }
        }

        public string Connection
        {
            get { return (string)GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }
    }
}
