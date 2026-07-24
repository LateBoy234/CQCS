using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.Helper
{
    public class AbsorbanceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length == 2)
            {
                if (values[0] is Spectrum spectrum && values[1] is bool transmitt && parameter is int waveLength)
                {
                    if (!transmitt) return "--";
                    var temp = Formula.GetColorValueSWLAt(spectrum, waveLength, true);
                    return (temp == double.NaN || double.IsInfinity(temp) || temp < 0) ? "--" : temp.ToString("F2");
                }
            }

            return "--";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
