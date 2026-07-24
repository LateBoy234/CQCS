using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.Converters
{
    class IlluminantConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int ival)
            {
                return ((StandardIlluminant) ival).Description();
            }

            if (value is StandardIlluminant illuminant)
            {
                return illuminant.Description();
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                StandardIlluminant illuminant;
                if (!Enum.TryParse(s, out illuminant))
                {
                    var descriptions = EnumHelper.GetAllValuesAndDescriptions(typeof(StandardIlluminant));
                    try
                    {
                        var result = descriptions.First(_ => _.Description == s);
                        illuminant = (StandardIlluminant)result.Value;
                    }
                    catch
                    {
                        return Binding.DoNothing;
                    }
                }

                if (targetType == typeof(StandardIlluminant))
                {
                    return illuminant;
                }

                if (targetType == typeof(int))
                {
                    return (int) illuminant;
                }
            }

            return Binding.DoNothing;
        }
    }
}
