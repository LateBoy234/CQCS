using System;
using System.Collections.Generic;
using System.Linq;

namespace ThreeNH.Helper
{
    public static class StatisticsFormula
    {
        /// <summary>
        /// 样本标准偏差
        /// </summary>
        /// <param name="sample">样本数据</param>
        /// <param name="target">平均值，如果不为null则使用该值，否则从样本数据计算平均值</param>
        /// <returns>返回样本标准偏差</returns>
        /// <exception cref="ArgumentNullException">如果参数为空，抛出该异常</exception>
        public static double StdDev(this IEnumerable<double> sample, double? target = null)
        {
            if (sample == null)
                throw new ArgumentNullException();

            if (sample.Count() < 2)
                return 0.0;

            double mean = target == null ? sample.Average() : (double)target;
            double diff = 0.0;
            foreach(var value in sample)
            {
                diff += (value - mean) * (value - mean);
            }

            return Math.Sqrt(diff / (sample.Count() - 1));
        }
    }
}
