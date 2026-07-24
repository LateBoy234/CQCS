using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.ColorQualityControl.Converters
{
    /// <summary>
    /// L,a,b值转换为Brush
    /// </summary>
    class LabToPseudoColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 3 && values.All(x => x is double))
            {
                CIELAB lab = new CIELAB((double)values[0], (double)values[1], (double)values[2]);
                var srgb = Formula.LabToSrgb(lab);
                return new SolidColorBrush(Color.FromRgb(srgb.R, srgb.G, srgb.B));
            }

            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
