using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.ColorQualityControl.UI
{
    class SpectrumConverter : IValueConverter
    {
        public static SpectrumConverter Converter = new SpectrumConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Spectrum && parameter is int)
            {
                var spectrum = value as Spectrum;
                var wavelength = (int)parameter;

                //return spectrum.get_Item(wavelength).ToString("F3");
                return string.Format("{0:F3}", spectrum[wavelength] * 100);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ReflectanceTableControl.xaml 的交互逻辑
    /// </summary>
    public partial class ReflectanceTableControl : UserControl
    {
        public static readonly DependencyProperty SpectrumProperty;

        private WavelengthRange range = new WavelengthRange(400, 700, 10);


        static ReflectanceTableControl()
        {
            SpectrumProperty = DependencyProperty.Register("Spectrum",
                typeof(Spectrum), typeof(ReflectanceTableControl),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(
                    OnSpectrumChanged)));
        }

        public ReflectanceTableControl()
        {
            InitializeComponent();
            ResetHeader(range);
            Visibility = Visibility.Collapsed;
        }

        public static void OnSpectrumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ReflectanceTableControl control = d as ReflectanceTableControl;
            control.ShowSpectrum(e.NewValue as Spectrum);
        }

        private void ShowSpectrum(Spectrum spectrum)
        {
            if (spectrum != null)
            {
                if (Visibility != Visibility.Visible)
                    Visibility = Visibility.Visible;

                if (spectrum.Range != range)
                {
                    ResetHeader(spectrum.Range);
                }
                ReflectanceListView.Items.Clear();
                ReflectanceListView.Items.Add(spectrum);
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        public Spectrum Spectrum
        {
            set
            {
                SetValue(SpectrumProperty, value);
            }
            get
            {
                return (Spectrum)GetValue(SpectrumProperty);
            }
        }

        private void ResetHeader(WavelengthRange range)
        {
            this.range = range;

            gridView.Columns.Clear();
            for(int wavelength = range.Min; wavelength <= range.Max; wavelength += range.Step)
            {
                GridViewColumn column = new GridViewColumn();
                column.Header = string.Format("{0}nm", wavelength);

                Binding binding = new Binding();
                binding.Converter = SpectrumConverter.Converter;
                binding.ConverterParameter = wavelength;
                column.DisplayMemberBinding = binding;

                gridView.Columns.Add(column);
            }
        }
    }
}
