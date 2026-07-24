using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ThreeNH.ColorQualityControl.Converters
{
    /// <summary>
    /// 简单的四则运算，只接收数值类型，参数必须为+、-、*、/、%之一
    /// 且只接收两个参数
    /// 返回结果可以返回double, int, string
    /// </summary>
    class SimpleArithmeticConverter : IMultiValueConverter
    {
        private static readonly Dictionary<Type, string> FuncDict = new Dictionary<Type, string>
        {
            {typeof(byte), nameof(System.Convert.ToByte)},
            {typeof(sbyte), nameof(System.Convert.ToSByte)},
            {typeof(short), nameof(System.Convert.ToInt16)},
            {typeof(ushort), nameof(System.Convert.ToUInt16)},
            {typeof(int), nameof(System.Convert.ToInt32)},
            {typeof(uint), nameof(System.Convert.ToUInt32)},
            {typeof(long), nameof(System.Convert.ToInt64)},
            {typeof(ulong), nameof(System.Convert.ToUInt64)},
            {typeof(float), nameof(System.Convert.ToSingle)},
            {typeof(double), nameof(System.Convert.ToDouble)},
            {typeof(string), nameof(System.Convert.ToString)}
        };

        static Dictionary<string, Func<double, double, double>> DoubleOperations = new Dictionary<string, Func<double, double, double>>
        {
            {"+", (a, b) => a + b},
            {"-", (a, b) => a - b},
            {"*", (a, b) => a * b},
            {"/", (a, b) => a / b},
            {"%", (a, b) => a % b},
        };

        static Dictionary<string, Func<long, long, long>> IntOperations = new Dictionary<string, Func<long, long, long>>
        {
            {"+", (a, b) => a + b},
            {"-", (a, b) => a - b},
            {"*", (a, b) => a * b},
            {"/", (a, b) => a / b},
            {"%", (a, b) => a % b},
        };

        static object ConvertTo(object o, Type targetType)
        {
            if (FuncDict.ContainsKey(targetType))
            {
                var method = typeof(System.Convert).GetMethod(FuncDict[targetType], new[] {o.GetType()});
                if (method != null)
                {
                    return method.Invoke(null, new []{o});
                }
            }

            return null;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(_ => _ == null))
                return Binding.DoNothing;


            var opt = (string)parameter;

            if (!DoubleOperations.ContainsKey(opt))
            {
                throw new NotImplementedException();
            }


            if (values.Any(_ => _ is double) || targetType == typeof(double))
            {
                // 全转换为double
                var numbers = values.Select(_ => System.Convert.ToDouble(_)).ToArray();
                double result = 0.0;
                if (opt == "+") result = numbers.Sum();
                else
                {
                    if (numbers.Length > 2)
                        throw new NotImplementedException();
                    result = DoubleOperations[opt](numbers[0], numbers[1]);


                }
                return ConvertTo(result, targetType);
            }
            else
            {
                var numbers = values.Select(_ => System.Convert.ToInt64(_)).ToArray();
                long result = 0;
                if (opt == "+")
                    result = numbers.Sum();
                else
                {
                    if (numbers.Length > 2)
                        throw new NotImplementedException();
                    result = IntOperations[opt](numbers[0], numbers[1]);


                }
                return ConvertTo(result, targetType);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
