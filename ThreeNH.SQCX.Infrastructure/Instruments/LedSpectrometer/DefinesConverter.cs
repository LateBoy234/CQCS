using System;
using System.ComponentModel;
using ThreeNH.ColorAlgorithm;
using ThreeNH.Optical;
using ColorDiffFormula = ThreeNH.SQCX.Infrastructure.Instruments.LedSpectrometer.Instrument.ColorDiffFormula;

namespace ThreeNH.SQCX.Infrastructure.Instruments.LedSpectrometer
{
    internal static class DefinesConverter
    {
        /// <summary>
        /// 将仪器中使用的标准光源代号转换为全局使用的
        /// </summary>
        /// <param name="illuminant">仪器中使用的光源代号</param>
        /// <returns></returns>
        internal static StandardIlluminant Uniform(this Instrument.StandardIlluminant illuminant)
        {
            switch (illuminant)
            {
                case Instrument.StandardIlluminant.A: return StandardIlluminant.A;
                case Instrument.StandardIlluminant.C: return StandardIlluminant.C;
                case Instrument.StandardIlluminant.D50: return StandardIlluminant.D50;
                case Instrument.StandardIlluminant.D55: return StandardIlluminant.D55;
                case Instrument.StandardIlluminant.D65: return StandardIlluminant.D65;
                case Instrument.StandardIlluminant.D75: return StandardIlluminant.D75;
                case Instrument.StandardIlluminant.F1: return StandardIlluminant.F1;
                case Instrument.StandardIlluminant.F2: return StandardIlluminant.F2;
                case Instrument.StandardIlluminant.F3: return StandardIlluminant.F3;
                case Instrument.StandardIlluminant.F4: return StandardIlluminant.F4;
                case Instrument.StandardIlluminant.F5: return StandardIlluminant.F5;
                case Instrument.StandardIlluminant.F6: return StandardIlluminant.F6;
                case Instrument.StandardIlluminant.F7: return StandardIlluminant.F7;
                case Instrument.StandardIlluminant.F8: return StandardIlluminant.F8;
                case Instrument.StandardIlluminant.F9: return StandardIlluminant.F9;
                case Instrument.StandardIlluminant.F10: return StandardIlluminant.F10;
                case Instrument.StandardIlluminant.F11: return StandardIlluminant.F11;
                case Instrument.StandardIlluminant.F12: return StandardIlluminant.F12;
                default: throw new ArgumentException("Unknown illuminant code");
            }
        }

        /// <summary>
        /// 将全局使用的标准光源代号转换回仪器中使用的
        /// </summary>
        /// <param name="illuminant">全局中使用的光源代号</param>
        /// <returns>返回仪器中使用的光源代号</returns>
        internal static Instrument.StandardIlluminant ToInstrumentForm(this StandardIlluminant illuminant)
        {
            switch (illuminant)
            {
                case StandardIlluminant.A: return Instrument.StandardIlluminant.A;
                case StandardIlluminant.C: return Instrument.StandardIlluminant.C;
                case StandardIlluminant.D50: return Instrument.StandardIlluminant.D50;
                case StandardIlluminant.D55: return Instrument.StandardIlluminant.D55;
                case StandardIlluminant.D65: return Instrument.StandardIlluminant.D65;
                case StandardIlluminant.D75: return Instrument.StandardIlluminant.D75;
                case StandardIlluminant.F1: return Instrument.StandardIlluminant.F1;
                case StandardIlluminant.F2: return Instrument.StandardIlluminant.F2;
                case StandardIlluminant.F3: return Instrument.StandardIlluminant.F3;
                case StandardIlluminant.F4: return Instrument.StandardIlluminant.F4;
                case StandardIlluminant.F5: return Instrument.StandardIlluminant.F5;
                case StandardIlluminant.F6: return Instrument.StandardIlluminant.F6;
                case StandardIlluminant.F7: return Instrument.StandardIlluminant.F7;
                case StandardIlluminant.F8: return Instrument.StandardIlluminant.F8;
                case StandardIlluminant.F9: return Instrument.StandardIlluminant.F9;
                case StandardIlluminant.F10: return Instrument.StandardIlluminant.F10;
                case StandardIlluminant.F11: return Instrument.StandardIlluminant.F11;
                case StandardIlluminant.F12: return Instrument.StandardIlluminant.F12;
                default: throw new ArgumentException("Unknown illuminant code");
            }
        }

        /// <summary>
        /// 将仪器中的标准观察者代号转换为全局使用的
        /// </summary>
        /// <param name="observer">仪器中的观察者代号</param>
        /// <returns>全局使用的观察者代号</returns>
        internal static StandardObserver Uniform(this Instrument.StandardObserver observer)
        {
            switch (observer)
            {
                case Instrument.StandardObserver.CIE1931: return StandardObserver.CIE1931;
                case Instrument.StandardObserver.CIE1964: return StandardObserver.CIE1964;
                default: throw new ArgumentException("Invalid standard observer code");
            }
        }

        /// <summary>
        /// 将全局使用的标准观察者代号转换为仪器中使用的
        /// </summary>
        /// <param name="observer">全局使用的观察者代号</param>
        /// <returns>仪器中的观察者代号</returns>
        internal static Instrument.StandardObserver ToInstrumentForm(this StandardObserver observer)
        {
            switch (observer)
            {
                case StandardObserver.CIE1931: return Instrument.StandardObserver.CIE1931;
                case StandardObserver.CIE1964: return Instrument.StandardObserver.CIE1964;
                default: throw new ArgumentException("Invalid standard observer code");
            }
        }

        internal static MeasurementCaliber Uniform(this Instrument.MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case Instrument.MeasurementCaliber.Phi4: return MeasurementCaliber.Phi4;
                case Instrument.MeasurementCaliber.Phi8: return MeasurementCaliber.Phi8;
                case Instrument.MeasurementCaliber.Phi8Plus: return MeasurementCaliber.Phi2;
                default: return MeasurementCaliber.Unknown;
            }
        }

        internal static Instrument.MeasurementCaliber ToInstrumentForm(this MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case MeasurementCaliber.Phi4: return Instrument.MeasurementCaliber.Phi4;
                case MeasurementCaliber.Phi8: return Instrument.MeasurementCaliber.Phi8;
                case MeasurementCaliber.Phi2: return Instrument.MeasurementCaliber.Phi8Plus;
                default: return Instrument.MeasurementCaliber.Unknown;
            }
        }

        internal static SpecularComponent Uniform(this Instrument.SpecularComponent sc)
        {
            switch (sc)
            {
                case Instrument.SpecularComponent.SCI: return SpecularComponent.SCI;
                case Instrument.SpecularComponent.SCE: return SpecularComponent.SCE;
                default: throw new ArgumentException("Invalid specular component code");
            }
        }

        internal static Instrument.SpecularComponent ToInstrumentForm(this SpecularComponent sc)
        {
            switch (sc)
            {
                case SpecularComponent.SCI: return Instrument.SpecularComponent.SCI;
                case SpecularComponent.SCE: return Instrument.SpecularComponent.SCE;
                default: throw new ArgumentException("Invalid specular component code");
            }
        }

        internal static ColorSpace Uniform(this Instrument.ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case Instrument.ColorSpace.CIE_LABCH:
                case Instrument.ColorSpace.CIE_LAB:
                    return ColorSpace.CIE_LAB;
                case Instrument.ColorSpace.CIE_XYZ:
                    return ColorSpace.CIE_XYZ;
                case Instrument.ColorSpace.HUNTER_LAB:
                    return ColorSpace.HUNTER_LAB;
                default: throw new InvalidEnumArgumentException(nameof(colorSpace), (int)colorSpace, typeof(Instrument.ColorSpace));
            }
        }

        internal static Instrument.ColorSpace ToInstrumentForm(this ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.CIE_LAB: return Instrument.ColorSpace.CIE_LAB;
                case ColorSpace.CIE_LCH: return Instrument.ColorSpace.CIE_LABCH;
                case ColorSpace.CIE_Yxy:
                case ColorSpace.CIE_XYZ: return Instrument.ColorSpace.CIE_XYZ;
                case ColorSpace.HUNTER_LAB: return Instrument.ColorSpace.HUNTER_LAB;
                default: throw new InvalidOperationException();
            }
        }

        internal static Optical.ColorDiffFormula Uniform(this Instrument.ColorDiffFormula formula)
        {
            switch (formula)
            {
                case ColorDiffFormula.DE : return Optical.ColorDiffFormula.CIE1976;
                case ColorDiffFormula.DE94: return Optical.ColorDiffFormula.CIE1994;
                case ColorDiffFormula.DE2000: return Optical.ColorDiffFormula.CIE2000;
                case ColorDiffFormula.CMC11: return Optical.ColorDiffFormula.CMC11;
                case ColorDiffFormula.CMC21: return Optical.ColorDiffFormula.CMC21;
                case ColorDiffFormula.HUNTER_DE: return Optical.ColorDiffFormula.DHunterE;
                case ColorDiffFormula.NH_ELUV76: return Optical.ColorDiffFormula.DEuv;
                default: return Optical.ColorDiffFormula.CIE1976;
            }
        }

        internal static Instrument.ColorDiffFormula ToInstrumentForm(this Optical.ColorDiffFormula formula)
        {
            switch (formula)
            {
                case Optical.ColorDiffFormula.CIE1976: return ColorDiffFormula.DE;
                case Optical.ColorDiffFormula.CIE1994: return ColorDiffFormula.DE94;
                case Optical.ColorDiffFormula.CMC11: return ColorDiffFormula.CMC11;
                case Optical.ColorDiffFormula.CMC21: return ColorDiffFormula.CMC21;
                case Optical.ColorDiffFormula.CIE2000: return ColorDiffFormula.DE2000;
                case Optical.ColorDiffFormula.DHunterE: return ColorDiffFormula.HUNTER_DE;
                case Optical.ColorDiffFormula.DEuv: return ColorDiffFormula.NH_ELUV76;
                default: throw new InvalidOperationException();
            }
        }
    }
}
