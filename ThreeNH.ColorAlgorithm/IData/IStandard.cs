using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm.IData
{
    /// <summary>
    /// 标样接口
    /// </summary>
    public interface IStandard : ISample
    {
        /// <summary>
        /// 容差
        /// </summary>
        ITolerance[] Tolerances { get; set; }
    }
}
