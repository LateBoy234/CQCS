﻿using System;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer
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

                case Instrument.StandardIlluminant.CWF: return StandardIlluminant.CWF;
                case Instrument.StandardIlluminant.DLF: return StandardIlluminant.DLF;
                case Instrument.StandardIlluminant.TL83: return StandardIlluminant.TL83;
                case Instrument.StandardIlluminant.TL84: return StandardIlluminant.TL84;
                case Instrument.StandardIlluminant.TPL5: return StandardIlluminant.TPL5;
                case Instrument.StandardIlluminant.U30: return StandardIlluminant.U30;
                case Instrument.StandardIlluminant.B: return StandardIlluminant.B;
                case Instrument.StandardIlluminant.U35: return StandardIlluminant.U35;
                case Instrument.StandardIlluminant.NBF: return StandardIlluminant.NBF;
                case Instrument.StandardIlluminant.ID50: return StandardIlluminant.ID50;
                case Instrument.StandardIlluminant.ID65: return StandardIlluminant.ID65;
                case Instrument.StandardIlluminant.LED_B1: return StandardIlluminant.LED_B1;
                case Instrument.StandardIlluminant.LED_B2: return StandardIlluminant.LED_B2;
                case Instrument.StandardIlluminant.LED_B3: return StandardIlluminant.LED_B3;
                case Instrument.StandardIlluminant.LED_B4: return StandardIlluminant.LED_B4;
                case Instrument.StandardIlluminant.LED_B5: return StandardIlluminant.LED_B5;
                case Instrument.StandardIlluminant.LED_BH1: return StandardIlluminant.LED_BH1;
                case Instrument.StandardIlluminant.LED_RGB1: return StandardIlluminant.LED_RGB1;
                case Instrument.StandardIlluminant.LED_V1: return StandardIlluminant.LED_V1;
                case Instrument.StandardIlluminant.LED_V2: return StandardIlluminant.LED_V2;
                case Instrument.StandardIlluminant.LED_C2: return StandardIlluminant.LED_C2;
                case Instrument.StandardIlluminant.LED_C3: return StandardIlluminant.LED_C3;
                case Instrument.StandardIlluminant.LED_C5: return StandardIlluminant.LED_C5;
                case Instrument.StandardIlluminant.LED_T8G: return StandardIlluminant.LEDT8G;
                case Instrument.StandardIlluminant.U3500: return StandardIlluminant.U3500;
               // case Instrument.StandardIlluminant.H: return StandardIlluminant.H;
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

                case StandardIlluminant.CWF: return Instrument.StandardIlluminant.CWF;
                case StandardIlluminant.DLF: return Instrument.StandardIlluminant.DLF;
                case StandardIlluminant.TL83: return Instrument.StandardIlluminant.TL83;
                case StandardIlluminant.TL84: return Instrument.StandardIlluminant.TL84;
                case StandardIlluminant.TPL5: return Instrument.StandardIlluminant.TPL5;
                case StandardIlluminant.U30: return Instrument.StandardIlluminant.U30;
                case StandardIlluminant.B: return Instrument.StandardIlluminant.B;
                case StandardIlluminant.U35: return Instrument.StandardIlluminant.U35;
                case StandardIlluminant.NBF: return Instrument.StandardIlluminant.NBF;
                case StandardIlluminant.ID50: return Instrument.StandardIlluminant.ID50;
                case StandardIlluminant.ID65: return Instrument.StandardIlluminant.ID65;
                case StandardIlluminant.LED_B1: return Instrument.StandardIlluminant.LED_B1;
                case StandardIlluminant.LED_B2: return Instrument.StandardIlluminant.LED_B2;
                case StandardIlluminant.LED_B3: return Instrument.StandardIlluminant.LED_B3;
                case StandardIlluminant.LED_B4: return Instrument.StandardIlluminant.LED_B4;
                case StandardIlluminant.LED_B5: return Instrument.StandardIlluminant.LED_B5;
                case StandardIlluminant.LED_BH1: return Instrument.StandardIlluminant.LED_BH1;
                case StandardIlluminant.LED_RGB1: return Instrument.StandardIlluminant.LED_RGB1;
                case StandardIlluminant.LED_V1: return Instrument.StandardIlluminant.LED_V1;
                case StandardIlluminant.LED_V2: return Instrument.StandardIlluminant.LED_V2;
                case StandardIlluminant.LED_C2: return Instrument.StandardIlluminant.LED_C2;
                case StandardIlluminant.LED_C3: return Instrument.StandardIlluminant.LED_C3;
                case StandardIlluminant.LED_C5: return Instrument.StandardIlluminant.LED_C5;
                case StandardIlluminant.LEDT8G: return Instrument.StandardIlluminant.LED_T8G;
                case StandardIlluminant.U3500: return Instrument.StandardIlluminant.U3500;
                //case StandardIlluminant.H: return Instrument.StandardIlluminant.H;
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
                case Instrument.MeasurementCaliber.Phi1by3: return MeasurementCaliber.Phi1By3;
                default: return MeasurementCaliber.Unknown;
            }
        }
        internal static MeasurementCaliber UniformForKS(this Instrument.MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case Instrument.MeasurementCaliber.Phi4: return MeasurementCaliber.Phi10;
                case Instrument.MeasurementCaliber.Phi8: return MeasurementCaliber.Phi11;
                case Instrument.MeasurementCaliber.Phi1by3: return MeasurementCaliber.Phi6;
                case Instrument.MeasurementCaliber.Phi2: return MeasurementCaliber.Phi5;
                case Instrument.MeasurementCaliber.Phi7: return MeasurementCaliber.Phi3;
                case Instrument.MeasurementCaliber.Unknown: return MeasurementCaliber.Phi1By3;
                default: return MeasurementCaliber.Phi10;
            }
        }

        internal static Instrument.MeasurementCaliber ToInstrumentForm(this MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case MeasurementCaliber.Phi4: return Instrument.MeasurementCaliber.Phi4;
                case MeasurementCaliber.Phi8: return Instrument.MeasurementCaliber.Phi8;
                case MeasurementCaliber.Phi1By3: return Instrument.MeasurementCaliber.Phi1by3;
                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// KS/PS 系列仪器口径编码与常规型号不同，回写须用 UniformForKS 的逆映射
        /// </summary>
        internal static Instrument.MeasurementCaliber ToInstrumentFormForKS(this MeasurementCaliber caliber)
        {
            switch (caliber)
            {
                case MeasurementCaliber.Phi11: return Instrument.MeasurementCaliber.Phi8;
                case MeasurementCaliber.Phi10: return Instrument.MeasurementCaliber.Phi4;
                case MeasurementCaliber.Phi6: return Instrument.MeasurementCaliber.Phi1by3;
                case MeasurementCaliber.Phi5: return Instrument.MeasurementCaliber.Phi2;
                case MeasurementCaliber.Phi3: return Instrument.MeasurementCaliber.Phi7;
                case MeasurementCaliber.Phi1By3: return Instrument.MeasurementCaliber.Unknown;
                default: throw new InvalidOperationException();
            }
        }

        internal static SpecularComponent Uniform(this Instrument.SpecularComponent sc)
        {
            switch (sc)
            {
                case Instrument.SpecularComponent.SCI: return SpecularComponent.SCI;
                case Instrument.SpecularComponent.SCE: return SpecularComponent.SCE;
                case Instrument.SpecularComponent.SCIAndSCE: return SpecularComponent.SCIAndSCE;
                default: throw new ArgumentException("Invalid specular component code");
            }
        }

        internal static Instrument.SpecularComponent ToInstrumentForm(this SpecularComponent sc)
        {
            switch (sc)
            {
                case SpecularComponent.SCI: return Instrument.SpecularComponent.SCI;
                case SpecularComponent.SCE: return Instrument.SpecularComponent.SCE;
                case SpecularComponent.SCIAndSCE: return Instrument.SpecularComponent.SCIAndSCE;
                default: return Instrument.SpecularComponent.SCI;
            }
        }

        internal static ColorSpace Uniform(this Instrument.ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case Instrument.ColorSpace.CIE_LAB: return ColorSpace.CIE_LAB;
                case Instrument.ColorSpace.CIE_LCH: return ColorSpace.CIE_LCH;
                case Instrument.ColorSpace.CIE_XYZ: return ColorSpace.CIE_XYZ;
                case Instrument.ColorSpace.CIE_Yxy: return ColorSpace.CIE_Yxy;
                case Instrument.ColorSpace.HUNTER_LAB: return ColorSpace.HUNTER_LAB;
                case Instrument.ColorSpace.CIE_LUV: return ColorSpace.CIE_LUV;
                case Instrument.ColorSpace.Betaxy: return ColorSpace.BETA_xy;
                case Instrument.ColorSpace.S_RGB: return ColorSpace.sRGB;//
                default: throw new ArgumentException("Unknown color space code");
            }
        }

        internal static Instrument.ColorSpace ToInstrumentForm(this ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.CIE_LAB: return Instrument.ColorSpace.CIE_LAB;
                case ColorSpace.CIE_LCH: return Instrument.ColorSpace.CIE_LCH;
                case ColorSpace.CIE_XYZ: return Instrument.ColorSpace.CIE_XYZ;
                case ColorSpace.CIE_Yxy: return Instrument.ColorSpace.CIE_Yxy;
                case ColorSpace.HUNTER_LAB: return Instrument.ColorSpace.HUNTER_LAB;
                case ColorSpace.CIE_LUV: return Instrument.ColorSpace.CIE_LUV;
                case ColorSpace.sRGB: return Instrument.ColorSpace.S_RGB;
                case ColorSpace.BETA_xy: return Instrument.ColorSpace.Betaxy;
                default: throw new ArgumentException("Instrument not support");
            }
        }

        internal static ColorDiffFormula Uniform(this Instrument.ColorDiffFormula colorDiffFormula)
        {
            switch (colorDiffFormula)
            {
                case Instrument.ColorDiffFormula.CIE_DE: return ColorDiffFormula.CIE1976;
                case Instrument.ColorDiffFormula.CIE_DE94: return ColorDiffFormula.CIE1994;
                case Instrument.ColorDiffFormula.HUNTER_DE: return ColorDiffFormula.DHunterE;
                case Instrument.ColorDiffFormula.CMC11:
                case Instrument.ColorDiffFormula.CMC21:
                case Instrument.ColorDiffFormula.CMCLC:
                    return ColorDiffFormula.CMC;
                case Instrument.ColorDiffFormula.CIE_DE00: return ColorDiffFormula.CIE2000;
                default: throw new ArgumentException("Unknown color space code");
            }
        }

        internal static Instrument.ColorDiffFormula ToInstrumentForm(this ColorDiffFormula colorDiffFormula)
        {
            switch (colorDiffFormula)
            {
                case ColorDiffFormula.CIE1976: return Instrument.ColorDiffFormula.CIE_DE;
                case ColorDiffFormula.CIE1994: return Instrument.ColorDiffFormula.CIE_DE94;
                case ColorDiffFormula.DHunterE: return Instrument.ColorDiffFormula.HUNTER_DE;
                case ColorDiffFormula.CMC: return Instrument.ColorDiffFormula.CMCLC;
                case ColorDiffFormula.CMC11: return Instrument.ColorDiffFormula.CMC11;
                case ColorDiffFormula.CMC21: return Instrument.ColorDiffFormula.CMC21;
                case ColorDiffFormula.CIE2000: return Instrument.ColorDiffFormula.CIE_DE00;
                default: throw new ArgumentException("Instrument not support");
            }
        }

        internal static DataModel.Tolerance Uniform(this Tolerance tol)
        {
            var dataModelTol = new DataModel.Tolerance();
            dataModelTol[ChromaticityField.dEab] = new ToleranceItem
            {
                Upper = tol.dE,
                UseForJudgement = true
            };
            dataModelTol[ChromaticityField.dL] = new ToleranceItem
            {
                Lower = tol.dL_Lower,
                Upper = tol.dL_Upper,
                UseForJudgement = true
            };
            dataModelTol[ChromaticityField.da] = new ToleranceItem
            {
                Lower = tol.da_Lower,
                Upper = tol.da_Upper,
                UseForJudgement = true
            };
            dataModelTol[ChromaticityField.db] = new ToleranceItem
            {
                Lower = tol.db_Lower,
                Upper = tol.db_Upper,
                UseForJudgement = true
            };
            return dataModelTol;
        }

        internal static Tolerance ToInstrumentForm(this DataModel.Tolerance tolerance)
        {

            var tol = new Tolerance
            {
                dE = tolerance[ChromaticityField.dEab].Upper.GetValueOrDefault(1.0),
                dL_Lower = tolerance[ChromaticityField.dL].Lower.GetValueOrDefault(-1.0),
                da_Lower = tolerance[ChromaticityField.da].Lower.GetValueOrDefault(-1.0),
                db_Lower = tolerance[ChromaticityField.db].Lower.GetValueOrDefault(-1.0),
                dL_Upper = tolerance[ChromaticityField.dL].Upper.GetValueOrDefault(1.0),
                da_Upper = tolerance[ChromaticityField.da].Upper.GetValueOrDefault(1.0),
                db_Upper = tolerance[ChromaticityField.db].Upper.GetValueOrDefault(1.0),
            };
            return tol;
        }

        internal static UVFlag Uniform(this UVFlag uv)
        {
            return uv;
        }

        internal static UVFlag ToInstrumentForm(this UVFlag uv)
        {
            if (uv == UVFlag.None || uv == UVFlag.On)
                return uv;
            throw new InvalidOperationException();
        }
    }
}
