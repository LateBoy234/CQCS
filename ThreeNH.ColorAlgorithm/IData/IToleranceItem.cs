using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm.IData
{
    public interface IToleranceItem
    {
        /// <summary>
        /// 下限，如果为NaN，表明不使用下限做判断
        /// </summary>
        double Lower { get; set; }

        /// <summary>
        /// 上限，如果为NaN表明不使用上限
        /// </summary>
        double Upper { get; set; }
    }
}
