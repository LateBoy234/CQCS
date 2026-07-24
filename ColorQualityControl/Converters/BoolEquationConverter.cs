using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace ThreeNH.ColorQualityControl.Converters
{
    class BoolEquationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string s)
            {
                try
                {
                    if (value is int ival)
                    {
                        return ival == int.Parse(s);
                    }
                    else if (value is short hval)
                    {
                        return hval == short.Parse(s);
                    }
                    else if (value is long lval)
                    {
                        return lval == long.Parse(s);
                    }
                    else if (value is Enum)
                    {
                        return Equals(value, Enum.Parse(value.GetType(), s));
                    }
                    else if (value is double dval)
                    {
                        return Math.Abs(dval - double.Parse(s)) < 0.000001;
                    }
                    else if (value is float fval)
                    {
                        return Math.Abs(fval - float.Parse(s)) < 0.000001;
                    }
                    else if (value is bool bval)
                    {
                        return bval == bool.Parse(s);
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return Equals(value, parameter);
            }
            

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEq)
            {
                if (!(parameter is string))
                    return parameter;

                var s = (string)parameter;
                try
                {
                    if (targetType.IsEnum)
                    {
                        return Enum.Parse(targetType, s);
                    }
                    if (targetType == typeof(int))
                    {
                        return int.Parse(s);
                    }
                    if (targetType == typeof(short))
                    {
                        return short.Parse(s);
                    }
                    if (targetType == typeof(byte))
                    {
                        return byte.Parse(s);
                    }
                    if (targetType == typeof(float))
                    {
                        return float.Parse(s);
                    }
                    if (targetType == typeof(double))
                    {
                        return double.Parse(s);
                    }
                    if (targetType == typeof(long))
                    {
                        return long.Parse(s);
                    }
                }
                catch
                {
                    return Binding.DoNothing;
                }
            }
            return Binding.DoNothing;
        }
    }
}
