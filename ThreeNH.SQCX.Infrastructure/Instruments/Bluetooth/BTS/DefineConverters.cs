﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.Optical;

namespace ThreeNH.SQCX.Infrastructure.Instruments.BTS
{
    public static class DefineConverters
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
                case Caliber.Phi15: return MeasurementCaliber.Phi15;
                case Caliber.Phi25: return MeasurementCaliber.Phi1in;
                default: return MeasurementCaliber.Unknown;
            }
        }

        public static Caliber ToInstrumentForm(this MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case MeasurementCaliber.Phi4: return Caliber.Phi4;
                case MeasurementCaliber.Phi8: return Caliber.Phi8;
                case MeasurementCaliber.Phi15: return Caliber.Phi15;
                case MeasurementCaliber.Phi1in: return Caliber.Phi25;
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

        public static ScMode ToInstrumentForm(this SpecularComponent sc)
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
                case UvMode.Uv460: return UVFlag.Cut460nm;
                default: return UVFlag.None;
            }
        }

        internal static UvMode ToInstrumentForm(this UVFlag uv)
        {
            switch (uv)
            {
                case UVFlag.NoCut:
                    return UvMode.NoCut;
                case UVFlag.Cut400nm:
                    return UvMode.Uv400;
                case UVFlag.Cut420nm:
                    return UvMode.Uv420;
                case UVFlag.Cut460nm:
                    return UvMode.Uv460;
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

        internal static ThreeNH.Optical.ColorSpace Uniform(this ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.CIELAB: return ThreeNH.Optical.ColorSpace.CIE_LAB;
                case ColorSpace.CIELCH: return ThreeNH.Optical.ColorSpace.CIE_LCH;
                case ColorSpace.CIEXYZ: return ThreeNH.Optical.ColorSpace.CIE_XYZ;
                case ColorSpace.CIEYxy: return ThreeNH.Optical.ColorSpace.CIE_Yxy;
                case ColorSpace.CIELUV: return ThreeNH.Optical.ColorSpace.CIE_LUV;
                case ColorSpace.HUNTERLAB: return ThreeNH.Optical.ColorSpace.HUNTER_LAB;
                case ColorSpace.SRGB: return ThreeNH.Optical.ColorSpace.sRGB;
                //                case ColorSpace.MUNSELL: return ThreeNH.Optical.ColorSpace.MUNSELL;
                case ColorSpace.BETA_XY: return ThreeNH.Optical.ColorSpace.BETA_xy;
                case ColorSpace.DIN_LAB99: return ThreeNH.Optical.ColorSpace.DIN99_LAB;
                default: return ThreeNH.Optical.ColorSpace.Unknown;
            }
        }

        internal static ColorSpace ToInstrumentForm(this ThreeNH.Optical.ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ThreeNH.Optical.ColorSpace.CIE_LAB: return ColorSpace.CIELAB;
                case ThreeNH.Optical.ColorSpace.CIE_LCH: return ColorSpace.CIELCH;
                case ThreeNH.Optical.ColorSpace.CIE_XYZ: return ColorSpace.CIEXYZ;
                case ThreeNH.Optical.ColorSpace.CIE_Yxy: return ColorSpace.CIEYxy;
                case ThreeNH.Optical.ColorSpace.CIE_LUV: return ColorSpace.CIELUV;
                case ThreeNH.Optical.ColorSpace.HUNTER_LAB: return ColorSpace.HUNTERLAB;
                case ThreeNH.Optical.ColorSpace.sRGB: return ColorSpace.SRGB;
                case ThreeNH.Optical.ColorSpace.BETA_xy: return ColorSpace.BETA_XY;
                case ThreeNH.Optical.ColorSpace.DIN99_LAB: return ColorSpace.DIN_LAB99;
                case ThreeNH.Optical.ColorSpace.MUNSELL: return ColorSpace.MUNSELL;
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
                case ColorDeltaFormula.DECMC11: return ColorDiffFormula.CMC11;
                case ColorDeltaFormula.DECMC21: return ColorDiffFormula.CMC21;
                case ColorDeltaFormula.DECMC: return ColorDiffFormula.CMC;
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
                case ColorDiffFormula.CMC11: return ColorDeltaFormula.DECMC11;
                case ColorDiffFormula.CMC21: return ColorDeltaFormula.DECMC21;
                case ColorDiffFormula.CMC: return ColorDeltaFormula.DECMC;
                case ColorDiffFormula.DHunterE: return ColorDeltaFormula.DEHUNTER;
                case ColorDiffFormula.DIN99: return ColorDeltaFormula.DIN_E99;
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
                case ColorIndex.GARDER_INDEX: return ThreeNH.Optical.ColorIndex.GardnerIndex;
                case ColorIndex.COLOR_FASTNESS: return ThreeNH.Optical.ColorIndex.ColorFastness;
                case ColorIndex.PT_CO_INDEX: return ThreeNH.Optical.ColorIndex.PtCoIndex;
                case ColorIndex.GLOSS_8: return ThreeNH.Optical.ColorIndex.Gloss8;
                case ColorIndex.HAZE: return ThreeNH.Optical.ColorIndex.Haze;
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
                case ThreeNH.Optical.ColorIndex.GardnerIndex: return ColorIndex.GARDER_INDEX;
                case ThreeNH.Optical.ColorIndex.ColorFastness: return ColorIndex.COLOR_FASTNESS;
                case ThreeNH.Optical.ColorIndex.Haze: return ColorIndex.HAZE;
                case ThreeNH.Optical.ColorIndex.Gloss8: return ColorIndex.GLOSS_8;
                case ThreeNH.Optical.ColorIndex.None: return ColorIndex.INVALID;
                case ThreeNH.Optical.ColorIndex.PtCoIndex: return ColorIndex.PT_CO_INDEX;//added by Liguanghai 20/03/28
                default: throw new InvalidOperationException();
            }
        }
    }
}
