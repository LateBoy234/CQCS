using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm
{
    public static class ColorIndexExtension
    {
        /// <summary>
        /// 通过XYZ值计算同色异谱指数
        /// 参数：
        /// [in]	illuminant: xyz1和xyz2的测试光源和观察者角度
        /// [in] observer:	xyz1和xyz2的测试光源和观察者角度
        /// [in] xyz1:		样品1的XYZ值
        /// [in] xyz2:		样品2的XYZ值
        /// [in] ref_illuminant:	参考光源
        /// [in] ref_observer:	参考观察者角度
        /// [in] xyz1_ref:	样品1在参考条件下测得的XYZ值
        /// [in] xyz2_ref:	样品2在参考条件下测得的XYZ值
        /// 返回：样品1和样品2的同色异谱指数
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="test"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <param name="refIlluminant"></param>
        /// <param name="refObserver"></param>
        /// <returns></returns>
        public static double GetMetamerismIndiex(ISpectralData standard, ISpectralData test,
            StandardIlluminant illuminant, StandardObserver observer,
            StandardIlluminant refIlluminant, StandardObserver refObserver)
        {
            var xyz1 = standard.ToCIEXYZ(illuminant, observer);
            var xyz2 = test.ToCIEXYZ(illuminant, observer);
            var refxyz1 = standard.ToCIEXYZ(refIlluminant, refObserver);
            var refxyz2 = test.ToCIEXYZ(refIlluminant, refObserver);
            float fx = xyz1.X != 0 ? xyz2.X / xyz1.X : 1.0f;
            float fy = xyz1.Y != 0 ? xyz2.Y / xyz1.Y : 1.0f;
            float fz = xyz1.Z != 0 ? xyz2.Z / xyz1.Z : 1.0f;

            var tmp = new CIEXYZ
            {
                X = refxyz1.X * fx,
                Y = refxyz1.Y * fy,
                Z = refxyz1.Z * fz,
                Illuminant = refIlluminant,
                Observer = refObserver
            };
            var lab1 = tmp.ToCIELab();
            var lab2 = refxyz2.ToCIELab();

            return clralgo_calculate_dEab(lab1, lab2);
        }

        /// <summary>
        /// 沾色牢度的dEgs
		/// C/2, D65/10 有效
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="test"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static double GetStainingFastness_dEgs(CIELAB standard, CIELAB test, StandardIlluminant illuminant, StandardObserver observer)
        {
            if (clralgo_calculate_ssr(illuminant, observer, test, standard, out float ssr, out float dEgs)
                != ColorFastnessGrade.clralgo_COLOR_FASTNESS_UNDEFINED)
            {
                return dEgs;
            }
            return float.NaN;
        }

        /// <summary>
        /// 沾色牢度级数
		/// C/2, D65/10 有效
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="test"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static ColourFastnessLevel GetReportedSSR(CIELAB standard, CIELAB test, StandardIlluminant illuminant, StandardObserver observer)
        {
            var grade = clralgo_calculate_ssr(illuminant, observer, test, standard, out float ssr, out float de);
            return (ColourFastnessLevel)grade;
        }

        /// <summary>
        /// 变色牢度级数
		/// D65和C有效
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="test"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static ColourFastnessLevel GetReportedGSR(CIELAB standard, CIELAB test, StandardIlluminant illuminant, StandardObserver observer)
        {
            var grade = clralgo_calculate_gsr(illuminant, observer, test, standard, out float ssr, out float edg);
            return (ColourFastnessLevel)grade;
        }

        /// <summary>
        /// 变色牢度的ΔEf
		/// D65和C有效
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="test"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static double GetColorFastness_dEf(CIELAB standard, CIELAB test, StandardIlluminant illuminant, StandardObserver observer)
        {
            var grade = clralgo_calculate_gsr(illuminant, observer, test, standard, out float ssr, out float def);
            return grade == ColorFastnessGrade.clralgo_COLOR_FASTNESS_UNDEFINED ? float.NaN : def;
        }

        /// <summary>
        /// 这盖度
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static float GetOpacity(this CIEXYZ sample, CIEXYZ target)
        {
            return clralgo_calculate_opacity(sample.Y, target.Y);
        }

        /// <summary>
        /// Strength SWL
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="target"></param>
        /// <param name="isTransmittance"></param>
        /// <param name="wavelengthOfMaximumAbsorption"></param>
        /// <returns></returns>
        public static double GetStrengthSWL(this ISpectralData sample, ISpectralData target, bool isTransmittance, out int wavelengthOfMaximumAbsorption)
        {
            return clralgo_calculate_strength_swl(sample, target, isTransmittance, out wavelengthOfMaximumAbsorption);
        }

        /// <summary>
        /// Color Value SWL
        /// </summary>
        /// <param name="spectralData"></param>
        /// <param name="isTransmittance"></param>
        /// <param name="wavelengthOfMaximumAbsorption"></param>
        /// <returns></returns>
        public static double GetColorValueSWL(this ISpectralData spectralData, bool isTransmittance, out int wavelengthOfMaximumAbsorption)
        {
            if (spectralData == null)
            {
                wavelengthOfMaximumAbsorption = 0;
                return double.NaN;
            }
            return clralgo_calculate_color_value_swl(spectralData, isTransmittance, out wavelengthOfMaximumAbsorption);
        }

        /// <summary>
        /// Color Value WSUM
        /// </summary>
        /// <param name="spectralData"></param>
        /// <param name="isTransmittance"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static double GetColorValueWSUM(this ISpectralData spectralData, bool isTransmittance, StandardIlluminant illuminant, StandardObserver observer)
        {
            return clralgo_calculate_color_value_wsum(illuminant, observer, spectralData, isTransmittance);
        }

        /// <summary>
        /// Average Strength
        /// </summary>
        /// <param name="test"></param>
        /// <param name="standard"></param>
        /// <param name="isTransmittance"></param>
        /// <returns></returns>
        public static double GetAverageStrength(this ISpectralData test, ISpectralData standard, bool isTransmittance)
        {
            return clralgo_calculate_average_strength(test, standard, isTransmittance);
        }

        /// <summary>
        /// Weighted Strength
        /// </summary>
        /// <param name="test"></param>
        /// <param name="standard"></param>
        /// <param name="isTransmittance"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static double GetWeightedStrength(this ISpectralData test, ISpectralData standard, bool isTransmittance, StandardIlluminant illuminant, StandardObserver observer)
        {
            return clralgo_calculate_weighted_strength(illuminant, observer, test, standard, isTransmittance);
        }

        #region Private


        /// <summary>
        /// 计算遮盖度
        /// </summary>
        /// <param name="Ywhite">白底样品的Y值</param>
        /// <param name="Yblack">黑底样品的Y值</param>
        /// <returns></returns>
        private static float clralgo_calculate_opacity(float Ywhite, float Yblack)
        {
            return Ywhite / Yblack;
        }

        // 计算 %Strength-SWL，算法参考《Strength Calculations》
        // 参数：
        // [in] test:		试样光谱数据
        // [in] standard:	标样光谱数据
        // [out, opt] wavelength_of_maximum_absorption: 如果不为空存储标样最大吸收波长
        // 返回：%Strength-SWL
        private static float clralgo_calculate_strength_swl(this ISpectralData test, ISpectralData standard, bool isTransmittance, out int wavelength_of_maximum_absorption)
        {
            int wave;
            float SWLstd, SWLtest;
            SWLstd = clralgo_calculate_color_value_swl(standard, isTransmittance, out wave);
            SWLtest = !isTransmittance ? K_S(test[wave]) : absorbance(test[wave]);
            wavelength_of_maximum_absorption = wave;

            return SWLtest / SWLstd * 100.0f;
        }

        // 计算 Color Value SWL，即最小反射率的K/S；具体算法参考HunterLab的《Strength Calculations》
        // 参数：
        // [in] spectra:					 反射率光谱
        // [out, opt] wavelength_of_maximum_absorption: 如果不为NULL存储取大吸收波长
        // 返回：Color Value SWL，即最小反射率的K/S
        private static float clralgo_calculate_color_value_swl(ISpectralData spectra, bool isTransmittance, out int wavelength_of_maximum_absorption)
        {
            int i;
            int minI = 0;
            float minR = spectra[0];

            // 只计算400nm到700nm之间的

            for (i = 1; i < spectra.Count; i++)
            {
                if (spectra[i] < minR)
                {
                    minR = spectra[i];
                    minI = i;
                }
            }

            wavelength_of_maximum_absorption = spectra.WavelengthBegin + minI * spectra.WavelengthInterval;
            return !isTransmittance ? K_S(minR) : absorbance(minR);
        }

        // 计算反射率的K/S值，具體算法參考《The Kubelka-Monk Theory and K/S》
        private static float K_S(float r)
        {
            // if (r < 0.000001)
            // 	r = 0.000001;
            return (1.0f - r) * (1.0f - r) / (2.0f * r);
        }

        // 根据透射率计算光学吸收率
        // 注意吸收率不能是百分比
        static float absorbance(float transmittance)
        {
            // if (transmittance < 0.000001)
            // 	transmittance = 0.000001;
            return -(float)Math.Log10(transmittance);
        }

        // 使用ΔE*ab计算lab1 - lab2的色差
        // 参数：
        // [in] lab1: 样品1的Lab值
        // [in] lab2: 样品2的Lab值
        // 返回：ΔE*ab
        private static float clralgo_calculate_dEab(CIELAB lab1, CIELAB lab2)
        {
            float dL = lab1.L - lab2.L;
            float da = lab1.a - lab2.a;
            float db = lab1.b - lab2.b;

            return (float)Math.Sqrt(dL * dL + da * da + db * db);
        }


        // 计算沾色牢度
        // 参数：
        // [in] illuminant:		standard和test使用的标准光源
        // [in] observer:		standard和test使用的标准观察者角度
        // [in] test:			试样的Lab值
        // [in] standard:		标样的Lab值
        // [out, opt] ssr:		如果不为空存储连续的色牢度等级
        // [out, opt] dEgs:		如果不为空存储ΔEgs
        // 返回：沾色牢度；如果指定条件无效返回clralgo_COLOR_FASTNESS_UNDEFINED
        private static ColorFastnessGrade clralgo_calculate_ssr(
        StandardIlluminant illuminant,
        StandardObserver observer,
        CIELAB test,
        CIELAB standard,
       out float _ssr, out float _dEgs)
        {
            _ssr = float.NaN;
            _dEgs = float.NaN;
            if ((observer == StandardObserver.CIE1931 && illuminant == StandardIlluminant.C)
                    || (observer == StandardObserver.CIE1964 && illuminant == StandardIlluminant.D65))
            {
                float dL, dE;

                dL = test.L - standard.L;
                dE = clralgo_calculate_dEab(test, standard);
                _dEgs = dE - 0.4f * (float)Math.Sqrt(dE * dE - dL * dL);
                _dEgs = (float)round_to(_dEgs, 2);
                _ssr = _dEgs >= 4.26f ? (6.1f - 1.45f * (float)Math.Log(_dEgs)) : (5.0f - 0.23f * _dEgs);
                _ssr = (float)round_to(_ssr, 2);

                return float_fastness_grade_to_enum(_ssr);
            }

            return ColorFastnessGrade.clralgo_COLOR_FASTNESS_UNDEFINED;
        }

        private static ColorFastnessGrade float_fastness_grade_to_enum(float fgrade)
        {
            int ival;
            float fval;

            if (fgrade < 1.25)
                return ColorFastnessGrade.clralgo_GRADE_1;
            if (fgrade >= 4.75)
                return ColorFastnessGrade.clralgo_GRADE_5;

            ival = (int)fgrade;
            fval = fgrade - ival;
            if (fval >= 0.25 && fval < 0.75)
                return (ColorFastnessGrade)(ival * 2);
            if (fval >= 0.75)
                ival += 1;
            return (ColorFastnessGrade)(ival * 2 - 1);
        }

        // 将小数约分为整数
        //int round(double x);
        // 将小数保留precision位小数点
        private static double round_to(double x, int precision)
        {
            double result = (int)x;
            double mult = (float)Math.Pow(10.0, precision);
            double temp = (x - result) * mult;
            double t = temp - (int)temp;

            if (t > 0.5)
                temp += 0.5;
            else if (t < -0.5)
                temp -= 0.5;
            return result + ((int)temp) / mult;
        }


        // 计算变色牢度
        // 参数：
        // [in] illuminant:		standard和test使用的标准光源
        // [in] observer:		standard和test使用的标准观察者角度
        // [in] test:			试样的Lab值
        // [in] standard:		标样的Lab值
        // [out, opt] gsr:		如果不为空存储连续的色牢度等级
        // [out, opt] dEf:		如果不为空存储ΔEf
        // 返回：变色牢度；如果指定条件无效返回clralgo_COLOR_FASTNESS_UNDEFINED
        private static ColorFastnessGrade clralgo_calculate_gsr(
        StandardIlluminant illuminant,
        StandardObserver observer,
        CIELAB test,
        CIELAB standard,
       out float _gsr, out float _dEf)
        {
            _gsr = float.NaN;
            _dEf = float.NaN;
            if (illuminant == StandardIlluminant.D65 || illuminant == StandardIlluminant.C)
            {
                float dL, dC, dH, Cm, X, D, dCk, dHk, dCf, dHf;
                var lch_test = test.ToLCh();
                var lch_std = standard.ToLCh();
                clralgo_calculate_dLCH(lch_test, lch_std, out dL, out dC, out dH);

                Cm = (lch_test.C + lch_std.C) / 2;
                X = (float)Math.Pow((360.0f - Math.Abs(average_degree(lch_test.h, lch_std.h) - 280.0f)) / 30.0f, 2);
                D = dC * Cm * (float)Math.Exp(-X) / 100.0f;
                dCk = dC - D;
                dHk = dH - D;
                dCf = dCk / (1.0f + (float)Math.Pow(20.0f * Cm / 1000.0f, 2));
                dHf = dHk / (1.0f + (float)Math.Pow(10.0f * Cm / 1000.0f, 2));
                _dEf = (float)Math.Sqrt(dL * dL + dCf * dCf + dHf * dHf);
                _dEf = (float)round_to(_dEf, 2);

                _gsr = _dEf > 3.40f ? (float)(5.0f - Math.Log10(_dEf / 0.85f) / Math.Log10(2.0f)) : (5.0f - _dEf / 1.7f);
                _gsr = (float)round_to(_gsr, 2);

                return float_fastness_grade_to_enum(_gsr);
            }

            return ColorFastnessGrade.clralgo_COLOR_FASTNESS_UNDEFINED;
        }

        // 计算lch1 - lch2的ΔL*、ΔC*、ΔH*
        // 参数：
        // [in] lch1: 样品1的LCh值
        // [in] lch2: 样品2的LCh值
        // [out] dL, dC, dH: 用于存储 ΔL*、ΔC*、ΔH*
        private static void clralgo_calculate_dLCH(CIELCh lch1, CIELCh lch2, out float dL, out float dC, out float dH)
        {
            float dh;

            dL = lch1.L - lch2.L;
            dC = lch1.C - lch2.C;
            dh = lch1.h - lch2.h;
            if (dh > 180.0f)
                dh -= 360.0f;
            else if (dh < -180.0f)
                dh += 360.0f;
            dH = 2.0f * (float)Math.Sqrt(lch1.C * lch2.C) * (float)Math.Sin(RADIANS(dh / 2.0f));
        }

        // 将角度转换为弧度
        private static float RADIANS(float degrees)
        {
            return ((float)((degrees) / 180.0 * Math.PI));
        }

        // 求两个角度的平均值
        static float average_degree(float a, float b)
        {
            if (Math.Abs(a - b) > 180)
            {
                return (a + b < 360) ? (a + b) / 2 + 180 : (a + b) / 2 - 180;
            }
            return (a + b) / 2;
        }

        private static float clralgo_calculate_color_value_wsum(StandardIlluminant illuminant, StandardObserver observer,
             ISpectralData spectra,
             bool isTransmittance)
        {
            int i;
            double sum;

            // 只计算400nm到700nm之间的

            sum = 0.0;
            for (i = 0; i < spectra.Count; i++)
            {
                int wavelength = spectra.WavelengthBegin + i * spectra.WavelengthInterval;
                var range = new WavelengthRange(400, 780, 5);
                ColorMatchingFunction s = ColorMatchingFunctions.GetByObserver(observer, range)[wavelength];
                float e = get_light_energy(EnergyDistributionTables.Tables[illuminant], wavelength);
                if (isTransmittance)
                    sum += absorbance(spectra[i]) * e * (s.x + s.y + s.z);
                else
                    sum += K_S(spectra[i]) * e * (s.x + s.y + s.z);
            }
            return (float)(sum / spectra.Count);
        }

        private static ColorMatchingFunction get_color_matching_function(ColorMatchingFunctionTable table, int wavelength)
        {

            ColorMatchingFunction s_zero_color_matching_function = new ColorMatchingFunction { x = 0.0f, y = 0.0f, z = 0.0f };

            if (wavelength >= table.minimum && wavelength <= table.maximum)
            {
                return table.functions[(wavelength - table.minimum) / table.interval];
            }
            return s_zero_color_matching_function;
        }

        private static float get_light_energy(EnergyDistributionTable table, int wavelength)
        {
            if (wavelength >= table.Minimum && wavelength <= table.Maximum)
            {
                return table.data[(wavelength - table.Minimum) / table.Interval];
            }
            return 0.0f;
        }

        // 计算Average Strength（也叫做 % Strength SUM）；具体算法参考HunterLab的《Strength Calculations》
        // 参数：
        // [in] test:		试样光谱数据
        // [in] standard:	标样光谱数据
        // 返回：Average Strength（也叫做 % Strength SUM）
        private static float clralgo_calculate_average_strength(ISpectralData test, ISpectralData standard, bool isTransmittance)
        {
            double trialSum, stdSum;
            int count;
            int wavelength;
            //return clralgo_calculate_color_value_sum(test, isTransmittance) / clralgo_calculate_color_value_sum(standard, isTransmittance) * 100.0f;

            trialSum = 0;
            stdSum = 0;
            count = (700 - 400) / standard.WavelengthInterval + 1;
            for (wavelength = 400; wavelength <= 700; wavelength += standard.WavelengthInterval)
            {
                if (isTransmittance)
                {
                    trialSum += K_S(test[wavelength]);
                    stdSum += K_S(standard[wavelength]);
                }
                else
                {
                    trialSum += absorbance(test[wavelength]);
                    stdSum += absorbance(standard[wavelength]);
                }
            }

            return (float)(trialSum / stdSum * 100.0);
        }

        private static float clralgo_calculate_weighted_strength(StandardIlluminant illuminant, StandardObserver observer, ISpectralData trialSpectra, ISpectralData stdSpectra, bool isTransmittance)
        {

            float[,] weights;
            int wavelength;
            double trialSum, stdSum;

            if (stdSpectra.WavelengthInterval == 5)
            {
                weights = TristimulusWeightingFactorTable.TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_5NM_TABLEs[observer, illuminant].Factors;
            }
            else if (stdSpectra.WavelengthInterval == 10)
            {
                weights = TristimulusWeightingFactorTable.TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_10NM_TABLEs[observer, illuminant].Factors;
            }

            else
            {
                // 只接受5nm和10nm的
                return float.NaN;
            }

            // 只计算400nm到700nm之间的
            trialSum = 0.0;
            stdSum = 0.0;
            for (wavelength = 400; wavelength <= 700; wavelength += stdSpectra.WavelengthInterval)
            {
                int index = (wavelength - 360) / stdSpectra.WavelengthInterval;
                float t = weights[index * 3, 0] + weights[index * 3, 1] + weights[index * 3, 2];

                if (isTransmittance)
                {
                    trialSum += absorbance(trialSpectra[wavelength]) * t;
                    stdSum += absorbance(stdSpectra[wavelength]) * t;
                }
                else
                {
                    trialSum += K_S(trialSpectra[wavelength]) * t;
                    stdSum += K_S(stdSpectra[wavelength]) * t;
                }

            }

            return (float)(trialSum / stdSum * 100.0);
        }

        #endregion

    }
}
