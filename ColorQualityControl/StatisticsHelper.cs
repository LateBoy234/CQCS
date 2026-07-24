using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.DataModel;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.Helper;

namespace ThreeNH.ColorQualityControl
{
    static class StatisticsHelper
    {
        /// <summary>
        /// 获取某个色品坐标的统计结果
        /// </summary>
        /// <param name="adapters">要统计的样品</param>
        /// <param name="field">要统计的色品坐标</param>
        /// <returns>统计结果</returns>
        public static Statistics GetChromaticityStatistics(SpecimenAdapter[] adapters, ChromaticityField field)
        {
            if (!field.IsChromaticity())
                throw new ArgumentException("field must be chromaticity coordinate");

            IEnumerable<double> values = from x in adapters
                         where (!double.IsNaN((double)x.GetPropertyValue(field)))
                         select (double)x.GetPropertyValue(field);

            Statistics statistics = new Statistics();
            statistics.Total = adapters.Length;
            if (values.Count() > 0)
            {
                statistics.Valid = values.Count();
                statistics.Min = values.Min();
                statistics.Max = values.Max();
                statistics.Mean = values.Average();
                statistics.StdDev = values.StdDev(statistics.Mean);
            }

            return statistics;
        }

        /// <summary>
        /// 获取某个色差的统计结果
        /// </summary>
        /// <param name="adapters">要统计的样品</param>
        /// <param name="field">要统计的色品坐标</param>
        /// <returns>统计结果</returns>
        public static Statistics GetColorDiffStatistics(TestSpecimenAdapter[] adapters, ChromaticityField field)
        {
            if (!field.IsColorDiffField())
                throw new ArgumentException("field must be color difference");


            Statistics statistics = new Statistics();

            List<double> values = new List<double>();
            foreach(var x in adapters)
            {
                var value = (double)x.GetPropertyValue(field);
                if (double.IsNaN(value))
                    continue;

                values.Add(value);
                if (x.Judgement[field] == JudgementLevel.Pass)
                    statistics.Pass += 1;
                else if (x.Judgement[field] == JudgementLevel.Warning)
                    statistics.Warning += 1;
                else if (x.Judgement[field] == JudgementLevel.NotGood)
                    statistics.NotGood += 1;
            }

            statistics.Total = adapters.Length;
            if (values.Count() > 0)
            {
                statistics.Min = values.Min();
                statistics.Max = values.Max();
                statistics.Mean = values.Average();
                statistics.StdDev = values.StdDev(statistics.Mean);
                statistics.Valid = values.Count();
            }
            return statistics;
        }

        /// <summary>
        /// 获取总结论的统计结果
        /// </summary>
        /// <param name="adapters">要统计的样品</param>
        /// <returns>统计结果</returns>
        public static Statistics GetJudgementStatistics(TestSpecimenAdapter[] adapters)
        {
            Statistics statistics = new Statistics();
            statistics.Total = adapters.Length;

            foreach (var x in adapters)
            {
                if (x.Judgement == null || x.Judgement.TotalJudgement == JudgementLevel.Invalid)
                    continue;

                statistics.Valid += 1;
                if (x.Judgement.TotalJudgement == JudgementLevel.Pass)
                    statistics.Pass += 1;
                else if (x.Judgement.TotalJudgement == JudgementLevel.Warning)
                    statistics.Warning += 1;
                else if (x.Judgement.TotalJudgement == JudgementLevel.NotGood)
                    statistics.NotGood += 1;
            }

            return statistics;
        }
    }
}
