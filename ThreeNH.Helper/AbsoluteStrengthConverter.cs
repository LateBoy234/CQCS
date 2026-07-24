using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.Helper
{
    public class AbsoluteStrengthConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Spectrum spectrum && parameter is int waveLength)
            {
                var temp = Formula.GetColorValueSWLAt(spectrum, waveLength, false);
                return (temp == double.NaN || double.IsInfinity(temp) || temp < 0.0) ? "--" : temp.ToString("F2");
            }
            else if (value is Spectrum spectrum1 && parameter is string str)
            {
                if (str.Contains("+"))
                {
                    var length = int.Parse(str.Substring(0, str.IndexOf("+")));
                    var isTrans= str.Contains("True")?true:false;
                    var temp = Formula.GetColorValueSWLAt(spectrum1, length, isTrans);
                    return (temp == double.NaN || double.IsInfinity(temp) || temp < 0.0) ? "--" : temp.ToString("F2");
                }
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
