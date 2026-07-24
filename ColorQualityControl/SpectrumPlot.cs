using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl
{
    class SpectrumPlot
    {
        /// <summary>
        /// 最小波长
        /// </summary>
        public int Minimum { get; set; }

        /// <summary>
        /// 最大波长
        /// </summary>
        public int Maximum { get; set; }


        /// <summary>
        /// 主要刻度线间隔
        /// </summary>
        public int MajorTickInterval { get; set; }

        /// <summary>
        /// 次要刻度线间隔
        /// </summary>
        public int MinorTickInterval { get; set; }
    }
}
