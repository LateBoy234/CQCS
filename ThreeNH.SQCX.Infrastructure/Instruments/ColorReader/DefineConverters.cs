using System;//InvalidOperationException
using System.ComponentModel;//InvalidEnumArgumentException: System.dll
using ThreeNH.ColorimeterInterface;
using ThreeNH.Optical;

namespace ThreeNH.SQCX.Infrastructure.Instruments.ColorReader
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

        internal static MeasurementCaliber Uniform(this Caliber caliber)
        {
            switch (caliber)
            {
                case Caliber.Phi4: return MeasurementCaliber.Phi4;
                case Caliber.Phi8: return MeasurementCaliber.Phi8;
                //                case Caliber.Phi15: return MeasurementCaliber.Phi15;
                //                case Caliber.Phi25: return MeasurementCaliber.Phi1in;
                default: return MeasurementCaliber.Unknown;
            }
        }

        internal static Caliber ToInstrumentForm(this MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case MeasurementCaliber.Phi4: return Caliber.Phi4;
                case MeasurementCaliber.Phi8: return Caliber.Phi8;
                case MeasurementCaliber.Unknown: return Caliber.Auto;
                //                case MeasurementCaliber.Phi15: return Caliber.Phi15;
                //                case MeasurementCaliber.Phi1in: return Caliber.Phi25;
                default: throw new InvalidOperationException();
            }
        }

        internal static SpecularComponent Uniform(this ScMode sc)
        {
            switch (sc)
            {
                case ScMode.SCI: return SpecularComponent.SCI;
                case ScMode.SCE: return SpecularComponent.SCE;
                case ScMode.SCIAndSCE: return SpecularComponent.SCIAndSCE;
                default: throw new InvalidEnumArgumentException(nameof(sc), (int)sc, typeof(ScMode));
            }
        }

        internal static ScMode ToInstrumentForm(this SpecularComponent sc)
        {
            switch (sc)
            {
                case SpecularComponent.SCI: return ScMode.SCI;
                case SpecularComponent.SCE: return ScMode.SCE;
                case SpecularComponent.SCIAndSCE: return ScMode.SCIAndSCE;
                default: //return ScMode.SCI;
                    throw new InvalidOperationException();
            }
        }

        internal static UVFlag Uniform(this UvMode uv)
        {
            switch (uv)
            {
                case UvMode.NoCut: return UVFlag.NoCut;
                case UvMode.Uv400: return UVFlag.Cut400nm;
                case UvMode.Uv420: return UVFlag.Cut420nm;
                default: return UVFlag.None;
            }
        }

        internal static UvMode ToInstrumentForm(this UVFlag uv)
        {
            switch (uv)
            {
                case UVFlag.None:
                case UVFlag.NoCut:
                    return UvMode.NoCut;
                case UVFlag.Cut400nm:
                    return UvMode.Uv400;
                case UVFlag.Cut420nm:
                    return UvMode.Uv420;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal static LensPosition Uniform(this LensPos lens)
        {
            switch (lens)
            {
                case LensPos.Pos4:
                    return LensPosition.Pos4;
                case LensPos.Pos8:
                    return LensPosition.Pos8;
                case LensPos.Pos15:
                    return LensPosition.Pos15;
                case LensPos.Pos25:
                    return LensPosition.Pos1in;
                default:
                    return LensPosition.Unknown;
            }
        }

        internal static LensPos ToInstrumentForm(this LensPosition lens)
        {
            switch (lens)
            {
                case LensPosition.Pos4: return LensPos.Pos4;
                case LensPosition.Pos8: return LensPos.Pos8;
                case LensPosition.Pos15: return LensPos.Pos15;
                case LensPosition.Pos1in: return LensPos.Pos25;
                default: //return LensPos.Pos8;
                    throw new InvalidOperationException();
            }
        }

        internal static LensSwitchMode Uniform(this LensMode mode)
        {
            if (mode == LensMode.Auto)
            {
                return LensSwitchMode.Auto;
            }

            return LensSwitchMode.Manual;
        }

        internal static LensMode ToInstrumentForm(this LensSwitchMode mode)
        {
            if (mode == LensSwitchMode.Auto)
            {
                return LensMode.Auto;
            }

            return LensMode.Manual;
        }

        internal static Optical.ColorSpace Uniform(this ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.CIE_LAB: return Optical.ColorSpace.CIE_LAB;
                case ColorSpace.HUNTER_LAB: return Optical.ColorSpace.HUNTER_LAB;
                case ColorSpace.CIE_XYZ: return Optical.ColorSpace.CIE_XYZ;
                case ColorSpace.sRGB: return Optical.ColorSpace.sRGB;
                case ColorSpace.CIE_Yxy: return Optical.ColorSpace.CIE_Yxy;
                case ColorSpace.MUNSELL: return Optical.ColorSpace.MUNSELL;
                case ColorSpace.CIE_LCH: return Optical.ColorSpace.CIE_LCH;
                case ColorSpace.BETA_xy: return Optical.ColorSpace.BETA_xy;
                case ColorSpace.CIE_LUV: return Optical.ColorSpace.CIE_LUV;
                case ColorSpace.DIN99_LAB: return Optical.ColorSpace.DIN99_LAB;
                default: return Optical.ColorSpace.Unknown;
            }
        }

        internal static ColorSpace ToInstrumentForm(this Optical.ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case Optical.ColorSpace.CIE_LAB: return ColorSpace.CIE_LAB;
                case Optical.ColorSpace.HUNTER_LAB: return ColorSpace.HUNTER_LAB;
                case Optical.ColorSpace.CIE_XYZ: return ColorSpace.CIE_XYZ;
                case Optical.ColorSpace.sRGB: return ColorSpace.sRGB;
                case Optical.ColorSpace.CIE_Yxy: return ColorSpace.CIE_Yxy;
                case Optical.ColorSpace.MUNSELL: return ColorSpace.MUNSELL;
                case Optical.ColorSpace.CIE_LCH: return ColorSpace.CIE_LCH;
                case Optical.ColorSpace.BETA_xy: return ColorSpace.BETA_xy;
                case Optical.ColorSpace.CIE_LUV: return ColorSpace.CIE_LUV;
                case Optical.ColorSpace.DIN99_LAB: return ColorSpace.DIN99_LAB;
                default: throw new InvalidOperationException();
            }
        }

        internal static Optical.ColorDiffFormula Uniform(this ColorDeltaFormula formula)
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

        internal static ColorDeltaFormula ToInstrumentForm(this Optical.ColorDiffFormula formula)
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

        internal static Optical.ColorIndex Uniform(this ColorIndex colorIndex)
        {
            switch (colorIndex)
            {
                case ColorIndex.YELLOWNESS: return Optical.ColorIndex.Yellowness;
                case ColorIndex.WHITENESS: return Optical.ColorIndex.Whiteness;
                case ColorIndex.STRENGTH: return Optical.ColorIndex.Strength;
                case ColorIndex.METAMERISM_INDIEX: return Optical.ColorIndex.MetamerismIndiex;
                case ColorIndex.OPACITY: return Optical.ColorIndex.Opacity;
                case ColorIndex.HUE_CLASSIFY_555: return Optical.ColorIndex.Shade555;
                case ColorIndex.STAINING_FASTNESS: return Optical.ColorIndex.StainingFastness;
                case ColorIndex.COLOR_FASTNESS: return Optical.ColorIndex.ColorFastness;
                case ColorIndex.GLOSS_8: return Optical.ColorIndex.Gloss8;
                default: return Optical.ColorIndex.None;
            }
        }

        internal static ColorIndex ToInstrumentForm(this Optical.ColorIndex colorIndex)
        {
            switch (colorIndex)
            {
                case Optical.ColorIndex.Yellowness: return ColorIndex.YELLOWNESS;
                case Optical.ColorIndex.Whiteness: return ColorIndex.WHITENESS;
                case Optical.ColorIndex.Strength: return ColorIndex.STRENGTH;
                case Optical.ColorIndex.MetamerismIndiex: return ColorIndex.METAMERISM_INDIEX;
                case Optical.ColorIndex.Opacity: return ColorIndex.OPACITY;
                case Optical.ColorIndex.Shade555: return ColorIndex.HUE_CLASSIFY_555;
                case Optical.ColorIndex.StainingFastness: return ColorIndex.STAINING_FASTNESS;
                case Optical.ColorIndex.ColorFastness: return ColorIndex.COLOR_FASTNESS;
                case Optical.ColorIndex.Gloss8: return ColorIndex.GLOSS_8;
                default: throw new InvalidOperationException();
            }
        }

        internal static InstrumentLanguage Uniform(this ColorReader.Language language)
        {
            switch (language)
            {
                case Language.English:
                    return InstrumentLanguage.English;
                case Language.Russian:
                    return InstrumentLanguage.Russian;
                case Language.SimplifiedChinese:
                    return InstrumentLanguage.SimplifiedChinese;
                case Language.TraditionalChinese:
                    return InstrumentLanguage.TraditionalChinese;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, null);
            }
        }

        internal static ColorReader.Language ToInstrument(this InstrumentLanguage language)
        {
            switch (language)
            {
                case InstrumentLanguage.English:
                    return Language.English;
                case InstrumentLanguage.SimplifiedChinese:
                    return Language.SimplifiedChinese;
                case InstrumentLanguage.TraditionalChinese:
                    return Language.TraditionalChinese;
                case InstrumentLanguage.Russian:
                    return Language.Russian;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, null);
            }
        }
    }
}
