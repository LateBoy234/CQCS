using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 透镜切换方式
    /// </summary>
    public enum LensSwitchMode
    {
        Manual, // 手动
        Auto,   // 自动
    }

    /// <summary>
    /// 光谱测量方式
    /// </summary>
    public enum SpectrumMeasurementMode
    {
        Reflective,     // 反射
        Transmissive,   // 透射
    }

    public class Tolerance
    {
        public double dE { get; set; }
        public Tuple<double, double> dL { get; set; }
        public Tuple<double, double> da { get; set; }
        public Tuple<double, double> db { get; set; }

        public DataModel.Tolerance Uniform()
        {
            var t = new DataModel.Tolerance();
            t[ChromaticityField.dEab] = new ToleranceItem { Upper = dE, UseForJudgement = true };
            t[ChromaticityField.dL] = new ToleranceItem { Lower = dL.Item1, Upper = dL.Item2, UseForJudgement = true };
            t[ChromaticityField.da] = new ToleranceItem { Lower = da.Item1, Upper = da.Item2, UseForJudgement = true };
            t[ChromaticityField.db] = new ToleranceItem { Lower = db.Item1, Upper = db.Item2, UseForJudgement = true };
            return t;
        }

        public static Tolerance FromUniform(DataModel.Tolerance tolerance)
        {
            Tolerance t = new Tolerance();
            t.dE = tolerance[ChromaticityField.dEab].Upper.GetValueOrDefault(1.0);
            t.dL = new Tuple<double, double>(
                tolerance[ChromaticityField.dL].Lower.GetValueOrDefault(-1.0),
                tolerance[ChromaticityField.dL].Upper.GetValueOrDefault(1.0));
            t.da = new Tuple<double, double>(
                tolerance[ChromaticityField.da].Lower.GetValueOrDefault(-1.0),
                tolerance[ChromaticityField.da].Upper.GetValueOrDefault(1.0));
            t.db = new Tuple<double, double>(
                tolerance[ChromaticityField.db].Lower.GetValueOrDefault(-1.0),
                tolerance[ChromaticityField.db].Upper.GetValueOrDefault(1.0));
            return t;
        }
    }

    public interface ISystemSetting
    {
        /// <summary>
        /// 口径大小选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalMeasurementCalibers { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 透镜位置选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalLensPositions { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 透镜切换模式选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalLensSwitchModes { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 测量方式选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalSpecularComponents { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 测量模式选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalSpectrumMeasurementModes { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// UV光源选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalUVFlags { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 标准光源选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalStandardIlluminants { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 观察者选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalStandardObservers { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 颜色空间选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalColorSpaces { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 色差公式选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalColorDiffFormulas { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 颜色指数选项
        /// </summary>
        IEnumerable<ValueDescription> OptionalColorIndexs { get; }//Add By Liguanghai 20.03.27

        /// <summary>
        /// 光谱测量方式
        /// </summary>
        SpectrumMeasurementMode SpectrumMeasurementMode { get; set; }

        /// <summary>
        /// 镜面光测量方式
        /// </summary>
        SpecularComponent SpecularComponent { get; set; }

        /// <summary>
        /// 透镜切换方式
        /// </summary>
        LensSwitchMode LensSwitchMode { get; set; }

        /// <summary>
        /// 透镜位置
        /// </summary>
        LensPosition LensPosition { get; set; }

        /// <summary>
        /// 口径大小
        /// </summary>
        MeasurementCaliber Aperture { get; set; }

        /// <summary>
        /// 标准光源
        /// </summary>
        StandardIlluminant StandardIlluminant { get; set; }

        /// <summary>
        /// 标样视场角
        /// </summary>
        StandardObserver StandardObserver { get; set; }

        /// <summary>
        /// 使用的颜色空间
        /// </summary>
        ColorSpace ColorSpace { get; set; }

        /// <summary>
        /// 使用的色差公式
        /// </summary>
        ColorDiffFormula ColorDiffFormula { get; set; }

        /// <summary>
        /// 使用的颜色指数
        /// </summary>
        ColorIndex ColorIndex { get; set; }

        /// <summary>
        /// UV模式
        /// </summary>
        UVFlag UvMode { get; set; }

        /// <summary>
        /// 日期时间
        /// </summary>
        DateTime DateTime { get; set; }

        /// <summary>
        /// 系统默认容差
        /// </summary>
        Tolerance SystemDefaultTolerance { get; set; }

        /// <summary>
        /// 系统语言
        /// </summary>
        CultureInfo Language { get; set; }

        /// <summary>
        /// 获取设置
        /// </summary>
        /// <param name="propertyName">设置的属性名</param>
        /// <returns></returns>
        object GetSetting(string propertyName);

        /// <summary>
        /// 设置系统设置
        /// </summary>
        /// <param name="propertyName">设置的系统设置名</param>
        /// <param name="value">新值</param>
        void SetSetting(string propertyName, object value);

        /// <summary>
        /// LensSwitchMode属性是否可改
        /// </summary>
        bool IsLensSwitchModeModifiable { get; }

        /// <summary>
        /// 透镜位置是否可改
        /// </summary>
        bool IsLensPositionModifiable { get; }

        /// <summary>
        /// 口径大小是否可改
        /// </summary>
        bool IsMeasurementCaliberModifiable { get; }

        /// <summary>
        /// UvMode属性是否可修改
        /// </summary>
        bool IsUvModifiable { get; }

        /// <summary>
        /// SpecularComponent 属性是否可修改
        /// </summary>
        bool IsSpecularComponentModifiable { get; }

        /// <summary>
        /// 光谱测量方式是否可更改
        /// </summary>
        bool IsSpectrumMeasurementModeModifiable { get; }

        /// <summary>
        /// 重新读取所有系统配置
        /// </summary>
        void RefreshCaches();

        /// <summary>
        /// 恢复出厂设置
        /// </summary>
        void RestoreFactorySettings();

        /// <summary>
        /// 设置仪器显示语言
        /// </summary>
        void SetInstrumentLanguage(InstrumentLanguage lang);

        /// <summary>
        /// 设置仪器日期时间（部分机型 DateTime setter 未完整实现时走此方法）
        /// </summary>
        void SetDateTime(DateTime datetime);
    }
}
