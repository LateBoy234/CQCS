using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm
{
    public static class MyMath
    {
        // 一些有用的小工具

        //public static double Sum(this IEnumerable<double> container)
        //{
        //    double tmp = 0;
        //    foreach(double item in container)
        //    {
        //        tmp += item;
        //    }
        //    return tmp;
        //}

        

        // 将角度转换为弧度
        public static double ToRadians(this double degrees)
        {
            return degrees / 180.0 * Math.PI;
        }

        // 将弧度转换为角度
        public static double ToDegrees(this double radians)
        {
            return radians / Math.PI * 180.0;
        }

        //public static double Max(double value1, params double[] values)
        //{
        //    double max = value1;
        //    if (values != null && values.Length > 0)
        //    {
        //        foreach (double v in values)
        //        {
        //            if (max < v)
        //                max = v;
        //        }
        //    }
        //    return max;
        //}

        //public static double Min(double value1, params double[] values)
        //{
        //    double min = value1;
        //    if (values != null && values.Length > 0)
        //    {
        //        foreach (double v in values)
        //        {
        //            if (min > v)
        //                min = v;
        //        }
        //    }
        //    return min;
        //}

        // 求角a角b的平均值。角a、角b均在0到360度之间
        public static double AverageAngleByDegree(double a, double b)
        {
            double h_m = (a + b) / 2;
            if (Math.Abs(a - b) > 180)
            {
                if (a + b < 360)
                    h_m += 180;
                else  // (a + b >= 360)
                    h_m -= 180;
            }
            return h_m;
        }
        // 求角a角b的平均值。角a、角b均在0到2PI之间
        public static double AverageAngleByRadian(double a, double b)
        {
            double h_m = (a + b) / 2;
            if (Math.Abs(a - b) > Math.PI)
            {
                if (a + b < Math.PI * 2.0)
                    h_m += Math.PI;
                else  // (a + b >= Math.PI * 2.0)
                    h_m -= Math.PI;
            }
            return h_m;
        }
    }
}
