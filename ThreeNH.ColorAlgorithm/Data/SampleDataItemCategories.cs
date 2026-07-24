using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm.Data
{
    /// <summary>
    /// 数据项顶级类
    /// </summary>
    public enum SamplePropertyTopCategoryType
    {
        Common = 0x0001,           // 通用
        ColorDependent = 0x0002,   // 颜色相关
        DensityDependent = 0x0003, // 密度相关
        SpectraDependent = 0x0004, // 光谱相关
        ColorDiffFormula = 0x0005, // 色差公式
        ColorIndex = 0x0006,       // 颜色指数
        CustomFunction = 0x0007,     // 用户自定义功能
    }

    /// <summary>
    /// 数据项所有类别
    /// 如果低位为顶级类别
    /// </summary>
    public enum SamplePropertyCategoryType
    {
        Common = SamplePropertyTopCategoryType.Common,  // 通用
        Color = SamplePropertyTopCategoryType.ColorDependent | 0x0100, // 颜色值
        ColorDiff = SamplePropertyTopCategoryType.ColorDependent | 0x0200, // 色差值
        Density = SamplePropertyTopCategoryType.DensityDependent | 0x0100, // 密度值
        DensityDiff = SamplePropertyTopCategoryType.DensityDependent | 0x0200, // 密度差
        SpectralData = SamplePropertyTopCategoryType.SpectraDependent | 0x0100, // 光谱数据
        SpectralDiff = SamplePropertyTopCategoryType.SpectraDependent | 0x0200, // 光谱数据差
        SpectralSpotDensity = SamplePropertyTopCategoryType.SpectraDependent | 0x0300, // 指定光谱专色密度
        SpectralSpotDensityDiff = SamplePropertyTopCategoryType.SpectraDependent | 0x0400, // 指定光谱专色密度差
        SpectralStrength = SamplePropertyTopCategoryType.SpectraDependent | 0x0500, // 指定波长的力份
        SpectralRelativeStrength = SamplePropertyTopCategoryType.SpectraDependent | 0x0600, // 指定波长的力份
        ColorDiffFormula = SamplePropertyTopCategoryType.ColorDiffFormula, // 色差公式
        ColorIndex = SamplePropertyTopCategoryType.ColorIndex,  // 颜色指数
        CustomFunction = SamplePropertyTopCategoryType.CustomFunction,  // 用户自定义函数
    }

    /// <summary>
    /// 样品数据项类别
    /// 其中使用第一字节的一级类，
    /// 第二字节为子类
    /// </summary>
    public static class SampleDataItemCategoryExtensions
    {
        static SampleDataItemCategoryExtensions()
        {
            AllTopCategoryTypes = Enum.GetValues(typeof(SamplePropertyTopCategoryType)).OfType<SamplePropertyTopCategoryType>()
                .ToArray();

            AllCategoryTypes = Enum.GetValues(typeof(SamplePropertyCategoryType)).OfType<SamplePropertyCategoryType>().ToArray();
        }

        /// <summary>
        /// 所有顶级类别
        /// </summary>
        public static SamplePropertyTopCategoryType[] AllTopCategoryTypes { get; set; }

        /// <summary>
        /// 所有子类别
        /// </summary>
        public static SamplePropertyCategoryType[] AllCategoryTypes { get; set; }

        /// <summary>
        /// 获取指定类别的顶级类
        /// </summary>
        public static SamplePropertyTopCategoryType GetTopCategory(this SamplePropertyCategoryType categoryType)
        {
            return (SamplePropertyTopCategoryType)((int)categoryType & 0xFF);
        }

        /// <summary>
        /// 获取指定顶级类别的所有子类
        /// </summary>
        public static SamplePropertyCategoryType[] GetSubCategories(this SamplePropertyTopCategoryType topCategoryType)
        {
            return AllCategoryTypes.Where(x => GetTopCategory(x) == topCategoryType).ToArray();
        }
    }
}
