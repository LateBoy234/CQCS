using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    /*
     * 样品属性设置说明
     * 所有样品属性性都必须包含两个基本属性，即ColorSetting和DensitySetting，
     * 其类型分别为IColorSetting和IDensitySetting的实现
     * 另外如果属性自身设置了Channel、Illuminant、Observer，DensityStatus
     * 会优先使用属性性的这些设置，找不到的情况下才会查找ColorSetting或DensitySetting
     *
     * 色差公式 CIE DE2000 及其分量
     *      KL00 -- L的参数因子
     *      KC00 -- C的参数因子
     *      KH00 -- H的参数因子
     * 色差公式 CIE DE1994 及其分量
     *      KL94 -- L的参数因子
     *      KC94 -- C的参数因子
     *      KH94 -- H的参数因子
     * 色差公式 CMC 及其分量
     *      KLcmc -- L的参数因子
     *      KCcmc -- C的参数因子
     * AstmE313 的白度和Tint
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * ISO 的白度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * Tappl 的白度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * Taube 的白度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * Hunter 1942 的白度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * Hunter 1960 的白度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * ASTM D1925 的黄度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * ASTM E313的黄度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * 同色异谱指数
     *      RefIlluminant -- 参考光源
     *      RefObserver -- 参考观察者
     * 沾色牢度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     * 变色牢度
     *      Illuminant -- 光源
     *      Observer -- 观察者
     */

    /// <summary>
    /// 样品数据项求值器
    /// </summary>
    /// <param name="settings">样品数据项的属性设置</param>
    /// <param name="sample">样品</param>
    /// <param name="target">样品关联的标样（视乎属性不同，可能不会用到该参数）</param>
    /// <param name="paperWhite">样品基准白（对于需要计算密度的传入该值会使用相对密度，如果为null则使用绝对密度</param>
    /// <returns>所求数据项</returns>
    public delegate object EvaluateSampleProperty(
        IDictionary<string, object> settings,
        ISample sample,
        ISample target,
        ISample paperWhite);

    /// <summary>
    /// 样品属性信息
    /// </summary>
    public class SamplePropertyInfo
    {
        /// <summary>
        /// 样品属性名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数据项的类型
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// 样品属性设置
        /// </summary>
        public IDictionary<string, object> PropertySettings { get; set; }

        /// <summary>
        /// 求值器，第一个参数为属性值，第二个为
        /// </summary>
        public EvaluateSampleProperty Evaluate { get; set; }
    }

    /// <summary>
    /// 样品属性设置的信息
    /// </summary>
    public class SamplePropertySettingInfo
    {
        /// <summary>
        /// 设置项的名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 设置项的值类型
        /// </summary>
        public Type ValueType { get; private set; }

        /// <summary>
        /// 默认设置值
        /// </summary>
        public object DefaultValue { get; private set; }

        public SamplePropertySettingInfo(string name, Type valueType, object defaultValue)
        {
            this.Name = name;
            this.ValueType = valueType;
            this.DefaultValue = defaultValue;
        }
    }

    public class SamplePropertyCategory
    {
        private bool _isEnable = true;
        private bool _isSelected;

        public string Name { get; }


        /// <summary>
        /// 包含的数据项
        /// </summary>
        public SamplePropertyInfo[] Properties { get; }

        /// <summary>
        /// 子类
        /// </summary>
        public SamplePropertyCategory[] SubCategories { get; }

        /// <summary>
        /// 是否包含子类
        /// </summary>
        public bool HasSubCategories => SubCategories != null;

        /// <summary>
        /// 使用指定类名和子项初始化分类
        /// </summary>
        /// <param name="name">类名</param>
        /// <param name="items">类包含的子项的名字</param>
        internal SamplePropertyCategory(string name, IEnumerable<SamplePropertyInfo> properties = null)
        {
            this.Name = name;
            if (properties != null)
            {
                this.Properties = properties.ToArray();
            }
        }

        /// <summary>
        /// 使用指定类名和子类初始化分类
        /// </summary>
        /// <param name="name">类名</param>
        /// <param name="subCategories">子类</param>
        internal SamplePropertyCategory(string name, params SamplePropertyCategory[] subCategories)
        {
            this.Name = name;
            this.SubCategories = subCategories;
        }
    }

    /// <summary>
    /// 样品属性
    /// </summary>
    public class SampleProperties
    {
        static SampleProperties()
        {
            var de94 = new[]
            {
                new SamplePropertySettingInfo(SamplePropertySettingKeys.CIEDE94FactorL, typeof(double), 1.0),
                new SamplePropertySettingInfo(SamplePropertySettingKeys.CIEDE94FactorC, typeof(double), 1.0),
                new SamplePropertySettingInfo(SamplePropertySettingKeys.CIEDE94FactorH, typeof(double), 1.0),
            };
            var de00 = new[]
            {
                new SamplePropertySettingInfo(SamplePropertySettingKeys.CIEDE00FactorL, typeof(double), 1.0),
                new SamplePropertySettingInfo(SamplePropertySettingKeys.CIEDE00FactorC, typeof(double), 1.0),
                new SamplePropertySettingInfo(SamplePropertySettingKeys.CIEDE00FactorH, typeof(double), 1.0),
            };
            var cmcde = new[]
            {
                new SamplePropertySettingInfo(SamplePropertySettingKeys.CMCDEFactorL, typeof(double), 2.0),
                new SamplePropertySettingInfo(SamplePropertySettingKeys.CMCDEFactorC, typeof(double), 1.0),
            };


            SamplePropertySettingInfo[] createPropertySettingInfos(StandardIlluminant illuminant,
                StandardObserver observer)
            {
                return new[]
                {
                    NewSettingInfo(SamplePropertySettingKeys.Illuminant, illuminant),
                    NewSettingInfo(SamplePropertySettingKeys.Observer, observer),
                };
            }

            // WI
            var d6510 = createPropertySettingInfos(StandardIlluminant.D65, StandardObserver.CIE1964);

            // MI
            var mi = new[]
            {
                NewSettingInfo(SamplePropertySettingKeys.RefIlluminant, StandardIlluminant.C),
                NewSettingInfo(SamplePropertySettingKeys.RefObserver, StandardObserver.CIE1931),
            };

            // 555 shade number tolerance
            var shade_tolerance = new[]
            {
                NewSettingInfo(SamplePropertySettingKeys.ShadeToleranceL, 1.0),
                NewSettingInfo(SamplePropertySettingKeys.ShadeToleranceA, 1.0),
                NewSettingInfo(SamplePropertySettingKeys.ShadeToleranceB, 1.0),
            };

            // RGB setting
            var rgb_setting = new[]
            {
                NewSettingInfo(SamplePropertySettingKeys.RgbColorSystem, (string)null),
                NewSettingInfo(SamplePropertySettingKeys.RgbGamma, 1.0)
            };


            PropertySettingInfos = new Dictionary<string, SamplePropertySettingInfo[]>
            {
                // CIE DE 1994
                {nameof(ColorDiffFormula_DE1994), de94},
                {nameof(ColorDiffFormula_DL1994), de94},
                {nameof(ColorDiffFormula_DC1994), de94},
                {nameof(ColorDiffFormula_DH1994), de94},
                // CIE DE 2000
                {nameof(ColorDiffFormula_DE2000), de00},
                {nameof(ColorDiffFormula_DL2000), de00},
                {nameof(ColorDiffFormula_DC2000), de00},
                {nameof(ColorDiffFormula_DH2000), de00},
                // CMC DE
                {nameof(ColorDiffFormula_CMCDE), cmcde},
                {nameof(ColorDiffFormula_DLcmc), cmcde},
                {nameof(ColorDiffFormula_DCcmc), cmcde},
                {nameof(ColorDiffFormula_DHcmc), cmcde},
                // 白度
                {nameof(ColorIndex_AstmE313_Tint), d6510},
                {nameof(ColorIndex_AstmE313_WI), d6510},
                {nameof(ColorIndex_CIEISO_Tint), d6510},
                {nameof(ColorIndex_CIEISO_WI), d6510},
                {nameof(ColorIndex_Tapple_WI), d6510},
                {nameof(ColorIndex_Taubel_WI), d6510},
                {nameof(ColorIndex_Hunter1942_WI), d6510},
                {nameof(ColorIndex_Hunter1960_WI), d6510},
                // 黄度
                {nameof(ColorIndex_AstmE313_YI), d6510},
                // 同色异谱
                {nameof(ColorIndex_MI), mi},
                // 色牢度
                {nameof(ColorIndex_StainingFastness_dEgs), d6510},
//                {nameof(ColorIndex_StainingFastness_SSR), d6510},
                {nameof(ColorIndex_ReportedSSR), d6510},
                {nameof(ColorIndex_ColorFastness_dEf), d6510},
//                {nameof(ColorIndex_ColorFastness_GSR), d6510},
                {nameof(ColorIndex_ReportedGSR), d6510},

                // 555 色调分类
                {nameof(ColorIndex_555Shade), shade_tolerance},
                {nameof(Color_RGB_R), rgb_setting},
                {nameof(Color_RGB_G), rgb_setting},
                {nameof(Color_RGB_B), rgb_setting},
                {nameof(Color_PseudoColor), rgb_setting},
            };
        }


        /// <summary>
        /// 样品所有需要设置项的属性的设置项的信息
        /// </summary>
        public static IReadOnlyDictionary<string, SamplePropertySettingInfo[]> PropertySettingInfos
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取属性信息
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <returns>返回值</returns>
        public static SamplePropertyInfo GetPropertyInfo(string propertyName)
        {
            var type = typeof(SampleProperties);

            if (!propertyName.StartsWith("Spectral", StringComparison.OrdinalIgnoreCase))
            {
                var method = type.GetMethod(propertyName, BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                {
                    return (SamplePropertyInfo)method.Invoke(type, new object[0]);//190924lilin 用当前的方法给委托赋值
                }
            }
            else
            {
                var match = Regex.Match(propertyName, @"^(Spectral\w+_At)_(\d+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var method = type.GetMethod(match.Groups[1].Value, BindingFlags.Static | BindingFlags.Public);
                    if (method != null)
                    {
                        return (SamplePropertyInfo)method.Invoke(type,
                            new object[] { Int32.Parse(match.Groups[2].Value) });
                    }
                }
            }

            throw new ArgumentException($"Unknown property '{propertyName}'", nameof(propertyName));
        }

        /// <summary>
        /// 获取指定属性名的设置信息
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <returns>设置信息</returns>
        public static IDictionary<string, object> GetPropertyDefaultSetting(string propertyName)
        {
            if (PropertySettingInfos.ContainsKey(propertyName))
            {
                return PropertySettingInfos[propertyName]
                    .ToDictionary(x => x.Name, x => x.DefaultValue);
            }

            return null;
        }

        /// <summary>
        /// 属性是否有设置项
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <returns>有返回真，无返回假</returns>
        public static bool HasPropertySettings(string propertyName)
        {
            return PropertySettingInfos.ContainsKey(propertyName);
        }

        /// <summary>
        /// 获取指定类别的所有样品数据项的名字
        /// </summary>
        /// <param name="categoryType"></param>
        /// <param name="wavelengths"></param>
        /// <returns></returns>
        public static IEnumerable<SamplePropertyInfo> GetCategoryItems(SamplePropertyCategoryType categoryType,
            IEnumerable<int> wavelengths = null)
        {
            var type = typeof(SampleProperties);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.Name.StartsWith(categoryType.ToString() + "_", StringComparison.OrdinalIgnoreCase));

            if (categoryType.GetTopCategory() != SamplePropertyTopCategoryType.SpectraDependent)
                return methods.Select(x => (SamplePropertyInfo)x.Invoke(type, new object[0]));

            if (wavelengths == null)
            {
                throw new ArgumentNullException(nameof(wavelengths));
            }

            var arr = wavelengths.ToArray();

            List<SamplePropertyInfo> list = new List<SamplePropertyInfo>();
            foreach (var method in methods)
            {
                list.AddRange(arr.Select(x => (SamplePropertyInfo)method.Invoke(type, new object[] { x })));
            }

            return list;
        }


        /// <summary>
        /// 获取所有属性的分类表
        /// </summary>
        /// <param name="wavelengths">反射率波长点</param>
        /// <returns>样品属性分类表</returns>
        public static List<SamplePropertyCategory> GetPropertyCategories(IEnumerable<int> wavelengths)
        {
            if (wavelengths == null)
            {
                wavelengths = Enumerable.Range(0, 31).Select(x => 400 + x * 10).ToArray();
            }

            var wavelengthArray = wavelengths.ToArray();

            var categories = new List<SamplePropertyCategory>();

            foreach (var topCategory in SampleDataItemCategoryExtensions.AllTopCategoryTypes)
            {
                SamplePropertyCategory category;
                var subCategories = topCategory.GetSubCategories();
                if (subCategories.Length == 1)
                {
                    category = new SamplePropertyCategory(subCategories[0].ToString(),
                        GetCategoryItems(subCategories[0], wavelengthArray));
                }
                else
                {
                    category = new SamplePropertyCategory(topCategory.ToString(),
                        subCategories.Select(subCategory => new SamplePropertyCategory(
                            subCategory.ToString(),
                            GetCategoryItems(subCategory, wavelengthArray))).ToArray());
                }

                categories.Add(category);
            }

            return categories;
        }


        public static SamplePropertyCategoryType GetPropertyCategoryType(string propertyName)
        {
            foreach (var category in SampleDataItemCategoryExtensions.AllCategoryTypes)
            {
                if (propertyName.StartsWith(category.ToString() + "_"))
                    return category;
            }
            throw new ArgumentException($"Unknown the property name \"{propertyName}\"");
        }

        #region 辅助函数

        static SamplePropertySettingInfo NewSettingInfo<ValueType>(string name, ValueType defaultValue)
        {
            return new SamplePropertySettingInfo(name, typeof(ValueType), defaultValue);
        }


        static SamplePropertyInfo CreatePropertyInfo(Type valueType, EvaluateSampleProperty evaluate,
            [CallerMemberName] string propertyName = null)
        {
            return new SamplePropertyInfo()
            {
                Name = propertyName,
                ValueType = valueType,
                Evaluate = evaluate
            };
        }


        /// <summary>
        /// 获取颜色设置
        /// </summary>
        internal static void GetColorSetting(IDictionary<string, object> settings, out string channel,
            out StandardIlluminant illuminant,
            out StandardObserver observer)//s3_L
        {
            IColorSetting colorSetting = settings.ContainsKey(SamplePropertySettingKeys.ColorSetting)
                ? (IColorSetting)(settings[SamplePropertySettingKeys.ColorSetting])
                : null;

            channel = settings.ContainsKey(SamplePropertySettingKeys.Channel)
                ? settings[SamplePropertySettingKeys.Channel] as string
                : colorSetting?.Channel ??
                  throw new ArgumentException("The 'Channel' is not specified", nameof(settings));

            illuminant = settings.ContainsKey(SamplePropertySettingKeys.Illuminant)
                ? (StandardIlluminant)settings[SamplePropertySettingKeys.Illuminant]
                : colorSetting?.Illuminant ??
                  throw new ArgumentException("The 'Illuminant' is not specified", nameof(settings));

            observer = settings.ContainsKey(SamplePropertySettingKeys.Observer)
                ? (StandardObserver)settings[SamplePropertySettingKeys.Observer]
                : colorSetting?.Observer ??
                  throw new ArgumentException("The 'Observer' is not specified", nameof(settings));
        }

        /// <summary>
        /// 获取密度设置
        /// </summary>
        internal static void GetDensitySetting(IDictionary<string, object> settings, out string channel,
            out DensityStatus status)//s3_M
        {
            IDensitySetting colorSetting = settings.ContainsKey(SamplePropertySettingKeys.DensitySetting)
                ? (IDensitySetting)(settings[SamplePropertySettingKeys.DensitySetting])
                : null;//colorSetting.DensityWhiteBase 是绝对白，还是减纸张

            channel = settings.ContainsKey(SamplePropertySettingKeys.Channel)
                ? settings[SamplePropertySettingKeys.Channel] as string
                : colorSetting?.Channel ??
                  throw new ArgumentException("The 'Channel' is not specified", nameof(settings));

            status = settings.ContainsKey(SamplePropertySettingKeys.DensityStatus)
                ? (DensityStatus)settings[SamplePropertySettingKeys.DensityStatus]
                : colorSetting?.DensityStatus ??
                  throw new ArgumentException("The 'DensityStatus' is not specified", nameof(settings));
        }

        internal static string GetColorChannel(IDictionary<string, object> settings)
        {
            if (settings.ContainsKey(SamplePropertySettingKeys.Channel))
                return (string)settings[SamplePropertySettingKeys.Channel];
            if (settings.ContainsKey(SamplePropertySettingKeys.ColorSetting))
                return ((IColorSetting)settings[SamplePropertySettingKeys.ColorSetting]).Channel;
            throw new ArgumentException("The 'Channel' is not specified", nameof(settings));
        }

        public static string GetDensityChannel(IDictionary<string, object> settings)
        {
            if (settings.ContainsKey(SamplePropertySettingKeys.Channel))
                return (string)settings[SamplePropertySettingKeys.Channel];
            if (settings.ContainsKey(SamplePropertySettingKeys.DensitySetting))
                return ((IDensitySetting)settings[SamplePropertySettingKeys.DensitySetting]).Channel;
            throw new ArgumentException("The 'Channel' is not specified", nameof(settings));
        }


        public static void GetDE00Factors(IDictionary<string, object> settings,
            out double KL, out double KC, out double KH)
        {
            ICIEDE2000Factors factors = settings.ContainsKey(SamplePropertySettingKeys.CIEDE00Factors)
                ? (ICIEDE2000Factors)settings[SamplePropertySettingKeys.CIEDE00Factors]
                : null;

            KL = settings.ContainsKey(SamplePropertySettingKeys.CIEDE00FactorL)
                ? Convert.ToDouble(settings[SamplePropertySettingKeys.CIEDE00FactorL])
                : factors?.L ?? throw new ArgumentException("Cannot find the factor L");
            KC = settings.ContainsKey(SamplePropertySettingKeys.CIEDE00FactorC)
                ? Convert.ToDouble(settings[SamplePropertySettingKeys.CIEDE00FactorC])
                : factors?.C ?? throw new ArgumentException("Cannot find the factor C");
            KH = settings.ContainsKey(SamplePropertySettingKeys.CIEDE00FactorH)
                ? Convert.ToDouble(settings[SamplePropertySettingKeys.CIEDE00FactorH])
                : factors?.H ?? throw new ArgumentException("Cannot find the factor H");
        }

        public static void GetDE94Factors(IDictionary<string, object> settings,
            out double KL, out double KC, out double KH)
        {
            ICIEDE1994Factors factors = settings.ContainsKey(SamplePropertySettingKeys.CIEDE94Factors)
                ? (ICIEDE1994Factors)settings[SamplePropertySettingKeys.CIEDE94Factors]
                : null;

            KL = settings.ContainsKey(SamplePropertySettingKeys.CIEDE94FactorL)
                ? Convert.ToDouble(settings[SamplePropertySettingKeys.CIEDE94FactorL])
                : factors?.L ?? throw new ArgumentException("Cannot find the factor L");
            KC = settings.ContainsKey(SamplePropertySettingKeys.CIEDE94FactorC)
                ? Convert.ToDouble(settings[SamplePropertySettingKeys.CIEDE94FactorC])
                : factors?.C ?? throw new ArgumentException("Cannot find the factor C");
            KH = settings.ContainsKey(SamplePropertySettingKeys.CIEDE94FactorH)
                ? Convert.ToDouble(settings[SamplePropertySettingKeys.CIEDE94FactorH])
                : factors?.H ?? throw new ArgumentException("Cannot find the factor H");
        }

        public static void GetCMCDEFactors(IDictionary<string, object> settings,
            out double KL, out double KC)
        {
            ICMCFactors factors = settings.ContainsKey(SamplePropertySettingKeys.CMCDEFactors)
                ? (ICMCFactors)settings[SamplePropertySettingKeys.CMCDEFactors]
                : null;

            KL = settings.ContainsKey(SamplePropertySettingKeys.CMCDEFactorL)
                ? Convert.ToDouble(settings[SamplePropertySettingKeys.CMCDEFactorL])
                : factors?.L ?? throw new ArgumentException("Cannot find the factor L");
            KC = settings.ContainsKey(SamplePropertySettingKeys.CMCDEFactorC)
                ? Convert.ToDouble(settings[SamplePropertySettingKeys.CMCDEFactorC])
                : factors?.C ?? throw new ArgumentException("Cannot find the factor C");
        }


        delegate TRet CalculateIndexDelegate<TRet>(CIELAB standard, CIELAB test);
        delegate TRet CalculateIndexExDelegate<TRet>(CIELAB standard, CIELAB test, StandardIlluminant illuminant, StandardObserver observer);

        private static object CalculateIndex<TRet>(IDictionary<string, object> settings,
            ISample sample, ISample target,
            CalculateIndexDelegate<TRet> calculate)
        {
            var trialLab = sample.GetLab(settings);
            var targetLab = target.GetLab(settings);
            if (trialLab != null && targetLab != null)
            {
                return calculate(targetLab, trialLab);
            }

            return null;
        }

        private static object CalculateIndexEx<TRet>(IDictionary<string, object> settings,
    ISample sample, ISample target,
    CalculateIndexExDelegate<TRet> calculate)
        {
            var trialLab = sample.GetLab(settings);
            var targetLab = target.GetLab(settings);
            var temp = SampleExtensions.GetSetting(settings);
            if (trialLab != null && targetLab != null)
            {
                return calculate(targetLab, trialLab, temp.Key, temp.Value);
            }

            return null;
        }

        public static RgbColorSystem GetRgbColorSystem(IDictionary<string, object> settings)
        {
            if (settings.ContainsKey(SamplePropertySettingKeys.RgbColorSystem))
            {
                var value = settings[SamplePropertySettingKeys.RgbColorSystem];
                // 如果是字符串表示是标准的色空间名
                if (value is string rgbColorSystemName)
                {
                    var filed = typeof(RgbColorSystems).GetField(rgbColorSystemName);
                    if (filed == null)
                    {
                        //throw new ArgumentException($"Unknown rgb color system '{rgbColorSystemName}'");
                        return null;
                    }

                    return (RgbColorSystem)filed.GetValue(null);
                }

                if (value is RgbColorSystem rgbColorSystem)
                {
                    return rgbColorSystem;
                }

            }

            return null;
        }

        public static double GetRgbGamma(IDictionary<string, object> settings)
        {
            if (settings.ContainsKey(SamplePropertySettingKeys.RgbGamma))
            {
                return Convert.ToDouble(settings[SamplePropertySettingKeys.RgbGamma]);
            }

            return 1.0;
        }

        #endregion

        #region 通用属性

        // 名称
        public static SamplePropertyInfo Common_Name()
        {
            return new SamplePropertyInfo()
            {
                Name = nameof(Common_Name),
                ValueType = typeof(string),
                Evaluate = (settings, sample, target, whiteBase) => sample?.Name
            };
        }

        // 时间
        public static SamplePropertyInfo Common_DateTime()
        {
            return new SamplePropertyInfo()
            {
                Name = nameof(Common_DateTime),
                ValueType = typeof(DateTime),
                Evaluate = (settings, sample, target, whiteBase) => sample?.DateTime
            };
        }

        // 料号
        public static SamplePropertyInfo Common_Material()
        {
            return CreatePropertyInfo(typeof(string), (settings, sample, target, whiteBase) => sample?.Material);
        }

        // 备注
        public static SamplePropertyInfo Common_Remark()
        {
            return CreatePropertyInfo(typeof(string), (settings, sample, target, whiteBase) => sample?.Remark);
        }

        // 配方
        public static SamplePropertyInfo Common_ColorFormula()
        {
            return CreatePropertyInfo(typeof(string), (settings, sample, target, whiteBase) => sample?.ColorFormula);
        }

        //绝对、纸张白
        public static SamplePropertyInfo Common_AbsMinusPaperTypeLilin()
        {
            return CreatePropertyInfo(typeof(string), (settings, sample, target, whiteBase) => SampleExtensions.GetWhitePaperSetLilin(settings));
        }
        #endregion

        #region 颜色属性

        // 测量条件
        public static SamplePropertyInfo Color_Channel()
        {
            return CreatePropertyInfo(typeof(string),
                (settings, sample, target, whiteBase) =>
                    (GetColorChannel(settings)));
        }

        // 光源/观察者
        public static SamplePropertyInfo Color_IlluminantObserver()
        {
            return CreatePropertyInfo(typeof(string), (settings, sample, target, whiteBase) =>
            {
                GetColorSetting(settings, out var channel, out var illuminant, out var observer);
                return string.Format("{0}/{1}", illuminant.ToString(),
                    observer == StandardObserver.CIE1931 ? "2\u00B0" : "10\u00B0");
            });
        }

        // 仿真色
        public static SamplePropertyInfo Color_PseudoColor()
        {
            return CreatePropertyInfo(typeof(uint), (settings, sample, target, whiteBase) =>
                sample?.GetRgb(settings)?.ToInteger());
        }

        // CIE L*a*b* - L*
        public static SamplePropertyInfo Color_CIELAB_L()//s1_L
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetLab(settings)?.L);
        }

        // CIE L*a*b* - a*
        public static SamplePropertyInfo Color_CIELAB_a()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetLab(settings)?.a);
        }

        // CIE L*a*b* - b*
        public static SamplePropertyInfo Color_CIELAB_b()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetLab(settings)?.b);
        }

        // CIE L*C*h° - C*
        public static SamplePropertyInfo Color_CIELCH_C()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetLab(settings)?.C);
        }

        // CIE L*C*h° - h°
        public static SamplePropertyInfo Color_CIELCH_h()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetLab(settings)?.h);
        }

        // 三刺激值X
        public static SamplePropertyInfo Color_CIEXYZ_X()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetXYZ(settings)?.X);
        }

        // 三刺激值Y
        public static SamplePropertyInfo Color_CIEXYZ_Y()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetXYZ(settings)?.Y);
        }

        // 三刺激值Z
        public static SamplePropertyInfo Color_CIEXYZ_Z()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetXYZ(settings)?.Z);
        }

        // 色品坐标x
        public static SamplePropertyInfo Color_CIEYxy_x()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetXYZ(settings)?.x);
        }

        // 色品坐标y
        public static SamplePropertyInfo Color_CIEYxy_y()
        {
            return CreatePropertyInfo(typeof(double), (settings, sample, target, white) =>
                sample?.GetXYZ(settings)?.y);
        }

        // CIELUV 1976 L*
        public static SamplePropertyInfo Color_Luv1976_L()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetLuv1976(settings)?.L));
        }

        // CIELUV 1976 u*
        public static SamplePropertyInfo Color_Luv1976_u()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetLuv1976(settings)?.u));
        }

        // CIELUV 1976 v*
        public static SamplePropertyInfo Color_Luv1976_v()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetLuv1976(settings)?.v));
        }

        // Hunter Lab - L
        public static SamplePropertyInfo Color_HunterLab_L()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetHunterLab(settings)?.L));
        }

        // Hunter Lab - a
        public static SamplePropertyInfo Color_HunterLab_a()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetHunterLab(settings)?.a));
        }

        // Hunter Lab - b
        public static SamplePropertyInfo Color_HunterLab_b()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetHunterLab(settings)?.b));
        }

        // 仿真色 R
        public static SamplePropertyInfo Color_RGB_R()
        {
            return CreatePropertyInfo(typeof(int), ((settings, sample, target, white) =>
                (int?)sample?.GetRgb(settings)?.R));
        }

        // 仿真色 G
        public static SamplePropertyInfo Color_RGB_G()
        {
            return CreatePropertyInfo(typeof(int), ((settings, sample, target, white) =>
                (int?)sample?.GetRgb(settings)?.G));
        }

        // 仿真色 B
        public static SamplePropertyInfo Color_RGB_B()
        {
            return CreatePropertyInfo(typeof(int), ((settings, sample, target, white) =>
                (int?)sample?.GetRgb(settings)?.B));
        }

        #endregion

        #region 密度属性

        // 密度状态
        public static SamplePropertyInfo Density_DensityStatus()
        {
            return CreatePropertyInfo(typeof(string),
                ((settings, sample, target, white) =>
                {
                    GetDensitySetting(settings, out var channel, out var status);
                    return status.ToString();
                }));
        }

        // C密度
        public static SamplePropertyInfo Density_C()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDensity(white, settings, DensityColor.C)));
        }

        // M密度
        public static SamplePropertyInfo Density_M()//s1_M
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDensity(white, settings, DensityColor.M)));
        }

        // Y密度
        public static SamplePropertyInfo Density_Y()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDensity(white, settings, DensityColor.Y)));
        }

        // K密度
        public static SamplePropertyInfo Density_K()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDensity(white, settings, DensityColor.K)));
        }

        // 自动密度色
        public static SamplePropertyInfo Density_AutoColor()
        {
            return CreatePropertyInfo(typeof(string),
                ((settings, sample, target, white) =>
                {
                    // 如果有标样，自动密度色与标样相同
                    var autoColor = target?.GetAutoDensity(settings)?.Item1;
                    return autoColor != null ? autoColor.ToString() : sample.GetAutoDensity(settings)?.Item1.ToString();
                }));
        }

        // 自动密度值
        public static SamplePropertyInfo Density_Auto()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    // 如果有标样，自动密度色与标样相同
                    var densityColor = target?.GetAutoDensity(settings)?.Item1 ?? DensityColor.Auto;
                    return sample.GetDensity(white, settings, densityColor);
                }));
        }

        // 专色密度
        public static SamplePropertyInfo Density_Spot()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    // 如果有标样，自动专色密度色与标样相同
                    var wavelength = target?.GetSpotDensity(white, settings)?.Item1;
                    return wavelength != null
                        ? sample.GetSpotDensity(white, settings, wavelength.Value)
                        : sample.GetSpotDensity(white, settings)?.Item2;
                }));
        }

        // 专色密度波长
        public static SamplePropertyInfo Density_SpotWavelength()
        {
            return CreatePropertyInfo(typeof(int),
                ((settings, sample, target, white) =>
                {
                    var wavelength = target?.GetSpotDensity(white, settings)?.Item1;
                    return wavelength ?? sample.GetSpotDensity(white, settings)?.Item1;
                }));
        }

        // 色调误差
        public static SamplePropertyInfo Density_HueError()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetHueError(white, settings)));
        }

        // 灰度值
        public static SamplePropertyInfo Density_Grayness()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetGrayness(white, settings)));
        }

        #endregion

        #region 光谱属性

        // 指定波长的专色密度
        public static SamplePropertyInfo SpectralSpotDensity_At(int wavelength)
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetSpotDensity(white, settings, wavelength)),
                nameof(SpectralSpotDensity_At) + "_" + wavelength);
        }

        // 指定波长的密度色差
        public static SamplePropertyInfo SpectralSpotDensityDiff_At(int wavelength)
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                    sample.GetSpotDensity(white, settings, wavelength) -
                    target.GetSpotDensity(white, settings, wavelength)),
                nameof(SpectralSpotDensityDiff_At) + "_" + wavelength);
        }

        // 指定波长的光谱数据
        public static SamplePropertyInfo SpectralData_At(int wavelength)
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                    sample?.GetSpectralData(settings)?[wavelength] * 100.0),
                nameof(SpectralData_At) + "_" + wavelength);
        }

        // 指定波长光谱的差值
        public static SamplePropertyInfo SpectralDiff_At(int wavelength)
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                    sample?.GetSpectralData(settings)?[wavelength] * 100.0 -
                    target?.GetSpectralData(settings)?[wavelength] * 100.0),
                nameof(SpectralDiff_At) + "_" + wavelength);
        }

        // 指定波长处的K/S值或吸收率
        // 绝对力份
        public static SamplePropertyInfo SpectralStrength_At(int wavelength)
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var clr = sample?.GetColorData(GetColorChannel(settings));
                    if (clr?.SpectralData != null)
                    {
                        return clr.SpectralData.GetColorValueSWLAt(wavelength, clr.TransmissionFlag);
                    }

                    return null;
                }),
                nameof(SpectralStrength_At) + "_" + wavelength);
        }

        // 指定波长处的% Strength
        public static SamplePropertyInfo SpectralRelativeStrength_At(int wavelength)
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var channel = GetColorChannel(settings);
                    var trialClr = sample?.GetColorData(channel);
                    var targetClr = target?.GetColorData(channel);
                    if (trialClr?.SpectralData != null && targetClr?.SpectralData != null &&
                        trialClr.TransmissionFlag == targetClr.TransmissionFlag)
                    {
                        if (!trialClr.SpectralData.Contains(wavelength) || !targetClr.SpectralData.Contains(wavelength))
                            return null;
                        return targetClr.SpectralData.GetRelativeStrength(trialClr.SpectralData, wavelength, trialClr.TransmissionFlag);
                    }

                    return null;
                }),
                nameof(SpectralRelativeStrength_At) + "_" + wavelength);
        }

        #endregion

        #region 色差属性

        // ΔL*
        public static SamplePropertyInfo ColorDiff_CIELAB_L()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetLab(settings)?.L - target.GetLab(settings)?.L));
        }

        // Δa*
        public static SamplePropertyInfo ColorDiff_CIELAB_a()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetLab(settings)?.a - target.GetLab(settings)?.a));
        }

        // Δb*
        public static SamplePropertyInfo ColorDiff_CIELAB_b()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetLab(settings)?.b - target.GetLab(settings)?.b));
        }

        // ΔC*
        public static SamplePropertyInfo ColorDiff_CIELCH_C()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetLab(settings)?.C - target.GetLab(settings)?.C));
        }

        // ΔH*
        public static SamplePropertyInfo ColorDiff_CIELCH_H()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var labSample = sample.GetLab(settings);
                    var labTarget = target.GetLab(settings);
                    if (labSample != null && labTarget != null)
                    {
                        return labSample.CalculateDeltaH(labTarget);
                    }

                    return null;
                }));
        }

        // ΔX
        public static SamplePropertyInfo ColorDiff_CIEXYZ_X()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetXYZ(settings)?.X - target.GetXYZ(settings)?.X));
        }

        // ΔY
        public static SamplePropertyInfo ColorDiff_CIEXYZ_Y()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetXYZ(settings)?.Y - target.GetXYZ(settings)?.Y));
        }

        // ΔZ
        public static SamplePropertyInfo ColorDiff_CIEXYZ_Z()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetXYZ(settings)?.Z - target.GetXYZ(settings)?.Z));
        }

        // Δx
        public static SamplePropertyInfo ColorDiff_CIEYxy_x()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetXYZ(settings)?.x - target.GetXYZ(settings)?.x));
        }

        // Δy
        public static SamplePropertyInfo ColorDiff_CIEYxy_y()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetXYZ(settings)?.y - target.GetXYZ(settings)?.y));
        }


        // CIELUV 1976 L*
        public static SamplePropertyInfo ColorDiff_Luv1976_L()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetLuv1976(settings)?.L - target?.GetLuv1976(settings)?.L));
        }

        // CIELUV 1976 u*
        public static SamplePropertyInfo ColorDiff_Luv1976_u()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetLuv1976(settings)?.u - target?.GetLuv1976(settings)?.u));
        }

        // CIELUV 1976 v*
        public static SamplePropertyInfo ColorDiff_Luv1976_v()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetLuv1976(settings)?.v - target?.GetLuv1976(settings)?.v));
        }

        // Hunter Lab - L
        public static SamplePropertyInfo ColorDiff_HunterLab_L()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetHunterLab(settings)?.L - target?.GetHunterLab(settings)?.L));
        }

        // Hunter Lab - a
        public static SamplePropertyInfo ColorDiff_HunterLab_a()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetHunterLab(settings)?.a - target?.GetHunterLab(settings)?.a));
        }

        // Hunter Lab - b
        public static SamplePropertyInfo ColorDiff_HunterLab_b()
        {
            return CreatePropertyInfo(typeof(double), ((settings, sample, target, white) =>
                sample?.GetHunterLab(settings)?.b - target?.GetHunterLab(settings)?.b));
        }

        // 仿真色 R
        public static SamplePropertyInfo ColorDiff_RGB_R()
        {
            return CreatePropertyInfo(typeof(int), ((settings, sample, target, white) =>
                sample?.GetRgb(settings)?.R - target?.GetRgb(settings)?.R));
        }

        // 仿真色 G
        public static SamplePropertyInfo ColorDiff_RGB_G()
        {
            return CreatePropertyInfo(typeof(int), ((settings, sample, target, white) =>
                sample?.GetRgb(settings)?.G - target?.GetRgb(settings)?.G));
        }

        // 仿真色 B
        public static SamplePropertyInfo ColorDiff_RGB_B()
        {
            return CreatePropertyInfo(typeof(int), ((settings, sample, target, white) =>
                sample?.GetRgb(settings)?.B - target?.GetRgb(settings)?.B));
        }

        #endregion

        #region 色差公式

        // ΔE*
        public static SamplePropertyInfo ColorDiffFormula_DE1976()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe1976(target, settings)?.dE));
        }

        // ΔE1994
        public static SamplePropertyInfo ColorDiffFormula_DE1994()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe1994(target, settings)?.dE));
        }

        // ΔL94
        public static SamplePropertyInfo ColorDiffFormula_DL1994()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe1994(target, settings)?.dL));
        }

        // ΔC94
        public static SamplePropertyInfo ColorDiffFormula_DC1994()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe1994(target, settings)?.dC));
        }

        // ΔH94
        public static SamplePropertyInfo ColorDiffFormula_DH1994()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe1994(target, settings)?.dH));
        }


        // ΔE2000
        public static SamplePropertyInfo ColorDiffFormula_DE2000()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe2000(target, settings)?.dE));
        }


        // ΔL00
        public static SamplePropertyInfo ColorDiffFormula_DL2000()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe2000(target, settings)?.dL));
        }

        // ΔC00
        public static SamplePropertyInfo ColorDiffFormula_DC2000()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe2000(target, settings)?.dC));
        }

        // ΔH00
        public static SamplePropertyInfo ColorDiffFormula_DH2000()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDe2000(target, settings)?.dH));
        }

        // CMC
        public static SamplePropertyInfo ColorDiffFormula_CMCDE()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample?.GetCmcDe(target, settings)?.dE));
        }

        // CMC-ΔL00
        public static SamplePropertyInfo ColorDiffFormula_DLcmc()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample?.GetCmcDe(target, settings)?.dL));
        }

        // CMC-ΔC00
        public static SamplePropertyInfo ColorDiffFormula_DCcmc()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample?.GetCmcDe(target, settings)?.dC));
        }

        // CMC-ΔH
        public static SamplePropertyInfo ColorDiffFormula_DHcmc()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample?.GetCmcDe(target, settings)?.dH));
        }

        #endregion

        #region 密度差属性

        // 密度 ΔC
        public static SamplePropertyInfo DensityDiff_C()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDensity(white, settings, DensityColor.C) -
                                                      target.GetDensity(white, settings, DensityColor.C)));
        }

        // 密度 ΔM
        public static SamplePropertyInfo DensityDiff_M()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDensity(white, settings, DensityColor.M) -
                                                      target.GetDensity(white, settings, DensityColor.M)));
        }

        // 密度 ΔY
        public static SamplePropertyInfo DensityDiff_Y()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDensity(white, settings, DensityColor.Y) -
                                                      target.GetDensity(white, settings, DensityColor.Y)));
        }

        // 密度 ΔK
        public static SamplePropertyInfo DensityDiff_K()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) => sample.GetDensity(white, settings, DensityColor.K) -
                                                      target.GetDensity(white, settings, DensityColor.K)));
        }

        // 自动密度，密度色使用标样的自动密度
        public static SamplePropertyInfo DensityDiff_Auto()
        {
            return CreatePropertyInfo(typeof(double),
                (((settings, sample, target, white) =>
                {
                    var tupleSample = sample.GetAutoDensity(white, settings);
                    if (tupleSample == null) return null;
                    var dt = target.GetDensity(white, settings, tupleSample.Item1);
                    if (dt != null)
                        return tupleSample.Item2 - dt.Value;
                    return null;
                })));
        }

        // 专色密度，专色波长使用标样的专色波长
        public static SamplePropertyInfo DensityDiff_Spot()
        {
            return CreatePropertyInfo(typeof(double),
                (((settings, sample, target, white) =>
                {
                    var tupleSample = sample.GetSpotDensity(white, settings);
                    if (tupleSample == null) return null;
                    var dt = target.GetSpotDensity(white, settings, tupleSample.Item1);
                    if (dt != null)
                        return tupleSample.Item2 - dt.Value;
                    return null;
                })));
        }

        #endregion

        #region 颜色指数

        // ASTM-E313白度的Tint指数
        public static SamplePropertyInfo ColorIndex_AstmE313_Tint()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var xyz = sample.GetXYZ(settings);
                    var temp = SampleExtensions.GetSetting(settings);
                    return xyz.GetAstmE313Tint();
                }));
        }

        // ASTM-E313白度
        public static SamplePropertyInfo ColorIndex_AstmE313_WI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var xyz = sample.GetXYZ(settings);
                    return xyz.GetAstmE313Whiteness();
                }));
        }

        // CIE ISO Tint指数
        public static SamplePropertyInfo ColorIndex_CIEISO_Tint()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var xyz = sample.GetXYZ(settings);
                    return xyz.GetCIEISOTint();
                }));
        }

        // CIE ISO 白度
        public static SamplePropertyInfo ColorIndex_CIEISO_WI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var xyz = sample.GetXYZ(settings);
                    return xyz.GetAstmE313Whiteness();
                }));
        }

        // 坦伯白度
        // 在A、D55、D65、C、D75光源下有效
        public static SamplePropertyInfo ColorIndex_Tapple_WI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var xyz = sample.GetXYZ(settings);
                    return xyz.GetTapplWhiteness();
                }));
        }

        // 陶贝白度
        // 在A、D55、D65、C、D75光源下有效
        public static SamplePropertyInfo ColorIndex_Taubel_WI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var xyz = sample.GetXYZ(settings);
                    return xyz.GetTaubeWhiteness();
                }));
        }

        // Hunter 白度（1942）
        // D65/10和C/2光源下有效
        public static SamplePropertyInfo ColorIndex_Hunter1942_WI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var lab = sample.GetHunterLab(settings);
                    var temp = SampleExtensions.GetSetting(settings);
                    return lab.GetHunter1942Whiteness(temp.Key, temp.Value);
                }));
        }

        // Hunter 白度（1960）
        // D65/10和C/2光源下有效
        public static SamplePropertyInfo ColorIndex_Hunter1960_WI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var lab = sample.GetHunterLab(settings);
                    var temp = SampleExtensions.GetSetting(settings);
                    return lab.GetHunter1960Whiteness(temp.Key, temp.Value);
                }));
        }

        // R457白度
        public static SamplePropertyInfo ColorIndex_R457_WI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var spectra = sample.GetSpectralData(settings);
                    return spectra.GetR457Whiteness();
                }));
        }

        // ASTM D1925-1970 黄度 
        public static SamplePropertyInfo ColorIndex_ASTM1925_1970_YI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var channel = GetColorChannel(settings);
                    var xyz = sample.GetXYZ(channel, StandardIlluminant.C, StandardObserver.CIE1931);
                    return xyz.GetAstmD1925Yellowness();
                }));
        }

        // ASTM E313-2005 黄度
        public static SamplePropertyInfo ColorIndex_AstmE313_YI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var xyz = sample.GetXYZ(settings);
                    return xyz.GetAstmE313Yellowness();
                }));
        }

        // 同色异谱指数
        public static SamplePropertyInfo ColorIndex_MI()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var trialSpectra = sample.GetSpectralData(settings);
                    var targetSpectra = target.GetSpectralData(settings);
                    if (trialSpectra != null && targetSpectra != null)
                    {
                        var temp = SampleExtensions.GetSetting(settings);
                        return ColorIndexExtension.GetMetamerismIndiex(targetSpectra, trialSpectra,
                            temp.Key, temp.Value,
                            (StandardIlluminant)settings[SamplePropertySettingKeys.RefIlluminant],
                            (StandardObserver)settings[SamplePropertySettingKeys.RefObserver]);
                    }

                    return null;
                }));
        }

        // 沾色牢度的dEgs
        public static SamplePropertyInfo ColorIndex_StainingFastness_dEgs()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                    CalculateIndexEx(settings, sample, target,
                        ColorIndexExtension.GetStainingFastness_dEgs)));
        }

        // 沾色牢度级数（连续的）
        //        public static SamplePropertyInfo ColorIndex_StainingFastness_SSR()
        //        {
        //            return CreatePropertyInfo(typeof(double),
        //                ((settings, sample, target, white) =>
        //                    CalculateIndex(settings, sample, target,
        //                        SampleExtensions.GetFormula(settings).GetStainingFastness_SSR)));
        //        }

        // 沾色牢度级数
        public static SamplePropertyInfo ColorIndex_ReportedSSR()
        {
            return CreatePropertyInfo(typeof(string),
                ((settings, sample, target, white) =>
                    CalculateIndexEx(settings, sample, target,
                        ColorIndexExtension.GetReportedSSR)?.ToString()));
        }

        // 变色牢度级数（连续的）
        //        public static SamplePropertyInfo ColorIndex_ColorFastness_GSR()
        //        {
        //            return CreatePropertyInfo(typeof(double),
        //                ((settings, sample, target, white) =>
        //                    CalculateIndex(settings, sample, target,
        //                        SampleExtensions.GetFormula(settings).GetColorFastness_GSR)));
        //        }

        // 变色牢度级数
        public static SamplePropertyInfo ColorIndex_ReportedGSR()
        {
            return CreatePropertyInfo(typeof(string),
                ((settings, sample, target, white) =>
                    CalculateIndexEx(settings, sample, target,
                        ColorIndexExtension.GetReportedGSR)?.ToString()));
        }

        // 变色牢度的ΔEf
        public static SamplePropertyInfo ColorIndex_ColorFastness_dEf()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                    CalculateIndexEx(settings, sample, target,
                        ColorIndexExtension.GetColorFastness_dEf)));
        }

        // Color Value Sum
        public static SamplePropertyInfo ColorIndex_ColorValueSum()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var clr = sample?.GetColorData(GetColorChannel(settings));
                    return clr?.SpectralData.GetColorValueSum(clr.TransmissionFlag);
                }));
        }

        // Color Value SWL
        public static SamplePropertyInfo ColorIndex_ColorValueSWL()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var clr = sample?.GetColorData(GetColorChannel(settings));
                    return clr?.SpectralData.GetColorValueSWL( clr.TransmissionFlag, out int tmp);
                }));
        }

        // 最大吸收（或最小反射/透射）波长
        public static SamplePropertyInfo ColorIndex_WavelengthOfMaximumAbsorption()
        {
            return CreatePropertyInfo(typeof(int),
                ((settings, sample, target, white) =>
                {
                    var clr = sample?.GetColorData(GetColorChannel(settings));
                    if (clr?.SpectralData != null)
                    {
                        clr.SpectralData.GetColorValueSWL(clr.TransmissionFlag,
                            out int wavelengthOfMaximumAbsorption);
                        return wavelengthOfMaximumAbsorption;
                    }

                    return null;
                }));
        }


        // Color Value WSUM
        public static SamplePropertyInfo ColorIndex_ColorValueWSUM()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var clr = sample?.GetColorData(GetColorChannel(settings));
                    var temp = SampleExtensions.GetSetting(settings);
                    return clr?.SpectralData.GetColorValueWSUM(clr.TransmissionFlag,temp.Key,temp.Value);
                }));
        }

        // Average Strength
        public static SamplePropertyInfo ColorIndex_AverageStrength()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var channel = GetColorChannel(settings);
                    var trialClr = sample?.GetColorData(channel);
                    var targetClr = target?.GetColorData(channel);
                    if (trialClr?.SpectralData != null && targetClr?.SpectralData != null &&
                        trialClr.TransmissionFlag == targetClr.TransmissionFlag)
                    {
                        return trialClr.SpectralData.GetAverageStrength(targetClr.SpectralData,targetClr.TransmissionFlag);
                    }

                    return null;
                }));
        }

        // Weighted Strength
        public static SamplePropertyInfo ColorIndex_WeightedStrength()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var channel = GetColorChannel(settings);
                    var trialClr = sample?.GetColorData(channel);
                    var targetClr = target?.GetColorData(channel);
                    if (trialClr?.SpectralData != null && targetClr?.SpectralData != null &&
                        trialClr.TransmissionFlag == targetClr.TransmissionFlag)
                    {
                        var temp = SampleExtensions.GetSetting(settings);
                        return trialClr?.SpectralData.GetWeightedStrength(trialClr.SpectralData, targetClr.TransmissionFlag,temp.Key,temp.Value);
                    }

                    return null;
                }));
        }

        // Strength SWL
        public static SamplePropertyInfo ColorIndex_StrengthSWL()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var channel = GetColorChannel(settings);
                    var trialClr = sample?.GetColorData(channel);
                    var targetClr = target?.GetColorData(channel);
                    if (trialClr?.SpectralData != null && targetClr?.SpectralData != null &&
                        trialClr.TransmissionFlag == targetClr.TransmissionFlag)
                    {
                        return trialClr.SpectralData.GetStrengthSWL(trialClr.SpectralData, targetClr.TransmissionFlag, out var tmp);
                    }

                    return null;
                }));
        }

        // 遮盖度
        public static SamplePropertyInfo ColorIndex_Opacity()
        {
            return CreatePropertyInfo(typeof(double),
                ((settings, sample, target, white) =>
                {
                    var trialXyz = sample.GetXYZ(settings); // 试样做白底
                    var targetXyz = target.GetXYZ(settings); // 标样做黑底
                    if (trialXyz != null && targetXyz != null)
                    {
                        return trialXyz.GetOpacity(targetXyz);
                    }

                    return null;
                }));
        }

        // 555分类
        public static SamplePropertyInfo ColorIndex_555Shade()
        {
            return CreatePropertyInfo(typeof(int),
                ((settings, sample, target, white) =>
                {
                    var trialLab = sample.GetLab(settings);
                    var targetLab = target.GetLab(settings);
                    if (trialLab != null && targetLab != null)
                    {
                        double tolL = settings.ContainsKey(SamplePropertySettingKeys.ShadeToleranceL)
                            ? (double)settings[SamplePropertySettingKeys.ShadeToleranceL]
                            : 1.0;
                        double tolA = settings.ContainsKey(SamplePropertySettingKeys.ShadeToleranceA)
                            ? (double)settings[SamplePropertySettingKeys.ShadeToleranceA]
                            : 1.0;
                        double tolB = settings.ContainsKey(SamplePropertySettingKeys.ShadeToleranceB)
                            ? (double)settings[SamplePropertySettingKeys.ShadeToleranceB]
                            : 1.0;

                        return targetLab.Get555Shade(trialLab, (float)tolL, (float)tolA, (float)tolB);
                    }

                    return null;
                }));
        }

        #endregion

        #region 方便函数

        /// <summary>
        /// 判断属性是否是CIEDE2000 的分量
        /// </summary>
        public static bool IsCieDe00Item(string propertyName)
        {
            return propertyName == nameof(ColorDiffFormula_DE2000) ||
                   propertyName == nameof(ColorDiffFormula_DL2000) ||
                   propertyName == nameof(ColorDiffFormula_DC2000) ||
                   propertyName == nameof(ColorDiffFormula_DH2000);
        }

        /// <summary>
        /// 判断属性是否是CIEDE1994 的分量
        /// </summary>
        public static bool IsCieDe94Item(string propertyName)
        {
            return propertyName == nameof(ColorDiffFormula_DE1994) ||
                   propertyName == nameof(ColorDiffFormula_DL1994) ||
                   propertyName == nameof(ColorDiffFormula_DC1994) ||
                   propertyName == nameof(ColorDiffFormula_DH1994);
        }

        /// <summary>
        /// 判断属性是否是CMCDE的分量
        /// </summary>
        public static bool IsCmcDeItem(string propertyName)
        {
            return propertyName == nameof(ColorDiffFormula_CMCDE) ||
                   propertyName == nameof(ColorDiffFormula_DLcmc) ||
                   propertyName == nameof(ColorDiffFormula_DCcmc) ||
                   propertyName == nameof(ColorDiffFormula_DHcmc);
        }

        /// <summary>
        /// 为给定的颜色指数获取所有有效的光源、观察者设置
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <returns>有效的光源/观察者角度设置</returns>
        public static Tuple<StandardIlluminant, StandardObserver>[] GetValidIlluminantObserverTuples(
            string propertyName)
        {
            switch (propertyName)
            {
                case nameof(ColorIndex_AstmE313_WI):
                case nameof(ColorIndex_AstmE313_Tint):
                    return new Tuple<StandardIlluminant, StandardObserver>[]
                    {
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D50,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1931),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1931),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D50,
                            StandardObserver.CIE1931),
                    };
                case nameof(ColorIndex_CIEISO_WI):
                case nameof(ColorIndex_CIEISO_Tint):
                    return new Tuple<StandardIlluminant, StandardObserver>[]
                    {
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1931),
                    };
                case nameof(ColorIndex_Tapple_WI):
                case nameof(ColorIndex_Taubel_WI):
                    return new Tuple<StandardIlluminant, StandardObserver>[]
                    {
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.A, StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D55,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D75,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.A, StandardObserver.CIE1931),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D55,
                            StandardObserver.CIE1931),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1931),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1931),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D75,
                            StandardObserver.CIE1931),
                    };
                case nameof(ColorIndex_Hunter1942_WI):
                case nameof(ColorIndex_Hunter1960_WI):
                    return new[]
                    {
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1931),
                    };
                case nameof(ColorIndex_AstmE313_YI):
                    return new[]
                    {
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1931),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1931),
                    };
                case nameof(ColorIndex_ASTM1925_1970_YI):
                    return new[]
                    {
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1931),
                    };
                case nameof(ColorIndex_StainingFastness_dEgs):
                case nameof(ColorIndex_ReportedSSR):
                    //                case nameof(ColorIndex_StainingFastness_SSR):
                    return new[]
                    {
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1931),
                    };
                case nameof(ColorIndex_ColorFastness_dEf):
                //                case nameof(ColorIndex_ColorFastness_GSR):
                case nameof(ColorIndex_ReportedGSR):
                    return new[]
                    {
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.D65,
                            StandardObserver.CIE1931),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1964),
                        new Tuple<StandardIlluminant, StandardObserver>(StandardIlluminant.C, StandardObserver.CIE1931),
                    };
                default:
                    // 默认返回所有可用光源/视场角组合
                    var query = from illuminant in Enum.GetValues(typeof(StandardIlluminant))
                            .OfType<StandardIlluminant>()
                                from observer in Enum.GetValues(typeof(StandardObserver)).OfType<StandardObserver>()
                                where illuminant != StandardIlluminant.Unknown
                                select new Tuple<StandardIlluminant, StandardObserver>(illuminant, observer);
                    return query.ToArray();
            }
        }

        /// <summary>
        /// 获取指定色空间的数据项的名称
        /// </summary>
        /// <param name="colorSpace">色空间</param>
        /// <returns>返回相应的色空间的数据项对应的函数的名称</returns>
        /// <exception cref="NotSupportedException">不支持时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string[] GetColorItems(ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.CIE_XYZ:
                    return new[]
                    {
                        nameof(Color_CIEXYZ_X),
                        nameof(Color_CIEXYZ_Y),
                        nameof(Color_CIEXYZ_Z),
                    };
                case ColorSpace.CIE_Yxy:
                    return new[]
                    {
                        nameof(Color_CIEXYZ_Y),
                        nameof(Color_CIEYxy_x),
                        nameof(Color_CIEYxy_y),
                    };
                case ColorSpace.CIE_LAB:
                    return new[]
                    {
                        nameof(Color_CIELAB_L),
                        nameof(Color_CIELAB_a),
                        nameof(Color_CIELAB_b),
                    };
                case ColorSpace.CIE_LCH:
                    return new[]
                    {
                        nameof(Color_CIELAB_L),
                        nameof(Color_CIELCH_C),
                        nameof(Color_CIELCH_h)
                    };
                case ColorSpace.CIE_LUV:
                    return new[]
                    {
                        nameof(Color_Luv1976_L),
                        nameof(Color_Luv1976_u),
                        nameof(Color_Luv1976_v),
                    };
                case ColorSpace.HUNTER_LAB:
                    return new[]
                    {
                        nameof(Color_HunterLab_L),
                        nameof(Color_HunterLab_a),
                        nameof(Color_HunterLab_b),
                    };
                case ColorSpace.sRGB:
                    return new[]
                    {
                        nameof(Color_RGB_R),
                        nameof(Color_RGB_G),
                        nameof(Color_RGB_B),
                    };
                case ColorSpace.DIN99_LAB:
                case ColorSpace.DIN99_LCh:
                case ColorSpace.BETA_xy:
                case ColorSpace.CIE_RGB:
                case ColorSpace.Munsell:
                    throw new NotSupportedException();
                //                case ColorSpace.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorSpace), colorSpace, null);
            }
        }

        public static string[] GetColorDiffItems(ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.CIE_XYZ:
                    return new[]
                    {
                        nameof(ColorDiff_CIEXYZ_X),
                        nameof(ColorDiff_CIEXYZ_Y),
                        nameof(ColorDiff_CIEXYZ_Z),
                    };
                case ColorSpace.CIE_Yxy:
                    return new[]
                    {
                        nameof(ColorDiff_CIEXYZ_Y),
                        nameof(ColorDiff_CIEYxy_x),
                        nameof(ColorDiff_CIEYxy_y),
                    };
                case ColorSpace.CIE_LAB:
                    return new[]
                    {
                        nameof(ColorDiff_CIELAB_L),
                        nameof(ColorDiff_CIELAB_a),
                        nameof(ColorDiff_CIELAB_b),
                    };
                case ColorSpace.CIE_LCH:
                    return new[]
                    {
                        nameof(ColorDiff_CIELAB_L),
                        nameof(ColorDiff_CIELCH_C),
                        nameof(ColorDiff_CIELCH_H)
                    };
                case ColorSpace.CIE_LUV:
                    return new[]
                    {
                        nameof(ColorDiff_Luv1976_L),
                        nameof(ColorDiff_Luv1976_u),
                        nameof(ColorDiff_Luv1976_v),
                    };
                case ColorSpace.HUNTER_LAB:
                    return new[]
                    {
                        nameof(ColorDiff_HunterLab_L),
                        nameof(ColorDiff_HunterLab_a),
                        nameof(ColorDiff_HunterLab_b),
                    };
                case ColorSpace.sRGB:
                    return new[]
                    {
                        nameof(ColorDiff_RGB_R),
                        nameof(ColorDiff_RGB_G),
                        nameof(ColorDiff_RGB_B),
                    };
                case ColorSpace.DIN99_LAB:
                case ColorSpace.DIN99_LCh:
                case ColorSpace.BETA_xy:
                case ColorSpace.CIE_RGB:
                case ColorSpace.Munsell:
                    throw new NotSupportedException();
                //                case ColorSpace.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorSpace), colorSpace, null);
            }
        }

        /// <summary>
        /// 获取属性值的有效范围
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <returns>如果支持返回相应范围，否则返回null</returns>
        public static Tuple<double, double> GetItemValueRange(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(SampleProperties.Color_CIELAB_L):
                case nameof(SampleProperties.Color_Luv1976_L):
                case nameof(SampleProperties.Color_HunterLab_L):
                    return new Tuple<double, double>(0.0, 200);
                case nameof(SampleProperties.Color_CIELAB_a):
                case nameof(SampleProperties.Color_HunterLab_a):
                case nameof(SampleProperties.Color_CIELAB_b):
                case nameof(SampleProperties.Color_HunterLab_b):
                    return new Tuple<double, double>(-300, 300);
                case nameof(SampleProperties.Color_CIEXYZ_X):
                case nameof(SampleProperties.Color_CIEXYZ_Y):
                case nameof(SampleProperties.Color_CIEXYZ_Z):
                    return new Tuple<double, double>(0, 200);
                case nameof(SampleProperties.Color_CIEYxy_x):
                    return new Tuple<double, double>(0, 0.8);
                case nameof(SampleProperties.Color_CIEYxy_y):
                    return new Tuple<double, double>(0, 0.9);
                case nameof(SampleProperties.Color_Luv1976_u):
                    return new Tuple<double, double>(0, 0.6);
                case nameof(SampleProperties.Color_Luv1976_v):
                    return new Tuple<double, double>(0, 0.6);
                case nameof(SampleProperties.Color_CIELCH_C):
                    return new Tuple<double, double>(0, 200);
                case nameof(SampleProperties.Color_CIELCH_h):
                    return new Tuple<double, double>(0, 360);
                case nameof(Density_C):
                case nameof(Density_M):
                case nameof(Density_Y):
                case nameof(Density_K):
                case nameof(Density_Spot):
                    return new Tuple<double, double>(0, 4.0);
                case nameof(ColorDiffFormula_DE1976):
                case nameof(ColorDiffFormula_DE1994):
                case nameof(ColorDiffFormula_DE2000):
                case nameof(ColorDiffFormula_CMCDE):
                    return new Tuple<double, double>(0, 100);
                case nameof(ColorDiffFormula_DL2000):
                case nameof(ColorDiffFormula_DC2000):
                case nameof(ColorDiffFormula_DH2000):
                case nameof(ColorDiffFormula_DL1994):
                case nameof(ColorDiffFormula_DC1994):
                case nameof(ColorDiffFormula_DH1994):
                case nameof(ColorDiffFormula_DLcmc):
                case nameof(ColorDiffFormula_DCcmc):
                case nameof(ColorDiffFormula_DHcmc):
                //                    return new Tuple<double, double>(-100, 100);
                case nameof(ColorDiff_CIELAB_L):
                case nameof(ColorDiff_CIELAB_a):
                case nameof(ColorDiff_CIELAB_b):
                case nameof(ColorDiff_CIELCH_C):
                case nameof(ColorDiff_CIELCH_H):
                case nameof(ColorDiff_HunterLab_L):
                case nameof(ColorDiff_HunterLab_a):
                case nameof(ColorDiff_HunterLab_b):
                case nameof(ColorDiff_Luv1976_L):
                //                    return new Tuple<double, double>(-100, 100);
                case nameof(ColorDiff_CIEXYZ_X):
                case nameof(ColorDiff_CIEXYZ_Y):
                case nameof(ColorDiff_CIEXYZ_Z):
                    return new Tuple<double, double>(-100, 100);
                case nameof(ColorDiff_CIEYxy_x):
                    return new Tuple<double, double>(-0.8, 0.8);
                case nameof(ColorDiff_CIEYxy_y):
                    return new Tuple<double, double>(-0.9, 0.9);
                case nameof(ColorDiff_Luv1976_u):
                case nameof(ColorDiff_Luv1976_v):
                    return new Tuple<double, double>(-0.6, 0.6);
                case nameof(DensityDiff_C):
                case nameof(DensityDiff_M):
                case nameof(DensityDiff_Y):
                case nameof(DensityDiff_K):
                case nameof(DensityDiff_Spot):
                    return new Tuple<double, double>(-4.0, 4.0);
                default:
                    if (propertyName.StartsWith(nameof(SpectralData_At)))
                        return new Tuple<double, double>(0, 2.00); // 反射率为0～200
                    if (propertyName.StartsWith(nameof(SpectralDiff_At)))
                        return new Tuple<double, double>(-1.0, 1.0);
                    if (propertyName.StartsWith(nameof(SpectralSpotDensity_At)))
                        return new Tuple<double, double>(0.0, 4.0);
                    if (propertyName.StartsWith(nameof(SpectralSpotDensityDiff_At)))
                        return new Tuple<double, double>(-4.0, 4.0);
                    return null;
            }
        }
        #endregion
    }
}
