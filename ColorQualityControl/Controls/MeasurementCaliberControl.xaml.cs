using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.Optical;

namespace ThreeNH.Controls
{
    /// <summary>
    /// MeasurementCaliberControl.xaml 的交互逻辑
    /// </summary>
    public partial class MeasurementCaliberControl : UserControl
    {
        public MeasurementCaliberControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty MeasurementCaliberProperty =
            DependencyProperty.Register("MeasurementCaliber",
                typeof(MeasurementCaliber), typeof(MeasurementCaliberControl),
                new PropertyMetadata(MeasurementCaliber.Unknown, OnPropertyChanged));

        public MeasurementCaliber MeasurementCaliber
        {
            get { return (MeasurementCaliber)GetValue(MeasurementCaliberProperty); }
            set { SetValue(MeasurementCaliberProperty, value); }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MeasurementCaliberControl;
            control.OnMeasurementCaliberChanged((MeasurementCaliber)e.NewValue);
        }

        private void OnMeasurementCaliberChanged(MeasurementCaliber newValue)
        {
            switch (newValue)
            {
                case MeasurementCaliber.Phi4:
                    phi4.IsChecked = true;
                    break;

                case MeasurementCaliber.Phi8:
                    phi8.IsChecked = true;
                    break;

                case MeasurementCaliber.Phi2:
                    customized.IsChecked = true;
                    break;

                default:
                    other.IsChecked = true;
                    break;
            }
        }
    }
}
