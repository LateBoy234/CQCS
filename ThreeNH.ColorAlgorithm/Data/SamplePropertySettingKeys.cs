using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorAlgorithm.Data
{
    /// <summary>
    /// 样品属性设置的键
    /// </summary>
    public static class SamplePropertySettingKeys
    {
        /// <summary>
        /// 颜色配置，其值为IColorSetting
        /// </summary>
        public const string ColorSetting = "ColorSetting";

        /// <summary>
        /// 密度配置，其值类型为IDensitySetting
        /// </summary>
        public const string DensitySetting = "DensitySetting";

        /// <summary>
        /// 颜色通道设置，其值类型为string
        /// </summary>
        public const string Channel = "Channel";

        /// <summary>
        /// 光源设置，其值类型为StandardIlluminant
        /// </summary>
        public const string Illuminant = "Illuminant";

        /// <summary>
        /// 观察者角度设置，其值类型为StandardObserver
        /// </summary>
        public const string Observer = "Observer";

        /// <summary>
        /// 参考光源设置，其值类型为StandardIlluminant
        /// </summary>
        public const string RefIlluminant = "RefIlluminant";

        /// <summary>
        /// 参考观察者角度设置，其值类型为StandardObserver
        /// </summary>
        public const string RefObserver = "RefObserver";

        /// <summary>
        /// 密度状态，其值类型为DensityStatus
        /// </summary>
        public const string DensityStatus = "DensityStatus";

        /// <summary>
        /// CIEDE 2000 L的参数因子
        /// </summary>
        public const string CIEDE00FactorL = "KL00";

        /// <summary>
        /// CIEDE 2000 C的参数因子
        /// </summary>
        public const string CIEDE00FactorC = "KC00";

        /// <summary>
        /// CIEDE 2000 H的参数因子
        /// </summary>
        public const string CIEDE00FactorH = "KH00";

        /// <summary>
        /// CIEDE 1994 L的参数因子
        /// </summary>
        public const string CIEDE94FactorL = "KL94";

        /// <summary>
        /// CIEDE 1994 C的参数因子
        /// </summary>
        public const string CIEDE94FactorC = "KC94";

        /// <summary>
        /// CIEDE 1994 H的参数因子
        /// </summary>
        public const string CIEDE94FactorH = "KH94";

        /// <summary>
        /// CMCDE L的参数因子
        /// </summary>
        public const string CMCDEFactorL = "KLcmc";

        /// <summary>
        /// CMCDE C的参数因子
        /// </summary>
        public const string CMCDEFactorC = "KCcmc";

        /// <summary>
        /// CIEDE2000的参数因子，类型为ICIEDE2000Factors
        /// </summary>
        public const string CIEDE00Factors = "CIEDE00Factors";

        /// <summary>
        /// CIEDE 1994的参数因子，类型为ICIEDE1994Factors
        /// </summary>
        public const string CIEDE94Factors = "CIEDE94Factors";

        /// <summary>
        /// CMCDE 的参数因子，类型为ICMCDEFactors
        /// </summary>
        public const string CMCDEFactors = "CMCDEFactors";

        /// <summary>
        /// 555 shade number 的L*的容差
        /// </summary>
        public const string ShadeToleranceL = "ShadeTolerance_L";

        /// <summary>
        /// 555 shade number 的a*的容差
        /// </summary>
        public const string ShadeToleranceA = "ShadeTolerance_A";

        /// <summary>
        /// 555 shade number 的b*的容差
        /// </summary>
        public const string ShadeToleranceB = "ShadeTolerance_B";

        /// <summary>
        /// RGB的色空间系统，该参数为RGB的因子
        /// </summary>
        public const string RgbColorSystem = "RgbColorSystem";

        /// <summary>
        /// RGB的色空间系统的Gamma校正值
        /// </summary>
        public const string RgbGamma = "RgbGamma";
    }
}
