using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm;

namespace ThreeNH.ColorAlgorithm.IData
{
    /// <summary>
    /// 表示颜色数据，可以是光谱或XYZ值
    /// </summary>
    public interface IColorData : ICloneable
    {
        /// <summary>
        /// XYZ和Lab数据使用的标准光源
        /// </summary>
        StandardIlluminant Illuminant { get; }

        /// <summary>
        /// Xyz和Lab数据使用的观察者角度
        /// </summary>
        StandardObserver Observer { get; }

        /// <summary>
        /// XYZ数据
        /// </summary>
        CIEXYZ Xyz { get; }

        /// <summary>
        /// CIE LAB 数据
        /// </summary>
        CIELAB Lab { get; }

        /// <summary>
        /// 光谱数据
        /// </summary>
        ISpectralData SpectralData { get; }

        /// <summary>
        /// 仿真色
        /// </summary>
        sRGB PseudoColor { get; }



        /// <summary>
        /// 透射标志，如果为真表示是透射数据，否则表示反射
        /// </summary>
        bool TransmissionFlag { get; }
        MeasurementSpec MeasurementSpec { get; set; }
    }
}
