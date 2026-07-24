using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Adapter
{
    /// <summary>
    /// 颜色指数接口
    /// </summary>
    public interface IColorIndex
    {
        /// <summary>
        /// 指数名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 指数值
        /// </summary>
        string Value { get; }
    }

    /// <summary>
    /// 可配置
    /// </summary>
    public interface IConfigurable
    {

        /// <summary>
        /// 显示配置窗口
        /// </summary>
        /// <param name="owner">父窗口</param>
        void ShowConfigDialog(Window owner = null);

        /// <summary>
        /// 将配置生成一个XML节点
        /// </summary>
        /// <param name="doc">关联的XmlDocument对鲁人</param>
        /// <param name="nodeName">生成的节点名</param>
        /// <returns>生成的节点</returns>
        XmlElement ToXmlElement(XmlDocument doc, string nodeName);

        /// <summary>
        /// 从XML节点加载配置项
        /// </summary>
        /// <param name="node">存储配置信息的节点</param>
        /// <returns>加载成功返回true，否则返回false</returns>
        bool LoadXmlElement(XmlElement node);
    }

    /// <summary>
    /// 样品颜色指数
    /// </summary>
    public interface ISpecimenColorIndex : IColorIndex
    {
        /// <summary>
        /// 更新颜色指数
        /// </summary>
        /// <param name="specimen">要求出其颜色指数的样品</param>
        void Update(SpecimenAdapter specimen);
    }

    /// <summary>
    /// 试样颜色指数
    /// </summary>
    public interface ITestColorIndex : IColorIndex
    {
        /// <summary>
        /// 更新颜色指数
        /// </summary>
        /// <param name="standard">计算颜色指数需要的标样</param>
        /// <param name="test">计算颜色指数需要的试样</param>
        void Update(StandardSpecimenAdapter standard, TestSpecimenAdapter test);
    }
}
