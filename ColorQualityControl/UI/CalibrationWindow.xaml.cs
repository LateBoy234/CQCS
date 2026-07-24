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
using ThreeNH.ColorimeterInterface;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// CalibrationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CalibrationWindow : Window
    {
        private IInstrument _instrument;
        public CalibrationWindow(IInstrument instrument)
        {
            InitializeComponent();
            _instrument = instrument;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Page page = new BlackCalibrationPage(_instrument);
            CalibrationPage.Content = page;
        }
    }
}
