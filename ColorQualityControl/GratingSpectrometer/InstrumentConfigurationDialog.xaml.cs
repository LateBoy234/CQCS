using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ThreeNH.GratingSpectrometer
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
                if (ChromaticityOperationControl.Verify())
                {
                    ChromaticityOperations operations = ChromaticityOperationControl.ChromaticityOperations;
                    try
                    {
                        Spectrometer.SetChromaticityOperations(operations);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
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
            if (Accept())
            {
                MessageBox.Show("Operation successful");
            }
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
                try
                {
                    ChromaticityOperationControl.ChromaticityOperations =
                        Spectrometer.GetChromaticityOperations();
                }
                catch (Exception ex)
                {

                    //throw;
                    MessageBox.Show(ex.Message);

                }
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
