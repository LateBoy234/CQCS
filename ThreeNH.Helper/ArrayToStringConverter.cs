using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ThreeNH.Helper
{
    public class ArrayToStringConverter<T> : IValueConverter
    {
        public int? Length { get; set; }

        public Converter<T, string> ValToStr;

        public Converter<string, T> StrToVal;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var arr = value as T[];
            if (arr != null)
            {
                var list = new List<string>(Array.ConvertAll(arr, ValToStr));

                string str = "";
                for (int i = 0; i < arr.Length; i += 10)
                {
                    int n = Math.Min(arr.Length - i, 10);
                    str += string.Join("\t", list.GetRange(i, n));
                    str += "\r\n";
                }

                return str;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (s != null)
            {
                var separator = new[] {',', ' ', '\t', '\n', '\r'};
                s = s.Trim(separator);
                var list = s.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                int length = Length.GetValueOrDefault(list.Length);
                T[] arr = new T[length];
                for (int i = 0; i < list.Length && i < length; i++)
                {
                    arr[i] = StrToVal(list[i]);
                }

                return arr;
            }

            if (Length != null && targetType == typeof(T[]))
            {
                return new T[(int) Length];
            }

            return null;
        }
    }

    public class DoubleArrayToStringConverter : ArrayToStringConverter<double>
    {
        public string Format { get; set; }
        public DoubleArrayToStringConverter()
        {
            ValToStr = x => x.ToString(Format);
            StrToVal = double.Parse;
        }
    }

    public class FloatArrayToStringConverter : ArrayToStringConverter<float>
    {
        public string Format { get; set; }
        public FloatArrayToStringConverter()
        {
            ValToStr = x => x.ToString(Format);
            StrToVal = float.Parse;
        }
    }

}