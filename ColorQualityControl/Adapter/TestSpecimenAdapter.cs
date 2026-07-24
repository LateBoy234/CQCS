using System;
using System.Collections.Generic;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.ColorAlgorithm;
using System.ComponentModel;
using ThreeNH.Helper;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Diagnostics;

namespace ThreeNH.ColorQualityControl.Adapter
{
    public class TestSpecimenAdapter : SpecimenAdapter
    {
        /// <summary>
        /// 通过给定条件构造一个试样适配器
        /// </summary>
        /// <param name="test">试样</param>
        /// <param name="standard">试样的标样的适配器，其ChromaticitySetting、Formula、SpecularComponent
        /// 必须与传个试样的参数相同</param>
        /// <param name="referenceFormula1">参考1，用于计算同色异谱指数</param>
        /// <param name="referenceFormula2">参考2，用于计算同色异谱指数</param>
        public TestSpecimenAdapter(
            TestSpecimen test,
            Formula formula,
            ChromaticitySetting setting,
            SpecularComponent sc,
            StandardSpecimenAdapter standard = null,
            Formula referenceFormula1 = null,
            Formula referenceFormula2 = null)
            : base(test)
        {
            StainingFastness_dEgs = ColorFastness_dEf = MI1 = MI2 = Opacity = double.NaN;
            StainingFastness_SSRLevel = ColorFastness_GSRLevel = ColourFastnessLevel.INVALID;
            CalculateValues(formula, setting, sc, standard, /*tolerance, */referenceFormula1, referenceFormula2);
        }

        public TestSpecimen TestSpecimen
        {
            get { return Specimen as TestSpecimen; }
        }


        protected void CalculateValues(
            Formula formula,
            ChromaticitySetting setting,
            SpecularComponent sc,
            StandardSpecimenAdapter standard = null,
            Formula referenceFormula1 = null,
            Formula referenceFormula2 = null)
        {
            base.CalculateValues(setting, sc, formula);

            if (standard == null)
                return;

            CalculateHazeDiffValues(setting, standard);
            if (standard.XYZ == null || XYZ == null)
                return;
            CalculateColorDiffValues(formula, setting, standard, /*tolerance, */referenceFormula1, referenceFormula2);
            CalculateChromaticityIndexValues(formula, setting, standard, /*tolerance,*/ referenceFormula1, referenceFormula2);
            CalculateChromaticityIndexDiffValues(formula, setting, standard, referenceFormula1, referenceFormula2);
        }

        private void CalculateHazeDiffValues(ChromaticitySetting setting, StandardSpecimenAdapter standard)
        {
            foreach (var field in setting.DisplayFields)
            {
                switch (field)
                {
                    case ChromaticityField.DeltaTransmittanceC2:
                        DeltaTransmittanceC2 = TransmittanceC2 - standard.TransmittanceC2;
                        break;
                    case ChromaticityField.DeltaTransmittanceA2:
                        DeltaTransmittanceA2 = TransmittanceA2 - standard.TransmittanceA2;
                        break;
                    case ChromaticityField.DeltaTransmittanceD65:
                        DeltaTransmittanceD65 = TransmittanceD65 - standard.TransmittanceD65;
                        break;
                    case ChromaticityField.DeltaHazeC2:
                        DeltaHazeC2 = HazeC2 - standard.HazeC2;
                        break;
                    case ChromaticityField.DeltaHazeA2:
                        DeltaHazeA2 = HazeA2 - standard.HazeA2;
                        break;
                    case ChromaticityField.DeltaHazeD65:
                        DeltaHazeD65 = HazeD65 - standard.HazeD65;
                        break;
                    case ChromaticityField.DeltaClarity:
                        DeltaClarity = Clarity - standard.Clarity;
                        break;
                    default:
                        break;
                }
            }
        }

        private void CalculateChromaticityIndexDiffValues(Formula formula, ChromaticitySetting setting, StandardSpecimenAdapter standard, Formula referenceFormula1, Formula referenceFormula2)
        {
            foreach (var field in setting.DisplayFields)
            {
                switch (field)
                {
                    case ChromaticityField.Gloss8:
                        DeltaGloss8 = Gloss8 - standard.Gloss8;
                        break;
                    default:
                        break;
                }
            }
        }

        private void CalculateDeltaGloss8(StandardSpecimenAdapter standard)
        {
            DeltaGloss8 = double.NaN;
            if (standard.Gloss8 != double.NaN && Gloss8 != double.NaN)
                DeltaGloss8 = Gloss8 - standard.Gloss8;
        }

        private void CalculateColorDiffValues(
            Formula formula,
            ChromaticitySetting setting,
            StandardSpecimenAdapter standard,
            Formula referenceFormula1,
            Formula referenceFormula2)
        {
            DeltaLab = null;
            DeltaHunterLab = null;
            DeltaXyz = null;
            DeltaLuv = null;
            DeltaRgb = null;

            if (setting.NeedCIEDE1976)
            {
                DeltaLab = new CIEDE1976(Lab, standard.Lab);
            }

            if (setting.NeedCIEDE1994)
            {
                CIEDE1994 = standard.Tolerance == null
                    ? new CIEDE1994(Lab, standard.Lab)
                    : new CIEDE1994(Lab, standard.Lab, standard.Tolerance.KL94, standard.Tolerance.KC94,
                        standard.Tolerance.KH94);
            }
            if (setting.NeedCIEDE2000)
            {
                CIEDE2000 = standard.Tolerance == null
                    ? new CIEDE2000(Lab, standard.Lab)
                    : new CIEDE2000(Lab, standard.Lab, standard.Tolerance.KL00, standard.Tolerance.KC00,
                        standard.Tolerance.KH00);
            }
            if (setting.NeedCMCDE)
            {
                CMCDE = standard.Tolerance == null
                    ? new CMCDE(Lab, standard.Lab)
                    : new CMCDE(Lab, standard.Lab, standard.Tolerance.KLcmc, standard.Tolerance.KCcmc);
            }
            if (setting.NeedDeltaXyz)
            {
                DeltaXyz = new DeltaXYZ(XYZ, standard.XYZ);
            }
            if (setting.NeedDeltaHunterLab)
            {
                DeltaHunterLab = new DeltaHunterLab(HunterLab, standard.HunterLab);
            }
            if (setting.NeedDeltaLuv)
            {
                DeltaLuv = new DeltaLuv(Luv, standard.Luv);
                Trace.TraceInformation("ΔE*uv的值为：{0}", DeltaLuv.dE);
            }
            if (setting.NeedDeltaRGB)
            {
                DeltaRgb = new DeltaRGB(CIERGB, standard.CIERGB);
            }

            if (setting.NeedDIN99)
            {
                Din99 = new DIN99(Din99Lab, standard.Din99Lab);
            }

            if (setting.NeedCMC11DE)
            {
                CMC11DE = new CMCDE(Lab, standard.Lab, 1.0, 1.0);
            }

            if (setting.NeedCMC21DE)
            {
                CMC21DE = new CMCDE(Lab, standard.Lab, 2.0, 1.0);
            }

            if (setting.NeedMunsell)
            {
                DeltaMunsell = new DeltaMunsellHVC(MunsellHVC, standard.MunsellHVC);
            }
            if (Spectrum != null && standard.Spectrum != null)
            {
                ReflectanceDifferences = new Spectrum(Spectrum.Range);
                foreach (int wavelength in Spectrum.Range.Wavelengths)
                {
                    if (Spectrum.Range.Contains(wavelength) && standard.Spectrum.Range.Contains(wavelength))
                    {
                        ReflectanceDifferences[wavelength] = Spectrum[wavelength] - standard.Spectrum[wavelength];
                    }
                    else
                    {
                        ReflectanceDifferences[wavelength] = 0.0;
                    }
                }
            }

            if (Lab != null && DeltaLab != null)
            {
                ColorOffset = ColorOffsetHelper.FromDelta(DeltaLab.dL, DeltaLab.da, DeltaLab.db);
            }

            dLuminanceFactor = standard.LuminanceFactor - LuminanceFactor;
        }


        private void CalculateChromaticityIndexValues(
            Formula formula,
            ChromaticitySetting setting,
            StandardSpecimenAdapter standard,
            Formula referenceFormula1,
            Formula referenceFormula2)
        {
            foreach (var field in setting.DisplayFields)
            {
                switch (field)
                {
                    case ChromaticityField.StainingFastness_dEgs:
                        StainingFastness_dEgs = formula.GetStainingFastness_dEgs(standard.Lab, Lab);
                        break;
                    case ChromaticityField.StainingFastness_SSRLevel:
                        StainingFastness_SSRLevel = formula.GetReportedSSR(standard.Lab, Lab);
                        break;
                    case ChromaticityField.ColorFastness_dEf:
                        ColorFastness_dEf = formula.GetColorFastness_dEf(standard.Lab, Lab);
                        break;
                    case ChromaticityField.ColorFastness_GSRLevel:
                        ColorFastness_GSRLevel = formula.GetReportedGSR(standard.Lab, Lab);
                        break;
                        //力份总和
                    case ChromaticityField.AverageStrength:
                        if (Spectrum != null && standard.Spectrum != null)
                        {
                            AverageStrength = formula.GetAverageStrength(standard.Spectrum, Spectrum,standard.Specimen.IsTransmission);
                        }
                        break;
                       //最大吸收力份
                    case ChromaticityField.PrecentStrengthSWL:
                        if (Spectrum != null && TestSpecimen != null)
                        {
                            int wavelength = 0;
                            PrecentStrengthSWL = formula.GetColorValueSWL(Spectrum, ref wavelength, TestSpecimen.IsTransmission);
                            WavelengthOfPrecentStrengthSWL = wavelength;
                        }
                        else if (Spectrum != null && TestSpecimen == null)
                        {
                            int wavelength = 0;
                            PrecentStrengthSWL = formula.GetColorValueSWL(Spectrum, ref wavelength,false);
                            WavelengthOfPrecentStrengthSWL = wavelength;
                        }
                        break;
                    //加权力份
                    case ChromaticityField.WeightedStrengthSWL:
                        if (Spectrum != null && standard.Spectrum != null)
                        {
                            if (standard.Specimen != null)
                            {
                                WeightedStrengthSWL = formula.GetWeightedStrength(standard.Spectrum, Spectrum, standard.Specimen.IsTransmission);
                            }
                            else
                            {
                                WeightedStrengthSWL = formula.GetWeightedStrength(standard.Spectrum, Spectrum, false);
                            }
                        }
                        break;
                    case ChromaticityField.MI1:
                        if (referenceFormula1 != null)
                        {
                            if (Spectrum != null && standard.Spectrum != null)
                            {
                                MI1 = formula.GetMetamerismIndiex(standard.Spectrum, Spectrum, referenceFormula1);
                            }
                        }

                        break;
                    case ChromaticityField.MI2:
                        if (referenceFormula2 != null)
                        {
                            if (Spectrum != null && standard.Spectrum != null)
                            {
                                MI2 = formula.GetMetamerismIndiex(standard.Spectrum, Spectrum,      referenceFormula2);
                            }
                        }
                        break;
                    case ChromaticityField.Opacity:
                        var tmpValue = formula.GetOpacity(XYZ, standard.XYZ);
                        if (tmpValue > 1.0)
                        {
                            Opacity = 1;
                        }
                        else
                        {
                            Opacity = tmpValue;
                        }
                        break;

                    case ChromaticityField.ShadeBy555:
                        if (standard.Tolerance != null)
                        {
                            ShadeBy555 = Formula.Get555Shade(
                                standard.Lab, Lab,
                                Math.Max(Math.Abs((double)standard.Tolerance[ChromaticityField.dL].Lower), Math.Abs((double)standard.Tolerance[ChromaticityField.dL].Upper)),
                                Math.Max(Math.Abs((double)standard.Tolerance[ChromaticityField.da].Lower), Math.Abs((double)standard.Tolerance[ChromaticityField.da].Upper)),
                                Math.Max(Math.Abs((double)standard.Tolerance[ChromaticityField.db].Lower), Math.Abs((double)standard.Tolerance[ChromaticityField.db].Upper)));
                        }
                        else
                        {
                            ShadeBy555 = Formula.Get555Shade(standard.Lab, Lab, 0.5, 0.5, 0.5);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        //        public DeltaLab DeltaLab { get; private set; }
        public CIEDE1976 DeltaLab { get; private set; }
        public CIEDE2000 CIEDE2000 { get; private set; }
        public CIEDE1994 CIEDE1994 { get; private set; }
        public CMCDE CMCDE { get; private set; }
        public CMCDE CMC11DE { get; private set; }
        public CMCDE CMC21DE { get; private set; }
        public DeltaHunterLab DeltaHunterLab { get; private set; }
        public DeltaLuv DeltaLuv { get; private set; }
        public DeltaXYZ DeltaXyz { get; private set; }
        public DeltaRGB DeltaRgb { get; private set; }
        public DIN99 Din99 { get; private set; }
        public DeltaMunsellHVC DeltaMunsell { get; private set; }

        public override string GetFormatedPropertyValue(string propertyName, string defaultReturn = "--")
        {
            ChromaticityField field;
            if (!Enum.TryParse(propertyName, out field))
            {
                //if (propertyName == "MI1" || propertyName == "MI2")
                //{
                //    field = ChromaticityField.MI;
                // }
                //else
                //{
                return defaultReturn;
                //}
            }

            try
            {
                var converter = GetPropertyValueConverter(field);
                var value = GetPropertyValue(propertyName);
                return converter.Convert(value, typeof(string), null, CultureInfo.CurrentCulture) as string;
            }
            catch (Exception)
            {
                return defaultReturn;
            }
        }


        public void Judge(StandardSpecimenAdapter standard, JudgementSetting judgementSetting)
        {
            if (standard?.Tolerance == null)
            {
                Judgement = null;
            }
            else
            {
                Judge(standard, judgementSetting, standard.Tolerance);
            }
        }

        public void Judge(StandardSpecimenAdapter standard, JudgementSetting judgementSetting, Tolerance tolerance)
        {
            if (CIEDE1994 != null)
            {
                CIEDE1994 = tolerance == null
                    ? new CIEDE1994(Lab, standard.Lab)
                    : new CIEDE1994(Lab, standard.Lab, tolerance.KL94, tolerance.KC94,
                        tolerance.KH94);
            }
            if (CIEDE2000 != null)
            {
                CIEDE2000 = tolerance == null
                    ? new CIEDE2000(Lab, standard.Lab)
                    : new CIEDE2000(Lab, standard.Lab, tolerance.KL00, tolerance.KC00,
                        tolerance.KH00);
            }
            if (CMCDE != null)
            {
                CMCDE = tolerance == null
                    ? new CMCDE(Lab, standard.Lab)
                    : new CMCDE(Lab, standard.Lab, tolerance.KLcmc, tolerance.KCcmc);
            }
            Judgement = Judgement.Judge(this, tolerance, judgementSetting);
            // 如果使用了555分类，要更新555分类结果
            if (ShadeBy555 != 0)
            {
                if (tolerance != null)
                {
                    ShadeBy555 = Formula.Get555Shade(
                        standard.Lab, Lab,
                        Math.Max(Math.Abs((double)tolerance[ChromaticityField.dL].Lower), Math.Abs((double)tolerance[ChromaticityField.dL].Upper)),
                        Math.Max(Math.Abs((double)tolerance[ChromaticityField.da].Lower), Math.Abs((double)tolerance[ChromaticityField.da].Upper)),
                        Math.Max(Math.Abs((double)tolerance[ChromaticityField.db].Lower), Math.Abs((double)tolerance[ChromaticityField.db].Upper)));
                }
                else
                {
                    ShadeBy555 = Formula.Get555Shade(standard.Lab, Lab, 0.5, 0.5, 0.5);
                }
            }
        }

        /// <summary>
        /// 试样的判定结果，必须先调用Judge
        /// </summary>
        public Judgement Judgement { get; private set; }

        public double dL { get { return DeltaLab != null ? DeltaLab.dL: double.NaN; } }  // ΔL*

        public double da { get { return DeltaLab != null ? DeltaLab.da : double.NaN; } }  // Δa*

        public double db { get { return DeltaLab != null ? DeltaLab.db: double.NaN; } }  // Δb*

        
       // public double dab { get { return DeltaLab != null ? Math.Abs(Math.Abs(DeltaLab.da)-Math.Abs(DeltaLab.db)) : double.NaN; } }  // Δb*
        public double dC { get { return DeltaLab != null ? DeltaLab.dC : double.NaN; } }  // ΔC*

        //        public double dh { get { return DeltaLab != null ? DeltaLab.dh : double.NaN; } }  // Δh°

        public double dH { get { return DeltaLab != null ? DeltaLab.dH : double.NaN; } }  // ΔH*

        public double du { get { return DeltaLuv != null ? DeltaLuv.du : double.NaN; } }  // Δu*

        public double dv { get { return DeltaLuv != null ? DeltaLuv.dv : double.NaN; } }  // Δv*

        public double duq { get { return DeltaLuv != null ? DeltaLuv.duq : double.NaN; } }  // Δu*

        public double dvq { get { return DeltaLuv != null ? DeltaLuv.dvq : double.NaN; } }  // Δv*

        public double dX { get { return DeltaXyz != null ? DeltaXyz.dX : double.NaN; } }  // ΔX

        public double dY { get { return DeltaXyz != null ? DeltaXyz.dY : double.NaN; } }  // ΔY

        public double dZ { get { return DeltaXyz != null ? DeltaXyz.dZ : double.NaN; } }  // ΔZ

        public double dx { get { return DeltaXyz != null ? DeltaXyz.dx : double.NaN; } }  // Δx

        public double dy { get { return DeltaXyz != null ? DeltaXyz.dy : double.NaN; } }  // Δy

        public double dz { get { return DeltaXyz != null ? DeltaXyz.dz : double.NaN; } }  // Δz

        public double dR { get { return DeltaRgb != null ? DeltaRgb.dR : double.NaN; } }  // ΔR

        public double dG { get { return DeltaRgb != null ? DeltaRgb.dG : double.NaN; } }  // ΔG

        public double dB { get { return DeltaRgb != null ? DeltaRgb.dB : double.NaN; } }  // ΔB

        public double dHL { get { return DeltaHunterLab != null ? DeltaHunterLab.dL : double.NaN; } } // Hunter ΔL

        public double dHa { get { return DeltaHunterLab != null ? DeltaHunterLab.da : double.NaN; } } // Hunter Δa

        public double dHb { get { return DeltaHunterLab != null ? DeltaHunterLab.db : double.NaN; } } // Hunter Δb

        public double dEab { get { return DeltaLab != null ? DeltaLab.dE : double.NaN; } }        // ΔE*ab

        public double dE94 { get { return CIEDE1994 != null ? CIEDE1994.dE : double.NaN; } }        // ΔE*94

        public double dL94 { get { return CIEDE1994 != null ? CIEDE1994.dL : double.NaN; } }        // 计算ΔE*94的ΔL'

        public double dC94 { get { return CIEDE1994 != null ? CIEDE1994.dC : double.NaN; } }        // 计算ΔE*94的ΔC'

        public double dH94 { get { return CIEDE1994 != null ? CIEDE1994.dH : double.NaN; } }        // 计算ΔE*94的ΔH'

        public double dE00 { get { return CIEDE2000 != null ? CIEDE2000.dE : double.NaN; } }        // ΔE*00

        public double dL00 { get { return CIEDE2000 != null ? CIEDE2000.dL : double.NaN; } }        // 计算ΔE*00的 ΔL'

        public double dC00 { get { return CIEDE2000 != null ? CIEDE2000.dC : double.NaN; } }        // 计算ΔE*00的 ΔC'

        public double dH00 { get { return CIEDE2000 != null ? CIEDE2000.dH : double.NaN; } }        // 计算ΔE*00的 ΔH'

        public double dEcmc { get { return CMCDE != null ? CMCDE.dE : double.NaN; } }       // ΔEcmc(l:c)

        public double dLcmc { get { return CMCDE != null ? CMCDE.dL : double.NaN; } }       // 计算ΔEcmc的ΔL'

        public double dCcmc { get { return CMCDE != null ? CMCDE.dC : double.NaN; } }       // ΔEcmc的ΔC'

        public double dHcmc { get { return CMCDE != null ? CMCDE.dH : double.NaN; } }       // ΔEcmc的ΔH'

        public double dEcmc11 { get { return CMC11DE != null ? CMC11DE.dE : double.NaN; } }       // ΔEcmc(l:c)

        public double dLcmc11 { get { return CMC11DE != null ? CMC11DE.dL : double.NaN; } }       // 计算ΔEcmc的ΔL'

        public double dCcmc11 { get { return CMC11DE != null ? CMC11DE.dC : double.NaN; } }       // ΔEcmc的ΔC'

        public double dHcmc11 { get { return CMC11DE != null ? CMC11DE.dH : double.NaN; } }       // ΔEcmc的ΔH'

        public double dEcmc21 { get { return CMC21DE != null ? CMC21DE.dE : double.NaN; } }       // ΔEcmc(l:c)

        public double dLcmc21 { get { return CMC21DE != null ? CMC21DE.dL : double.NaN; } }       // 计算ΔEcmc的ΔL'

        public double dCcmc21 { get { return CMC21DE != null ? CMC21DE.dC : double.NaN; } }       // ΔEcmc的ΔC'

        public double dHcmc21 { get { return CMC21DE != null ? CMC21DE.dH : double.NaN; } }       // ΔEcmc的ΔH'

        public double dEuv { get { return DeltaLuv != null ? DeltaLuv.dE : double.NaN; } }        // ΔE*uv

        public double dHE { get { return DeltaHunterLab != null ? DeltaHunterLab.dE : double.NaN; } }         // Hunter 的 ΔE

        public double dL99 { get { return Din99 != null ? Din99.dL : double.NaN; } }  // ΔL99

        public double da99 { get { return Din99 != null ? Din99.da : double.NaN; } }  // Δa99

        public double db99 { get { return Din99 != null ? Din99.db : double.NaN; } }  // Δb99

        public double dC99 { get { return Din99 != null ? Din99.dC : double.NaN; } }  // ΔC99

        public double dH99 { get { return Din99 != null ? Din99.dH : double.NaN; } }  // ΔH99

        public double dE99
        {
            get { return Din99 != null ? Din99.dE : double.NaN; }
        }

        public double dMH => DeltaMunsell?.dMH ?? double.NaN;
        public double dMV => DeltaMunsell?.dMV ?? double.NaN;
        public double dMC => DeltaMunsell?.dMC ?? double.NaN;
        public double dLuminanceFactor { get; private set; } = double.NaN;

        public double StainingFastness_dEgs { get; protected set; }            // 沾色牢度
        public ColourFastnessLevel StainingFastness_SSRLevel { get; protected set; }       // 沾色牢度等级
        public double ColorFastness_dEf { get; protected set; }            // 变色牢度
        public ColourFastnessLevel ColorFastness_GSRLevel { get; protected set; }       // 变色牢度等级
        public double AverageStrength { get; protected set; }    // % Strength SUM
        public double PrecentStrengthSWL { get; protected set; } // % Strength SWL
        public double WeightedStrengthSWL { get; protected set; } // % Strength SWL

        /// <summary>
        /// % Strength SWL所在的最小波长位置
        /// </summary>
        public int WavelengthOfPrecentStrengthSWL { get; protected set; }
        /// <summary>
        /// 与第一参考光源和观察者角度的同色异谱指数
        /// </summary>
        public double MI1 { get; protected set; }
        /// <summary>
        /// 与第二参考光源和观察者角度的同色异谱指数
        /// </summary>
        public double MI2 { get; protected set; }

        /// <summary>
        /// 遮盖度
        /// </summary>
        public double Opacity { get; protected set; }

        /// <summary>
        /// 555色差分类
        /// </summary>
        public int ShadeBy555 { get; protected set; }

        /// <summary>
        /// 和标样反射率的差值
        /// </summary>
        public Spectrum ReflectanceDifferences
        {
            get; private set;
        }

        /// <summary>
        /// 试样的颜色偏向
        /// </summary>
        public ColorOffset ColorOffset
        {
            get; private set;
        }

        /// <summary>
        /// 8度光泽度之差
        /// </summary>
        public double DeltaGloss8 { get; private set; }

        public double DeltaTransmittanceC2 { get; private set; }

        public double DeltaTransmittanceA2 { get; private set; }

        public double DeltaTransmittanceD65 { get; private set; }

        public double DeltaHazeC2 { get; set; }

        public double DeltaHazeA2 { get; set; }

        public double DeltaHazeD65 { get; set; }
        public double DeltaClarity { get; private set; }
    }
}
