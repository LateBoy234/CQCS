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
    class ObserverConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            StandardObserver observer;
            if (value is StandardObserver standardObserver)
            {
                observer = standardObserver;
            }
            else if (value is int)
            {
                observer = (StandardObserver) (int) value;
            }
            else if (value is string s)
            {
                if (!Enum.TryParse(s, out observer))
                {
                    switch (s)
                    {
                        case "2":
                        case "2\u00B0":
                            observer = StandardObserver.CIE1931;
                            break;
                        case "10":
                        case "10\u00B0":
                            observer = StandardObserver.CIE1964;
                            break;
                        default:
                            return Binding.DoNothing;
                    }
                }
            }
            else
            {
                return Binding.DoNothing;
            }

            return observer.Description();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                StandardObserver observer;
                if (!Enum.TryParse(s, out observer))
                {
                    switch (s)
                    {
                        case "2":
                        case "2\u00B0":
                            observer = StandardObserver.CIE1931;
                            break;
                        case "10":
                        case "10\u00B0":
                            observer = StandardObserver.CIE1964;
                            break;
                        default:
                            return Binding.DoNothing;
                    }
                }

                return observer;
            }

            return Binding.DoNothing;
        }
    }
}