using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm.IData
{
    public interface IDensityData
    {
        /// <summary>
        /// 密度状态
        /// </summary>
        DensityStatus DensityStatus { get; }

        /// <summary>
        /// 密度色，不能为Density_Auto
        /// </summary>
        DensityColor DensityColor { get; }

        /// <summary>
        /// 密度值
        /// </summary>
        double Density { get; }

        /// <summary>
        /// 专色密度波长，仅在密度色为专色时有效
        /// </summary>
        int SpotWavelength { get; }

        /// <summary>
        /// 是否是绝对密度
        /// </summary>
        bool IsAbsolute { get; }

        /// <summary>
        /// 测量条件
        /// </summary>
        MeasurementSpec MeasurementSpec { get; }
    }
}
