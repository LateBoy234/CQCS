using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ThreeNH.Helper
{
    public class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            if (value is int ival)
            {
                return ival != 0;
            }
            if (value is short hval)
            {
                return hval != 0;
            }
            if (value is double dval)
            {
                return dval != 0.0;
            }
            if (value is string s)
            {
                return !string.IsNullOrEmpty(s);
            }
            if (value.GetType().IsValueType)
            {
                return !Equals(value, 0);
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
