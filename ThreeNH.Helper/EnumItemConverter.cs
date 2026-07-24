using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace ThreeNH.Helper
{
    [ValueConversion(typeof(Enum), typeof(ValueDescription))]
    public class EnumItemConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum)
                return ((Enum)value).Description();
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                string description = value as string;
                foreach (Enum item in targetType.GetEnumValues())
                {
                    if (item.Description() == description)
                        return item;
                }
            }
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
