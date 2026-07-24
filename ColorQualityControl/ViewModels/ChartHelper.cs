using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    class ChartHelper
    {
        
        public static int FindHighNumberPosition(double separator)
        {
            if (separator == 0 || double.IsNaN(separator) || double.IsInfinity(separator))
            {
                return 0;
            }

            int decimals = 0;
            if (Math.Abs(separator) < 1)
            {
                for (double t = separator; (int) t == 0; t = t * 10.0)
                {
                    decimals++;
                }
            }
            else
            {
                for (double t = separator * 0.1; (int) t != 0; t = t * 0.1)
                {
                    decimals--;
                }
            }

            return decimals;
        }

        /// <summary>
        /// 将数字保留指定位数有效数
        /// </summary>
        /// <param name="value"></param>
        /// <param name="n">保留的有效位数</param>
        /// <returns>结果</returns>
        public static double UniformNumber(double value, int n = 1)
        {
            return UniformNumber(value, n, Math.Round);
        }

        public static double UniformNumber(double value, int n, Func<double, double> func)
        {
            Trace.Assert(n > 0);

            int t = FindHighNumberPosition(value);
            double pow = Math.Pow(10, t + n - 1);
            return func(value * pow) / pow;
        }


        /// <summary>
        /// 自动调整坐标轴范围的算法
        /// </summary>
        /// <param name="axisMinRange">坐标轴的最小值</param>
        /// <param name="axisMaxRange">坐标轴的最大值</param>
        /// <param name="axisMinSeparator">网格线的最小间隔</param>
        /// <param name="minValue">可区区域的最小值</param>
        /// <param name="maxValue">可见区域的最大值</param>
        /// <returns>调整后的最小值、最大值及网格线分隔</returns>
        public static Tuple<double, double, double> GetAutoFitAxisRange(
            double axisMinRange,
            double axisMaxRange,
            double axisMinSeparator,
            double minValue, double maxValue)
        {
            double axisRange = axisMaxRange - axisMinRange;
            axisMinSeparator = double.IsNaN(axisMinSeparator) ? (axisRange / 100) : axisMinSeparator;
            
            if (double.IsNaN(minValue) || double.IsNaN(maxValue) || double.IsInfinity(minValue) ||
                double.IsInfinity(maxValue))
            {
                minValue = axisMinRange;
                maxValue = axisMaxRange;
            }
            else
            {
                minValue = Math.Max(axisMinRange, minValue);
                maxValue = Math.Min(axisMaxRange, maxValue);
            }

            var distance = maxValue - minValue;
            var separator = distance < axisMinSeparator * 4 ? axisMinSeparator : UniformNumber(distance / 4);

            var min = UniformNumber(minValue, 1, Math.Floor);
            var max = UniformNumber(maxValue, 1, Math.Ceiling);
            var center = UniformNumber((min + max) / 2, 1);
            var n = (int)Math.Log10((max - min) / separator);
            if (n > 0)
            {
                min = UniformNumber(Math.Max(axisMinRange, minValue), n+1, Math.Floor);
                max = UniformNumber(Math.Min(axisMaxRange, maxValue), n+1, Math.Ceiling);
                center = UniformNumber((min + max) / 2, n + 1);
            }

            if (max - min < separator * 4)
            {
                min = center - separator * 2;
                max = center + separator * 2;
            }

            min = Math.Round(min / separator) * separator;
            if (min > minValue)
            {
                min -= separator;
            }
            max = Math.Round(max / separator) * separator;
            if (max < maxValue)
            {
                max += separator;
            }

            return new Tuple<double, double, double>(
                Math.Max(min, axisMinRange), Math.Min(max, axisMaxRange), separator);
        }

    }
}
