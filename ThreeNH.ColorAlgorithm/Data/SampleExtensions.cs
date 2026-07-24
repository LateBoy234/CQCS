using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public static class SampleExtensions
    {
        //static Dictionary<int, Formula> Formulas = new Dictionary<int, Formula>();
        public static string AbsoLilin = "Absolute",
                             MinuLilin = "MinusPaper";
        public static string PaSetLilin { get; set; } = null;

        internal static KeyValuePair<StandardIlluminant,StandardObserver> GetSetting(IDictionary<string, object> settings)
        {
            IColorSetting colorSetting = settings.ContainsKey(SamplePropertySettingKeys.ColorSetting)
                ? (IColorSetting)(settings[SamplePropertySettingKeys.ColorSetting])
                : null;

            StandardIlluminant illuminant = settings.ContainsKey(SamplePropertySettingKeys.Illuminant)
                ? (StandardIlluminant)settings[SamplePropertySettingKeys.Illuminant]
                : colorSetting?.Illuminant ??
                  throw new ArgumentException("The 'Illuminant' is not specified", nameof(settings));

            StandardObserver observer = settings.ContainsKey(SamplePropertySettingKeys.Observer)
                ? (StandardObserver)settings[SamplePropertySettingKeys.Observer]
                : colorSetting?.Observer ?? throw new ArgumentException("The 'Observer' is not specified", nameof(settings));

            return new KeyValuePair<StandardIlluminant, StandardObserver>(illuminant, observer);
        }

        public static ISpectralData GetSpectralData(this ISample sample, string channel)//s7_M s6_L
        {
            return sample?.GetColorData(channel)?.SpectralData;
        }

        public static ISpectralData GetSpectralData(ISample sample, IColorSetting colorSetting)
        {
            return sample?.GetColorData(colorSetting.Channel)?.SpectralData;
        }

        internal static ISpectralData GetSpectralData(this ISample sample, IDictionary<string, object> settings)
        {
            if (settings.ContainsKey(SamplePropertySettingKeys.Channel))
            {
                return sample.GetSpectralData((string)settings[SamplePropertySettingKeys.Channel]);
            }

            if (settings.ContainsKey(SamplePropertySettingKeys.ColorSetting))
            {
                return sample.GetSpectralData(((IColorSetting)settings[SamplePropertySettingKeys.ColorSetting]).Channel);
            }

            throw new ArgumentException("Cannot find 'Channel' setting");
        }

        public static IDensityData GetDensityData(
            this ISample sample,
            IDensitySetting densitySetting,
            DensityColor densityColor = DensityColor.Auto,
            bool onlyAbsolute = false)
        {
            return GetDensityData(sample, densitySetting.Channel, densityColor, densitySetting.DensityStatus, onlyAbsolute);
        }

        public static IDensityData GetDensityData(
            this ISample sample,
            ISample paperWhite,
            IDensitySetting densitySetting,
            DensityColor densityColor = DensityColor.Auto)
        {
            return GetDensityData(sample, paperWhite, densitySetting.Channel, densityColor, densitySetting.DensityStatus);
        }

        /// <summary>
        /// 获取密度数据
        /// </summary>
        /// <param name="sample">要获取其密度数据的样品</param>
        /// <param name="channel">要获取的通道</param>
        /// <param name="densityColor">密度色，如果为自动将返回自动密度</param>
        /// <param name="densityStatus">要获取的密度状态</param>
        /// <param name="onlyAbsolute">是否只获取绝对密度</param>
        /// <returns>如果可以通过ISample的GetDensityData获取相应密度，优先返回该函数获取的密度
        /// 当无法使用GetDensityData获取时会获取相同通道的颜色数据，如果颜色数据有光谱，将使用光谱计算
        /// 相应密度，否则返回null</returns>
        public static IDensityData GetDensityData(
            this ISample sample,
            string channel,
            DensityColor densityColor,
            DensityStatus densityStatus,
            bool onlyAbsolute = false)
        {
            if (sample == null)
            {
                return null;
            }

            if (densityColor == DensityColor.Spot)
                return GetSpotDensityData(sample, channel, null, onlyAbsolute);

            var density = sample.GetDensityData(channel);
            if (density != null && density.DensityStatus == densityStatus &&
                (onlyAbsolute == false || density.IsAbsolute) &&
                (densityColor == DensityColor.Auto || densityColor == density.DensityColor))
                return density;

            var spectra = sample.GetColorData(channel)?.SpectralData;
            if (spectra != null)
            {
                return new DensityData(spectra, densityColor, densityStatus);
            }

            return null;
        }

        /// <summary>
        /// 获取密度数据
        /// </summary>
        /// <param name="sample">要获取其密度数据的样品</param>
        /// <param name="paperWhite">纸张白</param>
        /// <param name="channel">要获取的通道</param>
        /// <param name="densityColor">密度色，如果为自动将返回自动密度</param>
        /// <param name="densityStatus">要获取的密度状态</param>
        /// <returns>如果可以通过ISample的GetDensityData获取相应密度，如果密度存在且是绝对密度将使用该密度，
        /// 使用该密度减去纸张白密度，当无法使用GetDensityData获取时会获取相同通道的颜色数据，如果颜色数据有光谱，将使用光谱计算
        /// 相应密度。另外，如果果纸张白不存在与sample相应的密度返回null</returns>
        /// <remarks>注意：该函数返回逻辑与其重载的不带纸张白的细微差别</remarks>
        public static IDensityData GetDensityData(
            this ISample sample,
            ISample paperWhite,
            string channel,
            DensityColor densityColor,
            DensityStatus densityStatus)
        {
            if (densityColor == DensityColor.Spot)
            {
                return GetSpotDensityData(sample, paperWhite, channel);
            }
            var densityData = sample.GetDensityData(channel, densityColor, densityStatus);
            if (densityData == null) return null;

            var paperDensityData = paperWhite.GetDensityData(channel, densityData.DensityColor, densityStatus);
            if (paperDensityData == null) return null;

            return new DensityData
            {
                Density = densityData.Density - paperDensityData.Density,
                DensityColor = densityData.DensityColor,
                DensityStatus = densityStatus,
                IsAbsolute = false
            };
        }

        public static IDensityData GetSpotDensityData(
            this ISample sample,
            string channel,
            int? wavelength = null,
            bool onlyAbsolute = false)
        {
            var density = sample.GetDensityData(channel);
            if (density != null && density.DensityColor == DensityColor.Spot &&
                (onlyAbsolute == false || density.IsAbsolute) &&
                (wavelength == null || wavelength == density.SpotWavelength))
            {
                return density;
            }

            var spectra = sample.GetColorData(channel)?.SpectralData;
            if (spectra != null)
            {
                if (wavelength == null)
                    return new DensityData(spectra, DensityColor.Spot);
                return new DensityData(DensityColor.Spot,
                    (double)spectra.CalculateSpecificSpotDensity(wavelength.Value),
                    wavelength.Value);
            }

            return null;
        }

        public static IDensityData GetSpotDensityData(
            this ISample sample,
            ISample paperWhite,
            string channel,
            int? wavelength = null)
        {
            var densityData = sample.GetSpotDensityData(channel, wavelength);
            if (densityData == null) return null;
            var paperDensityData = paperWhite.GetSpotDensityData(channel, densityData.SpotWavelength);
            if (paperDensityData == null) return null;

            return new DensityData
            {
                Density = densityData.Density - paperDensityData.Density,
                DensityColor = DensityColor.Spot,
                SpotWavelength = densityData.SpotWavelength,
                IsAbsolute = false
            };
        }

        public static CIEXYZ GetXYZ(
            this ISample sample,
            string channel,
            StandardIlluminant illuminant,
            StandardObserver observer)
        {
            var clr = sample?.GetColorData(channel);
            if (clr != null && clr.Illuminant == illuminant && clr.Observer == observer)
            {
                return clr.Xyz;
            }

            var spectra = sample.GetSpectralData(channel);
            return spectra.ToCIEXYZ(illuminant, observer);
        }

        public static CIEXYZ GetXYZ(
            this ISample sample,
            IColorSetting colorSetting)
        {
            return sample.GetXYZ(colorSetting.Channel, colorSetting.Illuminant, colorSetting.Observer);
        }

        /// <summary>
        /// 通过配置获取XYZ值
        /// 如果配置包含"ColorSetting"，或包含
        /// "Channel", "Illuminant", "Observer"三项
        /// 如果同时包含则优先使用三项直接设置，找不到才使用ColorSetting中的设置
        /// </summary>
        internal static CIEXYZ GetXYZ(
            this ISample sample,
            IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);

            return sample.GetXYZ(channel, illuminant, observer);
        }


        public static CIELAB GetLab(
            this ISample sample,
            string channel,
            StandardIlluminant illuminant,
            StandardObserver observer)//s4_L
        {
            var clr = sample?.GetColorData(channel);
            if (clr != null && clr.Illuminant == illuminant && clr.Observer == observer)
            {
                return clr.Lab;
            }

            var spectra = sample.GetSpectralData(channel);
            return spectra.ToCIELAB(illuminant, observer);
        }

        public static CIELAB GetLab(
            this ISample sample,
            IColorSetting colorSetting)
        {
            return GetLab(sample, colorSetting.Channel, colorSetting.Illuminant, colorSetting.Observer);
        }

        internal static CIELAB GetLab(
            this ISample sample,
            IDictionary<string, object> settings)//s2_L
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);
            return sample.GetLab(channel, illuminant, observer);
        }

        public static sRGB GetPseudoColor(
            this ISample sample,
            string channel,
            StandardIlluminant illuminant,
            StandardObserver observer)
        {
            var lab = sample.GetLab(channel, illuminant, observer);
            return lab.TosRGB();
        }


        public static sRGB GetPseudoColor(
            this ISample sample,

            IColorSetting colorSetting)
        {
            return sample.GetPseudoColor(colorSetting.Channel, colorSetting.Illuminant, colorSetting.Observer);
        }

        internal static sRGB GetPseudoColor(
            this ISample sample,
            IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);
            return sample.GetPseudoColor(channel, illuminant, observer);
        }

        public static sRGB GetRgb(this ISample sample,
            string channel,
            StandardIlluminant illuminant,
            StandardObserver observer,
            RgbColorSystem rgbColorSystem,
            double gamma = 1.0)
        {
            if (sample == null) return null;

            var xyz = sample.GetXYZ(channel, illuminant, observer);
            if (xyz != null && rgbColorSystem != null)
            {
                var tuple = XYZtoRGBConversion.XYZtoNormalizedRGB(rgbColorSystem, xyz.X, xyz.Y, xyz.Z, gamma);
                return new sRGB((byte)tuple.Item1, (byte)tuple.Item2, (byte)tuple.Item3);
            }

            return GetPseudoColor(sample, channel, illuminant, observer);
        }

        internal static sRGB GetRgb(
            this ISample sample,
            IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);
            return sample.GetRgb(channel, illuminant, observer,
                SampleProperties.GetRgbColorSystem(settings),
                SampleProperties.GetRgbGamma(settings));
        }


        // Hunter Lab
        public static HunterLab GetHunterLab(this ISample sample, string channel, StandardIlluminant illuminant, StandardObserver observer)
        {
            var xyz = sample.GetXYZ(channel, illuminant, observer);
            return xyz.ToHunterLab();
        }

        public static HunterLab GetHunterLab(this ISample sample, IColorSetting colorSetting)
        {
            return sample.GetHunterLab(colorSetting.Channel, colorSetting.Illuminant, colorSetting.Observer);
        }

        internal static HunterLab GetHunterLab(this ISample sample, IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);
            return sample.GetHunterLab(channel, illuminant, observer);
        }


        // CIE L*u*v*
        public static CIELUV1976 GetLuv1976(this ISample sample, string channel, StandardIlluminant illuminant, StandardObserver observer)
        {
            var xyz = sample.GetXYZ(channel, illuminant, observer);
            return xyz.ToCIELUV1976();
        }

        public static CIELUV1976 GetLuv1976(this ISample sample, IColorSetting colorSetting)
        {
            var xyz = sample.GetXYZ(colorSetting);
            return xyz.ToCIELUV1976();
        }

        internal static CIELUV1976 GetLuv1976(this ISample sample, IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);
            return sample.GetLuv1976(channel, illuminant, observer);
        }

        public static CIEDE1976 GetDe1976(this ISample sample, ISample target, string channel,
            StandardIlluminant illuminant, StandardObserver observer)
        {
            var labSample = sample.GetLab(channel, illuminant, observer);
            var labTarget = target.GetLab(channel, illuminant, observer);
            if (labSample != null && labTarget != null)
                return new CIEDE1976(labSample, labTarget);
            return null;
        }

        public static CIEDE1976 GetDe1976(this ISample sample, ISample target, IColorSetting colorSetting)
        {
            return GetDe1976(sample, target, colorSetting.Channel, colorSetting.Illuminant, colorSetting.Observer);
        }

        internal static CIEDE1976 GetDe1976(this ISample sample, ISample target, IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);
            return GetDe1976(sample, target, channel, illuminant, observer);
        }

        public static CIEDE1994 GetDe1994(this ISample sample, ISample target, string channel,
            StandardIlluminant illuminant, StandardObserver observer, float KL, float KC, float KH)
        {
            var labSample = sample.GetLab(channel, illuminant, observer);
            var labTarget = target.GetLab(channel, illuminant, observer);
            if (labSample != null && labTarget != null)
                return new CIEDE1994(labSample, labTarget, KL, KC, KH);
            return null;
        }

        public static CIEDE1994 GetDe1994(this ISample sample, ISample target, IColorSetting colorSetting,
            double KL, double KC, double KH)
        {
            return GetDe1994(sample, target, colorSetting.Channel, colorSetting.Illuminant, colorSetting.Observer,
                (float)KL, (float)KC, (float)KH);
        }

        internal static CIEDE1994 GetDe1994(this ISample sample, ISample target, IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);

            SampleProperties.GetDE94Factors(settings, out var KL, out var KC, out var KH);

            return GetDe1994(sample, target, channel, illuminant, observer,
                (float)KL, (float)KC, (float)KH);
        }

        public static CIEDE2000 GetDe2000(this ISample sample, ISample target, string channel,
            StandardIlluminant illuminant, StandardObserver observer, float KL, float KC, float KH)
        {
            var labSample = sample.GetLab(channel, illuminant, observer);
            var labTarget = target.GetLab(channel, illuminant, observer);
            if (labSample != null && labTarget != null)
                return new CIEDE2000(labSample, labTarget, KL, KC, KH);
            return null;
        }

        public static CIEDE2000 GetDe2000(this ISample sample, ISample target, IColorSetting colorSetting,
            float KL, float KC, float KH)
        {
            return GetDe2000(sample, target, colorSetting.Channel, colorSetting.Illuminant, colorSetting.Observer,
                KL, KC, KH);
        }

        internal static CIEDE2000 GetDe2000(this ISample sample, ISample target, IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);
            SampleProperties.GetDE00Factors(settings, out var KL, out var KC, out var KH);
            return GetDe2000(sample, target, channel, illuminant, observer, (float)KL, (float)KC, (float)KH);
        }

        public static CMCDE GetCmcDe(this ISample sample, ISample target, string channel,
            StandardIlluminant illuminant, StandardObserver observer, double KL, double KC)
        {
            var labSample = sample.GetLab(channel, illuminant, observer);
            var labTarget = target.GetLab(channel, illuminant, observer);
            if (labSample != null && labTarget != null)
                return new CMCDE(labSample, labTarget, (float)KL, (float)KC);
            return null;
        }

        public static CMCDE GetCmcDe(this ISample sample, ISample target, IColorSetting colorSetting,
            double KL, double KC)
        {
            return GetCmcDe(sample, target, colorSetting.Channel, colorSetting.Illuminant, colorSetting.Observer,
                KL, KC);
        }

        internal static CMCDE GetCmcDe(this ISample sample, ISample target, IDictionary<string, object> settings)
        {
            SampleProperties.GetColorSetting(settings, out var channel, out var illuminant, out var observer);
            SampleProperties.GetCMCDEFactors(settings, out var KL, out var KC);
            return GetCmcDe(sample, target, channel, illuminant, observer, KL, KC);
        }

        public static double? GetSpotDensity(this ISample sample, string channel, int wavelength)
        {
            var spectra = sample.GetSpectralData(channel);
            return spectra.CalculateSpecificSpotDensity(wavelength);
        }

        public static double? GetSpotDensity(this ISample sample, IDensitySetting densitySetting, int wavelength)
        {
            var spectra = sample.GetSpectralData(densitySetting.Channel);
            return spectra.CalculateSpecificSpotDensity(wavelength);
        }

        internal static double? GetSpotDensity(this ISample sample, IDictionary<string, object> settings, int wavelength)
        {
            return GetSpotDensity(sample, SampleProperties.GetDensityChannel(settings), wavelength);
        }

        public static Tuple<int, double> GetSpotDensity(this ISample sample, string channel)
        {
            var spectra = sample.GetSpectralData(channel);
            if (spectra == null)
                return null;

            var density = spectra.CalculateSpotDensity(out int wavelength);
            return new Tuple<int, double>(wavelength, (double)density);
        }

        public static Tuple<int, double> GetSpotDensity(this ISample sample, IDensitySetting densitySetting)
        {
            return GetSpotDensity(sample, densitySetting.Channel);
        }

        internal static Tuple<int, double> GetSpotDensity(this ISample sample, IDictionary<string, object> settings)
        {
            return GetSpotDensity(sample, SampleProperties.GetDensityChannel(settings));
        }

        public static Tuple<int, double> GetSpotDensity(this ISample sample, ISample whiteBase, string channel)
        {
            var tuple = GetSpotDensity(sample, channel);
            if (whiteBase == null || tuple == null)
                return tuple;

            double? dw = GetSpotDensity(whiteBase, channel, tuple.Item1);
            if (dw != null)
            {
                return new Tuple<int, double>(tuple.Item1, tuple.Item2 - dw.Value);
            }

            return null;
        }

        public static Tuple<int, double> GetSpotDensity(this ISample sample, ISample whiteBase, IDensitySetting densitySetting)
        {
            return GetSpotDensity(sample, whiteBase, densitySetting.Channel);
        }

        internal static Tuple<int, double> GetSpotDensity(this ISample sample, ISample whiteBase, IDictionary<string, object> settings)
        {
            return GetSpotDensity(sample, whiteBase, SampleProperties.GetDensityChannel(settings));
        }

        public static double? GetSpotDensity(this ISample sample, ISample whiteBase, string channel, int wavelength)
        {
            var density = GetSpotDensity(sample, channel, wavelength);
            if (whiteBase == null || density == null)
                return density;

            double? dw = GetSpotDensity(whiteBase, channel, wavelength);
            if (dw != null)
            {
                return density.Value - dw.Value;
            }

            return null;
        }

        public static double? GetSpotDensity(this ISample sample, ISample whiteBase, IDensitySetting densitySetting, int wavelength)
        {
            return GetSpotDensity(sample, whiteBase, densitySetting.Channel, wavelength);
        }

        internal static double? GetSpotDensity(this ISample sample, ISample whiteBase, IDictionary<string, object> settings, int wavelength)
        {
            return GetSpotDensity(sample, whiteBase, SampleProperties.GetDensityChannel(settings), wavelength);
        }



        public static Tuple<DensityColor, double> GetAutoDensity(this ISample sample, string channel,
            DensityStatus densityStatus)
        {
            var spectra = sample.GetSpectralData(channel);
            if (spectra == null)
                return null;

            var density = spectra.CalculateDensity(densityStatus, DensityColor.Auto, out var autoColor, out float cmyk, out float grayness, out float hue_error);
            return new Tuple<DensityColor, double>(autoColor, density);
        }


        public static Tuple<DensityColor, double> GetAutoDensity(this ISample sample, IDensitySetting densitySetting)
        {
            return sample.GetAutoDensity(densitySetting.Channel, densitySetting.DensityStatus);
        }

        internal static Tuple<DensityColor, double> GetAutoDensity(this ISample sample,
            IDictionary<string, object> settings)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);
            return GetAutoDensity(sample, channel, status);
        }


        public static Tuple<DensityColor, double> GetAutoDensity(this ISample sample, ISample whiteBase,
            string channel, DensityStatus densityStatus)
        {
            var tuple = GetAutoDensity(sample, channel, densityStatus);
            if (whiteBase == null || tuple == null) return tuple;

            var dw = GetDensity(whiteBase, channel, densityStatus, tuple.Item1);
            if (dw == null) return null;

            return new Tuple<DensityColor, double>(tuple.Item1, tuple.Item2 - dw.Value);
        }

        public static Tuple<DensityColor, double> GetAutoDensity(this ISample sample, ISample whiteBase,
            IDensitySetting densitySetting)
        {
            return GetAutoDensity(sample, whiteBase, densitySetting.Channel, densitySetting.DensityStatus);
        }

        internal static Tuple<DensityColor, double> GetAutoDensity(this ISample sample, ISample whiteBase,
            IDictionary<string, object> settings)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);
            return GetAutoDensity(sample, whiteBase, channel, status);
        }

        public static double? GetDensity(this ISample sample, string channel, DensityStatus densityStatus, DensityColor densityColor)//s6_M
        {
            var spectra = sample.GetSpectralData(channel);
            if (spectra == null)
                return null;

            if (densityColor == DensityColor.Spot)
            {
                return spectra.CalculateSpotDensity(out int wavelength);
            }
            else
            {
                return spectra.CalculateDensity(densityStatus, densityColor);
            }
        }

        public static double? GetDensity(this ISample sample, IDensitySetting densitySetting, DensityColor densityColor)
        {
            return GetDensity(sample, densitySetting.Channel, densitySetting.DensityStatus, densityColor);
        }

        internal static double? GetDensity(this ISample sample, IDictionary<string, object> settings,
            DensityColor densityColor)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);
            return GetDensity(sample, channel, status, densityColor);
        }

        public static double? GetDensity(this ISample sample, ISample whiteBase, string channel, DensityStatus densityStatus, DensityColor densityColor)//s5_M
        {//se diao wu cha huidu  + listView
            if (densityColor == DensityColor.Auto)
            {
                return GetAutoDensity(sample, whiteBase, channel, densityStatus)?.Item2;
            }
            if (densityColor == DensityColor.Spot)
            {
                return GetSpotDensity(sample, whiteBase, channel)?.Item2;
            }

            var d = GetDensity(sample, channel, densityStatus, densityColor);
            //if (d == null || whiteBase == null) return d;//190923 lilin change zhong code to comment

            if (PaSetLilin == AbsoLilin)//190923lilin add following 5 lines添加是否绝对白或者减纸张的判断
            {
                return d;
            }
            else if (PaSetLilin == MinuLilin)
            {
                var dw = GetDensity(whiteBase, channel, densityStatus, densityColor);
                if (dw == null) return null;// add info to show no whitebase

                return d.Value - dw.Value;
            }
            else//190923lilin add else 
            {
                return null;
            }

        }

        public static double? GetDensity(this ISample sample, ISample whiteBase, IDensitySetting densitySetting, DensityColor densityColor)
        {
            return GetDensity(sample, whiteBase, densitySetting.Channel, densitySetting.DensityStatus, densityColor);
        }

        internal static double? GetDensity(this ISample sample, ISample whiteBase, IDictionary<string, object> settings,
            DensityColor densityColor)//s2_M
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);

            GetWhitePaperSetLilin(settings);

            return GetDensity(sample, whiteBase, channel, status, densityColor);
        }

        public static double[] GetCMYK(this ISample sample, string channel, DensityStatus densityStatus)
        {
            var spectra = GetSpectralData(sample, channel);
            if (spectra != null)
            {
                return spectra.CalculateCmyk(densityStatus);
            }

            return null;
        }

        public static double[] GetCMYK(this ISample sample, IDensitySetting densitySetting)
        {
            return GetCMYK(sample, densitySetting.Channel, densitySetting.DensityStatus);
        }

        internal static double[] GetCMYK(this ISample sample, IDictionary<string, object> settings)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);
            return GetCMYK(sample, channel, status);
        }

        public static double[] GetCMYK(this ISample sample, ISample whiteBase, string channel, DensityStatus densityStatus)
        {
            var cmyk = GetCMYK(sample, channel, densityStatus);
            /*
            if (cmyk == null || whiteBase == null) return null;

            var whiteCmyk = GetCMYK(whiteBase, channel, densityStatus);
            if (whiteCmyk == null) return null;

            return new[]
            {
                cmyk[0] - whiteCmyk[0],
                cmyk[1] - whiteCmyk[1],
                cmyk[2] - whiteCmyk[2],
                cmyk[3] - whiteCmyk[3]
            };
            */ // 20190923lilin change to comment
            //20190923lilin add

            if (cmyk == null)//列表视图：色调误差 灰度才会进入此函数
            {
                return null;
            }
            else
            {
                if (whiteBase == null || PaSetLilin == AbsoLilin)
                {
                    return new[]
                    {
                        cmyk[0],
                        cmyk[1],
                        cmyk[2],
                        cmyk[3]
                    };
                }
                else
                {
                    var whiteCmyk = GetCMYK(whiteBase, channel, densityStatus);
                    if (whiteCmyk == null) return null;

                    return new[]
                    {
                        cmyk[0] - whiteCmyk[0],
                        cmyk[1] - whiteCmyk[1],
                        cmyk[2] - whiteCmyk[2],
                        cmyk[3] - whiteCmyk[3]
                    };
                }
            }

            //20190923lilin end

        }

        public static double[] GetCMYK(this ISample sample, ISample whiteBase, IDensitySetting densitySetting)
        {
            return GetCMYK(sample, whiteBase, densitySetting.Channel, densitySetting.DensityStatus);
        }

        internal static double[] GetCMYK(this ISample sample, ISample whiteBase, IDictionary<string, object> settings)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);
            return GetCMYK(sample, whiteBase, channel, status);
        }

        public static double? GetHueError(this ISample sample, string channel, DensityStatus densityStatus)
        {
            var cmyk = GetCMYK(sample, channel, densityStatus);

            return cmyk != null
                ? cmyk.CalculateHueError() * 0.01
                : (double?)null;
        }

        public static double? GetHueError(this ISample sample, IDensitySetting densitySetting)
        {
            return GetHueError(sample, densitySetting.Channel, densitySetting.DensityStatus);
        }

        internal static double? GetHueError(this ISample sample, IDictionary<string, object> settings)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);
            return GetHueError(sample, channel, status);
        }

        public static double? GetHueError(this ISample sample, ISample whiteBase, string channel, DensityStatus densityStatus)
        {
            var cmyk = GetCMYK(sample, whiteBase, channel, densityStatus);

            return cmyk != null
                ? cmyk.CalculateHueError() * 0.01
                : (double?)null;
        }

        public static double? GetHueError(this ISample sample, ISample whiteBase, IDensitySetting densitySetting)
        {
            return GetHueError(sample, whiteBase, densitySetting.Channel, densitySetting.DensityStatus);
        }

        internal static double? GetHueError(this ISample sample, ISample whiteBase, IDictionary<string, object> settings)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);
            return GetHueError(sample, whiteBase, channel, status);
        }

        public static double? GetGrayness(this ISample sample, string channel,
            DensityStatus densityStatus)
        {
            var cmyk = GetCMYK(sample, channel, densityStatus);
            if (cmyk != null)
            {
                return cmyk.CalculateGrayness() * 0.01; // 乘以0.01将百分比转换为小数
            }

            return null;
        }

        public static double? GetGrayness(this ISample sample, IDensitySetting densitySetting)
        {
            return GetGrayness(sample, densitySetting.Channel, densitySetting.DensityStatus);
        }

        internal static double? GetGrayness(this ISample sample, IDictionary<string, object> settings)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);
            return GetGrayness(sample, channel, status);
        }

        public static double? GetGrayness(this ISample sample, ISample whiteBase, string channel,
            DensityStatus densityStatus)
        {
            var cmyk = GetCMYK(sample, whiteBase, channel, densityStatus);
            if (cmyk != null)
            {
                return cmyk.CalculateGrayness() * 0.01; // 乘以0.01将百分比转换为小数
            }

            return null;
        }

        public static double? GetGrayness(this ISample sample, ISample whiteBase, IDensitySetting densitySetting)
        {
            return GetGrayness(sample, whiteBase, densitySetting.Channel, densitySetting.DensityStatus);
        }

        internal static double? GetGrayness(this ISample sample, ISample whiteBase, IDictionary<string, object> settings)
        {
            SampleProperties.GetDensitySetting(settings, out var channel, out var status);

            GetWhitePaperSetLilin(settings);

            return GetGrayness(sample, whiteBase, channel, status);
        }

        public static void GetWhitePaperSetLilin(IDensitySetting densitySetting)
        {

        }
        public static string GetWhitePaperSetLilin(IDictionary<string, object> settings)//s4_M
        {
            Object obj = null;
            settings?.TryGetValue("DensitySetting", out obj);
            var info = obj?.GetType().GetProperty("DensityWhiteBase")?.GetValue(obj).ToString();
            PaSetLilin = info;
            return info;
        }
    }
}
