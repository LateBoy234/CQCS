using System;//InvalidOperationException
using System.ComponentModel;//InvalidEnumArgumentException: System.dll
using ThreeNH.ColorimeterInterface;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.YHComm
{
    internal static class DefineConverters
    {
        /// <summary>
        /// 将仪器中使用的标准光源代号转换为全局使用的
        /// </summary>
        /// <param name="illuminant">仪器中使用的光源代号</param>
        /// <returns></returns>
        internal static StandardIlluminant Uniform(this Illuminant illuminant)
        {
            return (StandardIlluminant)illuminant;
        }

        /// <summary>
        /// 将全局使用的标准光源代号转换回仪器中使用的
        /// </summary>
        /// <param name="illuminant">全局中使用的光源代号</param>
        /// <returns>返回仪器中使用的光源代号</returns>
        internal static Illuminant ToInstrumentForm(this StandardIlluminant illuminant)
        {
            return (Illuminant)illuminant;
        }

        /// <summary>
        /// 将仪器中的标准观察者代号转换为全局使用的
        /// </summary>
        /// <param name="observer">仪器中的观察者代号</param>
        /// <returns>全局使用的观察者代号</returns>
        internal static StandardObserver Uniform(this Observer observer)
        {
            switch (observer)
            {
                case Observer.Degree2: return StandardObserver.CIE1931;
                case Observer.Degree10: return StandardObserver.CIE1964;
                default: throw new InvalidEnumArgumentException(nameof(observer), (int)observer, typeof(StandardObserver));
            }
        }

        /// <summary>
        /// 将全局使用的标准观察者代号转换为仪器中使用的
        /// </summary>
        /// <param name="observer">全局使用的观察者代号</param>
        /// <returns>仪器中的观察者代号</returns>
        internal static Observer ToInstrumentForm(this StandardObserver observer)
        {
            switch (observer)
            {
                case StandardObserver.CIE1931: return Observer.Degree2;
                case StandardObserver.CIE1964: return Observer.Degree10;
                default: throw new InvalidEnumArgumentException(nameof(observer), (int)observer, typeof(StandardObserver));
            }
        }

        internal static MeasurementCaliber Uniform(this Aperture aperture)
        {
            switch (aperture)
            {
                case Aperture.Phi4: return MeasurementCaliber.Phi4;
                case Aperture.Phi8: return MeasurementCaliber.Phi8;
                case Aperture.Phi15: return MeasurementCaliber.Phi15;
                case Aperture.Phi20: return MeasurementCaliber.Phi20;
                default: return MeasurementCaliber.Unknown;
            }
        }

        internal static Aperture ToInstrumentForm(this MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case MeasurementCaliber.Phi4: return Aperture.Phi4;
                case MeasurementCaliber.Phi8: return Aperture.Phi8;
                case MeasurementCaliber.Phi15: return Aperture.Phi15;
                case MeasurementCaliber.Phi20: return Aperture.Phi20;
                default: throw new InvalidOperationException();
            }
        }

        internal static ThreeNH.Optical.ColorSpace Uniform(this ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.CIE_LAB: return ThreeNH.Optical.ColorSpace.CIE_LAB;
                case ColorSpace.HUNTER_LAB: return ThreeNH.Optical.ColorSpace.HUNTER_LAB;
                case ColorSpace.CIE_XYZ: return ThreeNH.Optical.ColorSpace.CIE_XYZ;
                case ColorSpace.sRGB: return ThreeNH.Optical.ColorSpace.sRGB;
                case ColorSpace.CIE_Yxy: return ThreeNH.Optical.ColorSpace.CIE_Yxy;
                case ColorSpace.MUNSELL: return ThreeNH.Optical.ColorSpace.MUNSELL;
                case ColorSpace.CIE_LCH: return ThreeNH.Optical.ColorSpace.CIE_LCH;
                case ColorSpace.BETA_xy: return ThreeNH.Optical.ColorSpace.BETA_xy;
                case ColorSpace.CIE_LUV: return ThreeNH.Optical.ColorSpace.CIE_LUV;
                case ColorSpace.DIN99_LAB: return ThreeNH.Optical.ColorSpace.DIN99_LAB;
                default: return ThreeNH.Optical.ColorSpace.Unknown;
            }
        }

        internal static ColorSpace ToInstrumentForm(this ThreeNH.Optical.ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ThreeNH.Optical.ColorSpace.CIE_LAB: return ColorSpace.CIE_LAB;
                case ThreeNH.Optical.ColorSpace.HUNTER_LAB: return ColorSpace.HUNTER_LAB;
                case ThreeNH.Optical.ColorSpace.CIE_XYZ: return ColorSpace.CIE_XYZ;
                case ThreeNH.Optical.ColorSpace.sRGB: return ColorSpace.sRGB;
                case ThreeNH.Optical.ColorSpace.CIE_Yxy: return ColorSpace.CIE_Yxy;
                case ThreeNH.Optical.ColorSpace.MUNSELL: return ColorSpace.MUNSELL;
                case ThreeNH.Optical.ColorSpace.CIE_LCH: return ColorSpace.CIE_LCH;
                case ThreeNH.Optical.ColorSpace.BETA_xy: return ColorSpace.BETA_xy;
                case ThreeNH.Optical.ColorSpace.CIE_LUV: return ColorSpace.CIE_LUV;
                case ThreeNH.Optical.ColorSpace.DIN99_LAB: return ColorSpace.DIN99_LAB;
                default: throw new InvalidOperationException();
            }
        }

        internal static ThreeNH.Optical.ColorDiffFormula Uniform(this ColorDeltaFormula formula)
        {
            switch (formula)
            {
                case ColorDeltaFormula.CIEDE: return ColorDiffFormula.CIE1976;
                case ColorDeltaFormula.CIEDE94: return ColorDiffFormula.CIE1994;
                case ColorDeltaFormula.CIEDE2000: return ColorDiffFormula.CIE2000;
                case ColorDeltaFormula.DECMC21: return ColorDiffFormula.CMC21;
                case ColorDeltaFormula.DECMC11: return ColorDiffFormula.CMC11;
                case ColorDeltaFormula.DECMC: return ColorDiffFormula.CMC;
                case ColorDeltaFormula.DEuv: return ColorDiffFormula.DEuv;
                case ColorDeltaFormula.DIN_E99: return ColorDiffFormula.DIN99;
                case ColorDeltaFormula.DEHUNTER: return ColorDiffFormula.DHunterE;
                default: return ColorDiffFormula.Unknown;
            }
        }

        internal static ColorDeltaFormula ToInstrumentForm(this ThreeNH.Optical.ColorDiffFormula formula)
        {
            switch (formula)
            {
                case ColorDiffFormula.CIE1976: return ColorDeltaFormula.CIEDE;
                case ColorDiffFormula.CIE1994: return ColorDeltaFormula.CIEDE94;
                case ColorDiffFormula.CIE2000: return ColorDeltaFormula.CIEDE2000;
                case ColorDiffFormula.CMC21: return ColorDeltaFormula.DECMC21;
                case ColorDiffFormula.CMC11: return ColorDeltaFormula.DECMC11;
                case ColorDiffFormula.CMC: return ColorDeltaFormula.DECMC;
                case ColorDiffFormula.DEuv: return ColorDeltaFormula.DEuv;
                case ColorDiffFormula.DIN99: return ColorDeltaFormula.DIN_E99;
                case ColorDiffFormula.DHunterE: return ColorDeltaFormula.DEHUNTER;
                default: throw new InvalidOperationException();
            }
        }

        internal static ThreeNH.Optical.ColorIndex Uniform(this ColorIndex colorIndex)
        {
            switch (colorIndex)
            {
                case ColorIndex.YELLOWNESS: return ThreeNH.Optical.ColorIndex.Yellowness;
                case ColorIndex.WHITENESS: return ThreeNH.Optical.ColorIndex.Whiteness;
                case ColorIndex.STRENGTH: return ThreeNH.Optical.ColorIndex.Strength;
                case ColorIndex.METAMERISM_INDIEX: return ThreeNH.Optical.ColorIndex.MetamerismIndiex;
                case ColorIndex.OPACITY: return ThreeNH.Optical.ColorIndex.Opacity;
                case ColorIndex.HUE_CLASSIFY_555: return ThreeNH.Optical.ColorIndex.Shade555;
                case ColorIndex.STAINING_FASTNESS: return ThreeNH.Optical.ColorIndex.StainingFastness;
                case ColorIndex.COLOR_FASTNESS: return ThreeNH.Optical.ColorIndex.ColorFastness;
                case ColorIndex.GLOSS_8: return ThreeNH.Optical.ColorIndex.Gloss8;
                default: return ThreeNH.Optical.ColorIndex.None;
            }
        }

        internal static ColorIndex ToInstrumentForm(this ThreeNH.Optical.ColorIndex colorIndex)
        {
            switch (colorIndex)
            {
                case ThreeNH.Optical.ColorIndex.Yellowness: return ColorIndex.YELLOWNESS;
                case ThreeNH.Optical.ColorIndex.Whiteness: return ColorIndex.WHITENESS;
                case ThreeNH.Optical.ColorIndex.Strength: return ColorIndex.STRENGTH;
                case ThreeNH.Optical.ColorIndex.MetamerismIndiex: return ColorIndex.METAMERISM_INDIEX;
                case ThreeNH.Optical.ColorIndex.Opacity: return ColorIndex.OPACITY;
                case ThreeNH.Optical.ColorIndex.Shade555: return ColorIndex.HUE_CLASSIFY_555;
                case ThreeNH.Optical.ColorIndex.StainingFastness: return ColorIndex.STAINING_FASTNESS;
                case ThreeNH.Optical.ColorIndex.ColorFastness: return ColorIndex.COLOR_FASTNESS;
                case ThreeNH.Optical.ColorIndex.Gloss8: return ColorIndex.GLOSS_8;
                default: throw new InvalidOperationException();
            }
        }

        internal static InstrumentLanguage Uniform(this ThreeNH.YHComm.Language language)
        {
            switch (language)
            {
                case Language.English:
                    return InstrumentLanguage.English;
                case Language.SimplifiedChinese:
                    return InstrumentLanguage.SimplifiedChinese;
                case Language.TraditionalChinese:
                    return InstrumentLanguage.TraditionalChinese;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, null);
            }
        }

        internal static ThreeNH.YHComm.Language ToInstrument(this InstrumentLanguage language)
        {
            switch (language)
            {
                case InstrumentLanguage.English:
                    return Language.English;
                case InstrumentLanguage.SimplifiedChinese:
                    return Language.SimplifiedChinese;
                case InstrumentLanguage.TraditionalChinese:
                    return Language.TraditionalChinese;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, null);
            }
        }
    }
}
