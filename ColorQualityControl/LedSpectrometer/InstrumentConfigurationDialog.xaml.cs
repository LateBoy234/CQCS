﻿﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace ThreeNH.SQCX.Infrastructure.Instruments.LedSpectrometer
{
    /// <summary>
    /// InstrumentConfigurationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InstrumentConfigurationDialog : Window
    {

        public InstrumentConfigurationDialog(Spectrometer spectrometer)
        {
            Spectrometer = spectrometer;
            InitializeComponent();
        }

        public Spectrometer Spectrometer
        {
            get; set;
        }

        private bool Accept()
        {
            if (tabControl.SelectedIndex == 0)
            {
                if (MeasurementParameterControl.Verify())
                {
                    MeasurementParameter para = MeasurementParameterControl.MeasurementParameter;
                    try
                    {
                        Spectrometer.SetMeasurementParameter(para);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format("Set measurement parameter failed: {0}", ex.Message));
                    }
                }
            }
            else if (tabControl.SelectedIndex == 1)
            {

            }
            else if (tabControl.SelectedIndex == 2)
            {
                if (WhiteBoardParameterControl.Verify())
                {
                    WhiteBoardParameter para = WhiteBoardParameterControl.WhiteBoardParameter;
                    try
                    {
                        Spectrometer.SetWhiteBoardParameter(para);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format("Set white board parameter failed: {0}", ex.Message));
                    }
                }
            }
            return false;
        }

        private void OnAcceptClick(object sender, RoutedEventArgs e)
        {
            Accept();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (Accept())
            {
                DialogResult = true;
            }
        }

        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = e.Source as TabControl;
            if (tabControl == null)
                return;

            if (tabControl.SelectedIndex == 0) // 测量参数
            {
                try
                {
                    MeasurementParameterControl.MeasurementParameter =
                        Spectrometer.GetMeasurementParameter();
                }
                catch (Exception)
                {
                    //throw new Exception(string.Format("Get measurement parameter failed"), ex);
                }
            }
            else if (tabControl.SelectedIndex == 1) // 颜色选项
            {

            }
            else if (tabControl.SelectedIndex == 2) // 白板参数
            {
                try
                {
                    WhiteBoardParameterControl.WhiteBoardParameter =
                        Spectrometer.GetWhiteBoardParameter();
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
