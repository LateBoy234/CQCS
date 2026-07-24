using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm.IData
{

    public interface IToleranceCondition : IEqualityComparer<IToleranceCondition>
    {
        /// <summary>
        /// 测量条件
        /// </summary>
        string MeasurementCondition { get; set; }

        /// <summary>
        /// 判断该条件是否包含另一个条件
        /// </summary>
        /// <param name="condition">要判断其是否被包含的条件</param>
        /// <returns>如果是返回真</returns>
        bool IsContains(IToleranceCondition condition);
    }
}
