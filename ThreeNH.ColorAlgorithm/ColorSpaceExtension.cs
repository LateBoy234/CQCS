using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;
using ThreeNH.Data;

namespace ThreeNH.ColorAlgorithm
{
    public static class ColorSpaceExtension
    {
        private static int _minWavelength = 360;
        private static int _maxWavelength = 780;

        /// <summary>
        /// 获取反射率数据对应固定波长范围的数据列表
        /// </summary>
        /// <param name="spectralData"></param>
        /// <returns></returns>
        public static float[] GetWavelengthData(this ISpectralData spectralData)
        {
            List<float> list = new List<float>();
            var outrange = GetFixedWavelengthRange(spectralData.WavelengthInterval);
            for (int lambda = _minWavelength; lambda <= _maxWavelength; lambda += spectralData.WavelengthInterval)
            {
                float temp;
                var index = (lambda - _minWavelength) / spectralData.WavelengthInterval;
                if (lambda < spectralData.WavelengthBegin)
                    temp = spectralData[spectralData.WavelengthBegin];
                else if (lambda > spectralData.WavelengthEnd)
                    temp = spectralData[spectralData.WavelengthEnd];
                else
                    temp = spectralData[lambda];
                list.Add(temp);
            }
            return list.ToArray();
        }

        /// <summary>
        /// 获取反射率数据对应波长的下标
        /// </summary>
        /// <param name="spectralData"></param>
        /// <param name="wavelength"></param>
        /// <returns></returns>
        public static int WavelengthIndexOf(this ISpectralData spectralData, int wavelength)
        {
            var outrange = GetFixedWavelengthRange(spectralData.WavelengthInterval);
            return outrange.IndexOf(wavelength);
        }

        /// <summary>
        /// 反射率数据转CIEXYZ
        /// </summary>
        /// <param name="spectralData"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static CIEXYZ ToCIEXYZ(this ISpectralData spectralData, StandardIlluminant illuminant, StandardObserver observer)
        {
            Check.NotNull(spectralData, nameof(ArgumentNullException));
            var datas = GetTristimulusValues(illuminant, observer, spectralData.WavelengthInterval);
            var buffer = spectralData.GetWavelengthData();
            var outrange = new WavelengthRange(360, 780, spectralData.WavelengthInterval);
            float X = 0, Y = 0, Z = 0;
            for (int i = 0; i < outrange.Count; i++)
            {
                X += datas.Item1[i] * buffer[i];
                Y += datas.Item2[i] * buffer[i];
                Z += datas.Item3[i] * buffer[i];
            }
            var xyz = new CIEXYZ(X, Y, Z);
            xyz.Observer = observer;
            xyz.Illuminant = illuminant;
            return xyz;
        }

        /// <summary>
        /// ASTM E 313 Tint
		// 支持C、D65、D50光源的2度和10度观察者
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static double? GetAstmE313Tint(this CIEXYZ xyz)
        {
            if (xyz == null) return null;
            var illuminant = xyz.Illuminant;
            var observer = xyz.Observer;
            var T = float.NaN;
            if (illuminant == StandardIlluminant.C || illuminant == StandardIlluminant.D50 || illuminant == StandardIlluminant.D65)
            {
                float Tx = (observer == StandardObserver.CIE1931) ? 1000.0f : 900.0f;
                float Ty = 650.0f;
                var xyz0 = GetWhitePointData(illuminant, observer);
                var yxy0 = xyz0.ToCIEYxy();
                var yxy = xyz.Yxy;
                float WI = yxy.Y + 800.0f * (yxy0.x - yxy.x) + 1700.0f * (yxy0.y - yxy.y);
                T = Tx * (yxy0.x - yxy.x) - Ty * (yxy0.y - yxy.y);

                if (40.0f < WI && WI < (5.0f * yxy.Y - 280.0f) && -3.0 < T && T < 3.0f)
                {
                    T = WI;
                }
            }
            return T;
        }

        public static double? GetAstmE313Whiteness(this CIEXYZ xyz)
        {
            if (xyz == null) return null;
            var illuminant = xyz.Illuminant;
            var observer = xyz.Observer;
            var T = float.NaN;
            if (illuminant == StandardIlluminant.C || illuminant == StandardIlluminant.D50 || illuminant == StandardIlluminant.D65)
            {
                float Tx = (observer == StandardObserver.CIE1931) ? 1000.0f : 900.0f;
                float Ty = 650.0f;
                float WI, _T;
                var xyz0 = GetWhitePointData(illuminant, observer);
                var yxy0 = xyz0.ToCIEYxy();
                var yxy = xyz.ToCIEYxy();
                WI = yxy.Y + 800.0f * (yxy0.x - yxy.x) + 1700.0f * (yxy0.y - yxy.y);
                _T = Tx * (yxy0.x - yxy.x) - Ty * (yxy0.y - yxy.y);

                if (40.0f < WI && WI < (5.0f * yxy.Y - 280.0f) && -3.0 < _T && _T < 3.0f)
                {
                    T = WI;
                }
            }
            return T;
        }

        public static float? GetCIEISOTint(this CIEXYZ xyz)
        {
            var illuminant = xyz.Illuminant;
            var observer = xyz.Observer;
            if (illuminant != StandardIlluminant.D65) return float.NaN;
            float Tx = (observer == StandardObserver.CIE1931) ? 1000.0f : 900.0f;
            float Ty = 650.0f;
            float WI, _T;
            var T = float.NaN;
            var xyz0 = GetWhitePointData(illuminant, observer);
            var yxy0 = xyz0.ToCIEYxy();
            var yxy = xyz.ToCIEYxy();
            WI = yxy.Y + 800.0f * (yxy0.x - yxy.x) + 1700.0f * (yxy0.y - yxy.y);
            T = Tx * (yxy0.x - yxy.x) - Ty * (yxy0.y - yxy.y);

            if (40.0f < WI && WI < (5.0f * yxy.Y - 280.0f) && -3.0 < T && T < 3.0f)
            {
                T = WI;
            }
            return T;
        }

        /// <summary>
        ///坦伯白度
		/// 在A、D55、D65、C、D75光源下有效
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static float? GetTapplWhiteness(this CIEXYZ xyz)
        {
            if (get_AGB(xyz.Illuminant, xyz.Observer, out float fXA, out float fXB, out float fZB))
            {
                return 1.0f / fZB * xyz.Z;
            }
            return float.NaN;
        }
        /// <summary>
        /// 陶贝白度
		/// 在A、D55、D65、C、D75光源下有效
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static float? GetTaubeWhiteness(this CIEXYZ xyz)
        {
            if (get_AGB(xyz.Illuminant, xyz.Observer, out float fXA, out float fXB, out float fZB))
            {
                return 4.0f / fZB * xyz.Z - 3.0f * xyz.Y;
            }
            return float.NaN;
        }

        /// <summary>
        ///Hunter 白度（1942）
        ///D65/10和C/2光源下有效
        /// </summary>
        /// <param name="lab"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static double GetHunter1942Whiteness(this HunterLab lab, StandardIlluminant illuminant, StandardObserver observer)
        {

            if ((illuminant == StandardIlluminant.C && observer == StandardObserver.CIE1931) ||
                (illuminant == StandardIlluminant.D65 && observer == StandardObserver.CIE1964))
            {
                return 100.0f - Math.Sqrt(Math.Pow(100.0f - lab.L, 2) + Math.Pow(lab.a, 2) + Math.Pow(lab.b, 2));
            }
            return double.NaN;
        }

        /// <summary>
        /// 计算Hunter 1960白度指数
        /// 参数：
        /// [in] illuminant:		lab所在光源
        /// [in] observer:		lab所在观察者角度
        /// [in] lab:			要计算其白度指数的Hunter Lab值
        /// 返回：如果光源和观察者角度有效返回白度指数，否则返回NAN
        /// </summary>
        /// <param name="lab"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static double GetHunter1960Whiteness(this HunterLab lab, StandardIlluminant illuminant, StandardObserver observer)
        {
            if (lab == null) return float.NaN;
            if ((illuminant == StandardIlluminant.C && observer == StandardObserver.CIE1931) ||
                (illuminant == StandardIlluminant.D65 && observer == StandardObserver.CIE1964))
            {
                return lab.L - 3.0f * lab.b;
            }
            return float.NaN;
        }

        /// <summary>
        /// 计算R457白度；注意：R457仅在A光源下有效
        /// </summary>
        /// <param name="spectra">反射率数据</param>
        /// <returns></returns>
        public static double GetR457Whiteness(this ISpectralData spectra)
        {
            if (spectra == null) return double.NaN;
            // F(λ) 400~520nm的表
            float[] Flambda = new float[] {/* 0.0, 0.0, */ 1.0f, 6.7f, 18.2f, 34.5f, 57.6f, 82.5f, 100.0f, 88.7f, 53.1f, 20.3f, 5.6f, 0.3f, 0.0f };
            float Kb = 100.0f / 468.5f;
            float sum = 0.0f;
            int offset = spectra.WavelengthIndexOf(400);
            int i;
            for (i = 0; i < Flambda.Length; i++)
            {
                sum += Flambda[i] * spectra[offset + i];
            }
            return Kb * sum;
        }

        /// <summary>
        /// 计算ASTM D1925-1970黄度指数（仅在2度观察角C光源有效）
        /// </summary>
        /// <param name="xyz">CIEXYZ</param>
        /// <returns></returns>
        public static double GetAstmD1925Yellowness(this CIEXYZ xyz)
        {
            if (xyz == null) return float.NaN;
            var illuminant = xyz.Illuminant;
            var observer = xyz.Observer;
            if (illuminant == StandardIlluminant.C && observer == StandardObserver.CIE1931)
            {
                return 100.0f * (1.28f * xyz.X - 1.06f * xyz.Z) / xyz.Y;
            }
            return float.NaN;
        }

        /// <summary>
        /// 计算ASTM E313-2005黄度指数
        /// </summary>
        /// <param name="xyz">CIEXYZ</param>
        /// <returns></returns>
        public static double GetAstmE313Yellowness(this CIEXYZ xyz)
        {
            if (xyz == null) return float.NaN;
            var observer = xyz.Observer;
            var illuminant = xyz.Illuminant;
            float Cx, Cz;
            switch (observer)
            {
                case StandardObserver.CIE1931:
                    switch (illuminant)
                    {
                        case StandardIlluminant.C:
                            Cx = 1.2769f;
                            Cz = 1.0592f;
                            break;
                        case StandardIlluminant.D65:
                            Cx = 1.2985f;
                            Cz = 1.1335f;
                            break;
                        default:
                            return float.NaN;
                    }
                    break;
                case StandardObserver.CIE1964:
                    switch (illuminant)
                    {
                        case StandardIlluminant.C:
                            Cx = 1.2871f;
                            Cz = 1.0781f;
                            break;
                        case StandardIlluminant.D65:
                            Cx = 1.3013f;
                            Cz = 1.1498f;
                            break;
                        default:
                            return float.NaN;
                    }
                    break;
                default:
                    return float.NaN;
            }

            return (xyz.Y > 0.0f) ? 100.0f * (Cx * xyz.X - Cz * xyz.Z) / xyz.Y : float.NaN;
        }

        /// <summary>
        /// Color Value Sum
        /// </summary>
        /// <param name="spectralData"></param>
        /// <param name="isTransmittance"></param>
        /// <returns></returns>
        public static double GetColorValueSum(this ISpectralData spectralData, bool isTransmittance)
        {
            if (spectralData == null) return double.NaN;
            return clralgo_calculate_color_value_sum(spectralData, isTransmittance);
        }

        public static double GetMetamerismIndiex(this ISpectralData sample, ISpectralData target,
            StandardIlluminant illuminant, StandardObserver observer,
            StandardIlluminant refIlluminant, StandardObserver refObserver)
        {
            var xyz1 = sample.ToCIEXYZ(illuminant, observer);
            var xyz2 = target.ToCIEXYZ(illuminant, observer);
            var refxyz1 = sample.ToCIEXYZ(refIlluminant, refObserver);
            var refxyz2 = target.ToCIEXYZ(refIlluminant, refObserver);
            return clralgo_calculate_metamerism_index_by_xyz(
            illuminant, observer, xyz1, xyz2,
            refIlluminant, refObserver, refxyz1, refxyz2);
        }
        /// <summary>
        /// CIEXYZ转CIEYxy
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static CIEYxy ToCIEYxy(this CIEXYZ xyz)
        {
            var yxy = new CIEYxy();
            float tmp;
            tmp = xyz.X + xyz.Y + xyz.Z;
            if (tmp != 0.0f)
            {
                yxy.Y = xyz.Y;
                yxy.x = xyz.X / tmp;
                yxy.y = xyz.Y / tmp;
            }
            else
            {
                yxy.Y = yxy.x = yxy.y = 0.0f;
            }
            return yxy;
        }



        /// <summary>
        /// 某波长点的力份
        /// </summary>
        /// <param name="spectralData"></param>
        /// <param name="wavelength"></param>
        /// <param name="isTransmittance"></param>
        /// <returns></returns>
        public static double GetColorValueSWLAt(this ISpectralData spectralData, int wavelength, bool isTransmittance)
        {
            throw new Exception();
        }

        /// <summary>
        /// 获取指定wavelength处的% Strength
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="target"></param>
        /// <param name="wavelength"></param>
        /// <param name="transmissionFlag"></param>
        /// <returns></returns>
        public static double GetRelativeStrength(this ISpectralData sample, ISpectralData target, int wavelength, bool transmissionFlag)
        {
            throw new Exception();
        }



        /// <summary>
        /// 反射率转CIELAB
        /// </summary>
        /// <param name="spectralData"></param>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <returns></returns>
        public static CIELAB ToCIELAB(this ISpectralData spectralData, StandardIlluminant illuminant, StandardObserver observer)
        {
            var xyz = spectralData.ToCIEXYZ(illuminant, observer);
            return xyz.ToCIELab();
        }

        /// <summary>
        /// CIELAB转sRGB
        /// </summary>
        /// <param name="lab"></param>
        /// <returns></returns>
        public static sRGB TosRGB(this CIELAB lab)
        {
            var rgb = new sRGB();
            if (lab == null)
                return rgb;
            // 线性变换矩阵
            float[][] mat = new float[][] {
                               new float[] { 3.13405134140307f, -1.61702771078607f, -0.49065220978742f},
                               new float[] { -0.97876272987415f, 1.91614222843468f, 0.03344962851486f},
                               new float[] { 0.07194257711719f, -0.22897117960925f, 1.40521830532745f}
                            };


            CIEXYZ xyz = lab.ToCIEXYZ(StandardIlluminant.D65, StandardObserver.CIE1931);

            var r = srgb_convert(mat[0], xyz);
            var g = srgb_convert(mat[1], xyz);
            var b = srgb_convert(mat[2], xyz);

            return new sRGB(round_to(r * 255, 0), round_to(g * 255, 0), round_to(b * 255, 0));
        }

        public static CIEXYZ ToCIEXYZ(this CIELAB lab, StandardIlluminant illuminant, StandardObserver observer)
        {
            var xyz = new CIEXYZ();
            xyz.Illuminant = illuminant;
            xyz.Observer = observer;
            if (lab.L == 0.0f && lab.a == 0.0f && lab.b == 0.0f)
            {
                xyz.X = xyz.Y = xyz.Z = 0.0f;
            }
            else
            {
                var xyz0 = GetWhitePointData(illuminant, observer);
                float f_Y_Yn = (lab.L + 16.0f) / 116.0f;  // f(Y/Yn)
                float Y_Yn = f_inv(f_Y_Yn); // Y/Yn
                float X_Xn = f_inv(lab.a / 500.0f + f_Y_Yn); // X/Xn
                float Z_Zn = f_inv(f_Y_Yn - lab.b / 200.0f); // Z/Zn
                xyz.X = xyz0.X * X_Xn;
                xyz.Y = xyz0.Y * Y_Yn;
                xyz.Z = xyz0.Z * Z_Zn;
            }
            return xyz;
        }

        /// <summary>
        /// CIEXYZ转CIELAB
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static CIELAB ToCIELab(this CIEXYZ xyz)
        {
            if (xyz == null) return null;
            CIELAB lab = new CIELAB();
            if (xyz.X + xyz.Y + xyz.Z == 0.0f)
            {
                lab.L = lab.a = lab.b = 0.0f;
            }
            else
            {

                var xyz0 = GetWhitePointData(xyz.Illuminant, xyz.Observer);

                float f_Y = f(xyz.Y / xyz0.Y);
                float f_X = f(xyz.X / xyz0.X);
                float f_Z = f(xyz.Z / xyz0.Z);

                lab.L = 116.0f * f_Y - 16.0f;
                lab.a = 500.0f * (f_X - f_Y);
                lab.b = 200.0f * (f_Y - f_Z);
            }
            return lab;
        }



        /// <summary>
        /// CIEXYZ转CIELUV1976
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static CIELUV1976 ToCIELUV1976(this CIEXYZ xyz)
        {
            var luv = new CIELUV1976();
            if (xyz.X + xyz.Y + xyz.Z == 0.0f)
            {
                luv.L = luv.u = luv.v = 0.0f;
            }
            else
            {
                var xyz0 = GetWhitePointData(xyz.Illuminant, xyz.Observer);

                luv.L = 116.0f * f(xyz.Y / xyz0.Y) - 16.0f;
                luv.u = 13.0f * luv.L * (uq(xyz) - uq(xyz0));
                luv.v = 13.0f * luv.L * (vq(xyz) - vq(xyz0));
            }
            return luv;
        }

        /// <summary>
        /// CIEXYZ转HunterLab
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public static HunterLab ToHunterLab(this CIEXYZ xyz)
        {
            var lab = new HunterLab();
            if (xyz.X + xyz.Y + xyz.Z == 0.0f)
            {
                lab.L = lab.a = lab.b = 0.0f;
            }
            else
            {
                float Ka, Kb, sqrt_Y_Yn;
                var xyz0 = GetWhitePointData(xyz.Illuminant, xyz.Observer);
                get_KaKb(xyz.Illuminant, xyz.Observer, out Ka, out Kb);

                sqrt_Y_Yn = (float)Math.Sqrt(xyz.Y / xyz0.Y);
                lab.L = 100.0f * sqrt_Y_Yn;
                lab.a = Ka * ((xyz.X / xyz0.X) / sqrt_Y_Yn - sqrt_Y_Yn);
                lab.b = Kb * (sqrt_Y_Yn - (xyz.Z / xyz0.Z) / sqrt_Y_Yn);
            }
            return lab;
        }

        #region Private

        /// <summary>
        /// 根据光源，观察者，波长获取三色权重因子
        /// </summary>
        /// <param name="illuminant"></param>
        /// <param name="observer"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private static Tuple<float[], float[], float[]> GetTristimulusValues(StandardIlluminant illuminant, StandardObserver observer, int interval)
        {
            var datas = 5 / interval == 1 ?
                TristimulusWeightingFactorTable.TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_5NM_TABLEs[observer, illuminant].Factors :
                TristimulusWeightingFactorTable.TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_10NM_TABLEs[observer, illuminant].Factors;
            float[] buffer = new float[85];
            List<float> tmpX = new List<float>();
            List<float> tmpY = new List<float>();
            List<float> tmpZ = new List<float>();
            var outrange = new WavelengthRange(360, 780, interval);
            for (int lambda = 360; lambda <= 780; lambda += interval)
            {
                var index = (lambda - 360) / interval;
                tmpX.Add(datas[index, 0]);
                tmpY.Add(datas[index, 1]);
                tmpZ.Add(datas[index, 2]);
            }
            return new Tuple<float[], float[], float[]>(tmpX.ToArray(), tmpY.ToArray(), tmpZ.ToArray());
        }

        /// <summary>
        /// 获取固定波长范围
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        private static WavelengthRange GetFixedWavelengthRange(int interval)
        {
            return new WavelengthRange(_minWavelength, _maxWavelength, interval);
        }

        private static float f(float a)
        {
            if (a > (float)Math.Pow((24.0 / 116.0), 3))
                return (float)Math.Pow(a, (1.0 / 3.0));
            else
                return (841.0f / 108.0f * a + 16.0f / 116.0f);
        }

        private static float f_inv(float x)
        {
            if (x > 24.0f / 116.0f)
            {
                return (float)Math.Pow(x, 3);
            }
            else
            {
                return (x - 16.0f / 116.0f) / (841.0f / 108.0f);
            }
        }


        private static CIEXYZ GetWhitePointData(StandardIlluminant illuminant, StandardObserver observer)
        {
            return TristimulusWeightingFactorTable.TRISTIMULUS_WEIGHTING_WEIGHTING_FACTORS_10NM_TABLEs[observer, illuminant].WhitePortPosition;
        }


        private static float srgb_convert(float[] CL, CIEXYZ xyz)
        {
            float tmp = CL[0] * xyz.X + CL[1] * xyz.Y + CL[2] * xyz.Z;
            tmp = tmp / 100.0f;

            if (tmp * 12.92f <= 0.04045f)
                tmp = tmp * 12.92f;
            else
                tmp = 1.055f * (float)Math.Pow(tmp, 1.0 / 2.4) - 0.055f;
            if (tmp < 0.0) tmp = 0.0f;
            if (tmp > 1.0) tmp = 1.0f;
            return tmp;
        }

        private static byte round_to(float x, int precision)
        {
            float result = (int)x;
            float mult = (float)Math.Pow(10.0, precision);
            float temp = (x - result) * mult;
            float t = temp - (int)temp;

            if (t > 0.5f)
                temp += 0.5f;
            else if (t < -0.5f)
                temp -= 0.5f;
            return (byte)(result + ((int)temp) / mult);
        }

        // 获取HunterLab和CIEXYZ互相转换的系数
        private static void get_KaKb(StandardIlluminant illuminant, StandardObserver observer, out float Ka, out float Kb)
        {

            if (observer == StandardObserver.CIE1931)
            {
                switch (illuminant)
                {
                    case StandardIlluminant.A:
                        Ka = 185.20f;
                        Kb = 38.40f;
                        break;
                    case StandardIlluminant.C:
                        Ka = 175.00f;
                        Kb = 70.00f;
                        break;
                    case StandardIlluminant.D50:
                        Ka = 173.51f;
                        Kb = 58.48f;
                        break;
                    case StandardIlluminant.D65:
                        Ka = 172.30f;
                        Kb = 67.20f;
                        break;
                    case StandardIlluminant.D75:
                        Ka = 172.22f;
                        Kb = 71.30f;
                        break;
                    case StandardIlluminant.F2:
                        Ka = 175.00f;
                        Kb = 52.90f;
                        break;
                    case StandardIlluminant.F11:
                        Ka = 178.00f;
                        Kb = 52.30f;
                        break;
                    case StandardIlluminant.F12:
                        Ka = 183.70f;
                        Kb = 37.50f;
                        break;
                    default:
                        {
                            CIEXYZ xyz = GetWhitePointData(illuminant, observer);
                            Ka = 175.0f / 198.04f * (xyz.X + xyz.Y);
                            Kb = 70.0f / 218.11f * (xyz.Y + xyz.Z);
                        }
                        break;
                }
            }
            else
            {
                switch (illuminant)
                {
                    case StandardIlluminant.A:
                        Ka = 186.30f;
                        Kb = 38.20f;
                        break;
                    case StandardIlluminant.C:
                        Ka = 174.30f;
                        Kb = 69.40f;
                        break;
                    case StandardIlluminant.D50:
                        Ka = 173.82f;
                        Kb = 58.13f;
                        break;
                    case StandardIlluminant.D65:
                        Ka = 172.10f;
                        Kb = 66.70f;
                        break;
                    case StandardIlluminant.D75:
                        Ka = 171.76f;
                        Kb = 70.76f;
                        break;
                    case StandardIlluminant.F2:
                        Ka = 178.60f;
                        Kb = 53.60f;
                        break;
                    case StandardIlluminant.F11:
                        Ka = 180.10f;
                        Kb = 52.70f;
                        break;
                    case StandardIlluminant.F12:
                        Ka = 186.30f;
                        Kb = 38.20f;
                        break;
                    default:
                        {
                            var xyz = GetWhitePointData(illuminant, observer);
                            Ka = 175.0f / 198.04f * (xyz.X + xyz.Y);
                            Kb = 70.0f / 218.11f * (xyz.Y + xyz.Z);
                        }
                        break;
                }
            }
        }

        // 计算u'的公式
        private static float uq(CIEXYZ xyz)
        {
            return 4.0f * xyz.X / (xyz.X + 15.0f * xyz.Y + 3 * xyz.Z);
        }

        // 计算v'的公式
        private static float vq(CIEXYZ xyz)
        {
            return 9.0f * xyz.Y / (xyz.X + 15.0f * xyz.Y + 3 * xyz.Z);
        }

        // 获取X、Y、Z与A、G、B之间的变换系数
        private static bool get_AGB(StandardIlluminant illuminant,
                     StandardObserver observer,
                    out float fXA, out float fXB, out float fZB)
        {
            fXA = float.NaN;
            fXB = float.NaN;
            fZB = float.NaN;
            float[][] cie1937 = new float[][] {
                new float[] { 1.0447f, 0.0539f, 0.3558f },  // A
                new float[]{ 0.8061f, 0.1504f, 0.9209f },	 // D55
                new float[]{ 0.7701f, 0.1804f, 1.0889f },	 // D65
                new float[]{ 0.7446f, 0.2047f, 1.2256f },	 // D75
                new float[]{ 0.7832f, 0.1975f, 1.1822f }    // C
            };
            float[][] cie1964 = new float[][] {
                new float[] { 1.0571f, 0.0544f, 0.3520f },  // A
                new float[] { 0.8078f, 0.1502f, 0.9098f },  // D55
                new float[] { 0.7683f, 0.1798f, 1.0733f },  // D65
                new float[]{ 0.7405f, 0.2038f, 1.2073f },  // D75
                new float[]{ 0.7772f, 0.1957f, 1.1614f }   // C
            };

            int index = 0;
            switch (illuminant)
            {
                case StandardIlluminant.A: index = 0; break;
                case StandardIlluminant.D55: index = 1; break;
                case StandardIlluminant.D65: index = 2; break;
                case StandardIlluminant.D75: index = 3; break;
                case StandardIlluminant.C: index = 4; break;
                default: return false;
            }

            if (observer == StandardObserver.CIE1931)
            {
                fXA = cie1937[index][0];
                fXB = cie1937[index][1];
                fZB = cie1937[index][2];
            }
            else
            {
                fXA = cie1964[index][0];
                fXB = cie1964[index][1];
                fZB = cie1964[index][2];
            }
            return true;
        }

        // 根据样品反射率计算 Color Value SUM。具体算法参考HunterLab的《Strength Calculations》
        // 参数：
        // [in] spectra: 反射率光谱
        // 返回：Color Value SUM
        private static float clralgo_calculate_color_value_sum(ISpectralData spectra, bool isTransmittance)
        {
            int i = 0;
            double sum = 0.0f;
            for (i = 0; i < spectra.Count; i++)
            {
                if (isTransmittance)
                    sum += absorbance(spectra[i]);
                else
                    K_S(spectra[i]);
            }

            return (float)(sum / spectra.Count);
        }

        // 根据透射率计算光学吸收率
        // 注意吸收率不能是百分比
        private static float absorbance(float transmittance)
        {
            // if (transmittance < 0.000001)
            // 	transmittance = 0.000001;
            return -(float)Math.Log10(transmittance);
        }
        // 计算反射率的K/S值，具體算法參考《The Kubelka-Monk Theory and K/S》
        static float K_S(float r)
        {
            // if (r < 0.000001)
            // 	r = 0.000001;
            return (1.0f - r) * (1.0f - r) / (2.0f * r);
        }

        // 通过XYZ值计算同色异谱指数
        // 参数：
        // [in]	illuminant: xyz1和xyz2的测试光源和观察者角度
        // [in] observer:	xyz1和xyz2的测试光源和观察者角度
        // [in] xyz1:		样品1的XYZ值
        // [in] xyz2:		样品2的XYZ值
        // [in] ref_illuminant:	参考光源
        // [in] ref_observer:	参考观察者角度
        // [in] xyz1_ref:	样品1在参考条件下测得的XYZ值
        // [in] xyz2_ref:	样品2在参考条件下测得的XYZ值
        // 返回：样品1和样品2的同色异谱指数
        private static float clralgo_calculate_metamerism_index_by_xyz(StandardIlluminant illuminant, StandardObserver observer,
            CIEXYZ xyz1, CIEXYZ xyz2, StandardIlluminant ref_illuminant, StandardObserver ref_observer, CIEXYZ xyz1_ref, CIEXYZ xyz2_ref)
        {
            float fx = xyz1.X != 0 ? xyz2.X / xyz1.X : 1.0f;
            float fy = xyz1.Y != 0 ? xyz2.Y / xyz1.Y : 1.0f;
            float fz = xyz1.Z != 0 ? xyz2.Z / xyz1.Z : 1.0f;

            var tmp = new CIEXYZ()
            {
                X = xyz1_ref.X * fx,
                Y = xyz1_ref.Y * fy,
                Z = xyz1_ref.Z * fz,
                Illuminant = ref_illuminant,
                Observer = ref_observer
            };
            var lab1 = tmp.ToCIELab();
            var lab2 = xyz2_ref.ToCIELab();

            return clralgo_calculate_dEab(lab1, lab2);
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
        #endregion

    }
}
