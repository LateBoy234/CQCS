using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThreeNH.ColorQualityControl.Converters
{
    class JudgementToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JudgementLevel leavel)
            {
                if (leavel == JudgementLevel.Pass)
                    return new BitmapImage( new Uri($@"..\Images\success.png", UriKind.Relative));
                else
                    return new  BitmapImage(new Uri($@"..\Images\fail.png", UriKind.Relative));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
