using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm.IData
{
    /// <summary>
    /// 试样接口
    /// </summary>
    public interface ISample : ICloneable
    {
        /// <summary>
        /// ID值
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// 样品名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        DateTime DateTime { get; set; }

        /// <summary>
        /// 料号
        /// </summary>
        string Material { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        string Remark { get; set; }

        /// <summary>
        /// 颜色配方
        /// </summary>
        string ColorFormula { get; set; }

        /// <summary>
        /// 仪器信息
        /// </summary>
        IInstrumentInformation InstrumentInformation { get; set; }

        /// <summary>
        /// 支持的颜色通道名
        /// </summary>
        string[] Channels { get; }

        /// <summary>
        /// 密度通道
        /// </summary>
        string[] DensityChannels { get; }

        /// <summary>
        /// 存储一些数据
        /// </summary>
        object Tag { get; set; }

        /// <summary>
        /// 获取颜色值
        /// </summary>
        /// <param name="channel">通道名</param>
        /// <returns>给定通道的颜色值；如果不存在则返回null</returns>
        IColorData GetColorData(string channel);

        /// <summary>
        /// 获取密度值
        /// </summary>
        /// <param name="channel">密度通道名</param>
        /// <returns>返回相应的密度值</returns>
        IDensityData GetDensityData(string channel);

        /// <summary>
        /// 设置指定通道的颜色值
        /// </summary>
        /// <param name="channel">通道名</param>
        /// <param name="colorData">要设置的值</param>
        void SetColorData(string channel, IColorData colorData);

        /// <summary>
        /// 设置密度值
        /// </summary>
        /// <param name="channel">密度通道名</param>
        /// <param name="densityData">要设置的密度值</param>
        void SetDensityData(string channel, IDensityData densityData);

        /// <summary>
        /// 20191206 lilin add
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool EqualObj(object obj1, object obj2);
    }

}