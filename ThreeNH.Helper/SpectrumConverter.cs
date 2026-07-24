using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Data;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.Helper
{
    public class SpectrumConverter : IValueConverter
    {
        static SpectrumConverter _converter = new SpectrumConverter();
        public static SpectrumConverter Converter
        {
            get { return _converter;  }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                double dVal = (double)value;
                if (double.IsNaN(dVal))
                    return "--";
                return (dVal * 100.0).ToString("F2");
            }

            Spectrum spectrum = value as Spectrum;
            if (spectrum == null) return null;

            if (parameter == null)
            {
                return string.Join(", ", Array.ConvertAll(spectrum.Data, x => (100.0 * x).ToString("F2")));
            }
            else
            {
                int wavelength = (int)parameter;
                if (spectrum.Range.Contains(wavelength))
                    return (spectrum[wavelength] * 100.0).ToString("F2");
                return "--";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
