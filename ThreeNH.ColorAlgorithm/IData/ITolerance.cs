using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorAlgorithm.IData
{
    /// <summary>
    /// 容差接口
    /// </summary>
    public interface ITolerance : ICloneable
    {
        /// <summary>
        /// 使用容差需要满足的条件
        /// </summary>
        IToleranceCondition Condition { get; set; }

        /// <summary>
        /// 获取或设置指定容差项的值
        /// </summary>
        /// <param name="name">容差项的名字</param>
        /// <returns>返回指定名称的容差项的值</returns>
        /// <exception cref="ArgumentException">指定名称不存在时</exception>
        IToleranceItem this[string name] { get; set; }

        /// <summary>
        /// 判断是否包含指定名的容差项
        /// </summary>
        bool Contains(string name);

        /// <summary>
        /// 判断是否包含指定名的参数因子
        /// </summary>
        bool ContainsFactor(string factorName);

        /// <summary>
        /// 获取指定名称的容差项
        /// </summary>
        /// <param name="name">容差项名称，通常约定使用SampleProperties中的</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>如果存在返回相应容差项，否则返回默认值</returns>
        IToleranceItem GetItem(string name, IToleranceItem defaultValue = null);

        /// <summary>
        /// 返回参数因子
        /// </summary>
        /// <param name="factorName">参数因子的名称</param>
        /// <returns>如果存在返回相应参数因子</returns>
        /// <exception cref="ArgumentException">不存在指定名称的参数因子时抛出</exception>
        double GetFactor(string factorName);

        /// <summary>
        /// 容差项
        /// </summary>
        IReadOnlyList<KeyValuePair<string, IToleranceItem>> Items { get; }

        /// <summary>
        /// 参数因子，键为参数因子的名称，值为因子
        /// </summary>
        IReadOnlyList<KeyValuePair<string, double>> Factors { get; }

        /// <summary>
        /// 判定给定的值是否合格
        /// </summary>
        /// <param name="values">给定的样品值，其键为值的名称，名称应与容差项名称命名一致</param>
        /// <returns>如果格返回true，如果否合格返回false，如果不包含用于判定合格与否的数据项则返回null</returns>
        bool? IsPass(IDictionary<string, double> values);
    }
}
