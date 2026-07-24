using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm.IData
{
    /// <summary>
    /// 仪器信息
    /// </summary>
    public interface IInstrumentInformation
    {
        /// <summary>
        /// 仪器型号
        /// </summary>
        string InstrumentModel { get; }

        /// <summary>
        /// 仪器光学结构
        /// </summary>
        string OpticalStructure { get; }

        /// <summary>
        /// 仪器序列号
        /// </summary>
        string SerialNumber { get; }

        /// <summary>
        /// 支持的颜色通道名称
        /// </summary>
        string[] Channels { get; }

        bool Equals(object obj);
    }
}
