using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace ThreeNH.Helper
{
    class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 如果为真，先反转要转换的值
        /// </summary>
        public bool Invert { get; set; }

        /// <summary>
        /// 如果为真，当为当要转换的值为false时返回Hidden，否则返回Collapsed
        /// </summary>
        public bool Hidden { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hidden;
            if (value is Boolean)
            {
                hidden = (bool) value;
            }
            else if (value is IConvertible)
            {
                hidden = ((IConvertible) value).ToBoolean(culture);
            }
            else
            {
                return null;
            }

            if (Invert)
            {
                hidden = !hidden;
            }
            return hidden ? Visibility.Visible : (Hidden ? Visibility.Hidden : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                var visiblity = (Visibility) value;
                if (!Invert)
                {
                    return visiblity == Visibility.Visible;
                }

                return (visiblity != Visibility.Visible);
            }

            return null;
        }
    }
}
