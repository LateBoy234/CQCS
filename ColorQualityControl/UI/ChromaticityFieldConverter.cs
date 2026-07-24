using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.UI
{
    class ChromaticityFieldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is ChromaticityField)
            {
                var field = (ChromaticityField)value;
                if (targetType == typeof(string))
                    return field.Name();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
