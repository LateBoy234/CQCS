using System;
using System.Collections.Generic;
using System.ComponentModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorAlgorithm
{
    // 色牢度等级
    public enum ColourFastnessLevel
    {
        [Description("5")]
        LEVEL_5,    // 5
        [Description("4-5")]
        LEVEL_4_5,  // 4-5
        [Description("4")]
        LEVEL_4,    // 4
        [Description("3-4")]
        LEVEL_3_4,  // 3-4
        [Description("3")]
        LEVEL_3,    // 3
        [Description("2-3")]
        LEVEL_2_3,  // 2-3
        [Description("2")]
        LEVEL_2,    // 2
        [Description("1-2")]
        LEVEL_1_2,  // 1-2
        [Description("1")]
        LEVEL_1,     // 1
        [Description("--")]
        INVALID     // 无效的
    }

    public class FormulaCS
    {
        WavelengthRange _spectrumRange;
        StandardObserver _observer;
        StandardIlluminant _illuminant;
        TristimulusValues _tristimulusFunc;

        /// <summary>
        /// 以380到740間隔10nm的波長,D65光源,10度觀察者角度構建一個Formula對象
        /// </summary>
        public FormulaCS() : this(new WavelengthRange(380, 740, 10),
            StandardIlluminant.D65, StandardObserver.CIE1964) { }
        /// <summary>
        /// 用指定的光源和觀察者角度構造一個Formula對象
        /// </summary>
        /// <param name="illuminant">標準光源</param>
        /// <param name="observer">觀察者角度</param>
        public FormulaCS(StandardIlluminant illuminant, StandardObserver observer)
            : this(new WavelengthRange(380, 740, 5), illuminant, observer) { }
        /// <summary>
        /// 以指定的光源、觀察者角度、波長范圍構建一個Formula對象
        /// </summary>
        /// <param name="spectrumRange">波長范圍</param>
        /// <param name="illuminant">標準光源</param>
        /// <param name="observer">標準觀察者角度</param>
        public FormulaCS(WavelengthRange spectrumRange, StandardIlluminant illuminant,
            StandardObserver observer)
        {
            _spectrumRange = spectrumRange;
            _observer = observer;
            _illuminant = illuminant;
            resetTristimulusFunctions();
        }

        /// <summary>
        /// 重設實例的波長范圍、光源、觀察者角度
        /// </summary>
        /// <param name="spectrumRange">波長范圍</param>
        /// <param name="illuminant">光源</param>
        /// <param name="observer">觀察者角度</param>
        public void Reset(WavelengthRange spectrumRange,
            StandardIlluminant illuminant, StandardObserver observer)
        {
            if (_spectrumRange != spectrumRange ||
                _observer != observer || _illuminant != illuminant)
            {
                _spectrumRange = spectrumRange;
                _observer = observer;
                _illuminant = illuminant;
                resetTristimulusFunctions();
            }
        }
        /// <summary>
        /// 當前的標準觀察者角度
        /// </summary>
        public StandardObserver Observer
        {
            get { return _observer; }
            set
            {
                if (value != _observer)
                {
                    _observer = value;
                    resetTristimulusFunctions();
                }
            }
        }
        /// <summary>
        /// 當前的光源
        /// </summary>
        public StandardIlluminant Illuminant
        {
            get { return _illuminant; }
            set
            {
                if (value != _illuminant)
                {
                    _illuminant = value;
                    resetTristimulusFunctions();
                }
            }
        }
        /// <summary>
        /// 當前的波長范圍
        /// </summary>
        public WavelengthRange SpectrumRange
        {
            get { return _spectrumRange; }
            set
            {
                if (value != _spectrumRange)
                {
                    _spectrumRange = value;
                    //resetTristimulusFunctions();
                }
            }
        }
        /// <summary>
        /// 使用指定的光源和觀察者角度根據反射率數據計算Lab值
        /// </summary>
        /// <param name="spectrum">反射率數據</param>
        /// <param name="illuminant">使用的標準光源</param>
        /// <param name="observer">使用的觀察者角度</param>
        /// <returns></returns>
        public static CIELABCS CalculateLab(SpectrumCS spectrum,
            StandardIlluminant illuminant, StandardObserver observer)
        {
            FormulaCS formula = new FormulaCS(spectrum.Range, illuminant, observer);
            return formula.CalculateLab(spectrum);
        }
        /// <summary>
        /// 使用指定的光源和觀察者角度根據反射率數據計算XYZ值
        /// </summary>
        /// <param name="spectrum">反射率數據</param>
        /// <param name="illuminant">使用的標準光源</param>
        /// <param name="observer">使用的觀察者角度</param>
        /// <returns></returns>
        public static CIEXYZ CalculateXyz(SpectrumCS spectrum,
            StandardIlluminant illuminant, StandardObserver observer)
        {
            FormulaCS formula = new FormulaCS(spectrum.Range, illuminant, observer);
            return formula.CalculateXyz(spectrum);
        }
        /// <summary>
        /// 計算XYZ值
        /// </summary>
        /// <param name="spectrum">反射率數據</param>
        /// <returns></returns>
        public CIEXYZ CalculateXyz(SpectrumCS spectrum)
        {
            return _tristimulusFunc.CalculateXYZ(spectrum);
        }

        /// <summary>
        /// 計算Lab值
        /// </summary>
        /// <param name="spectrum">反射率數據</param>
        /// <returns></returns>
        public CIELABCS CalculateLab(SpectrumCS spectrum)
        {
            return XyzToLab(CalculateXyz(spectrum));
        }

        private static double f(double a)
        {
            if (a > Math.Pow((24.0 / 116.0), 3))
                return Math.Pow(a, (1.0 / 3.0));
            else
                return (841.0 / 108.0 * a + 16.0 / 116.0);
        }
        private static double f_inv(double x)
        {
            if (x > 24.0 / 116.0)
            {
                return Math.Pow(x, 3);
            }
            else
            {
                return (x - 16.0 / 116.0) / (841.0 / 108.0);
            }
        }
        /// <summary>
        /// 將CIEXYZ轉換爲CIELAB
        /// </summary>
        /// <param name="XYZ">要轉換的XYZ值</param>
        /// <returns></returns>
        public CIELABCS XyzToLab(CIEXYZ XYZ)
        {
            double Xn = _tristimulusFunc.X;
            double Yn = _tristimulusFunc.Y;
            double Zn = _tristimulusFunc.Z;

            //double L = 116.0 * f(XYZ.Y / Yn) - 16.0;
            //double a = 500.0 * (f(XYZ.X / Xn) - f(XYZ.Y / Yn));
            //double b = 200.0 * (f(XYZ.Y / Yn) - f(XYZ.Z / Zn));
            double f_Y = f(XYZ.Y / Yn);
            double f_X = f(XYZ.X / Xn);
            double f_Z = f(XYZ.Z / Zn);

            double L = 116.0 * f_Y - 16.0;
            double a = 500.0 * (f_X - f_Y);
            double b = 200.0 * (f_Y - f_Z);

            return new CIELABCS(L, a, b);
        }
        /// <summary>
        /// 將CIELAB轉換爲CIEXYZ
        /// </summary>
        /// <param name="lab">要轉換的Lab值</param>
        /// <returns></returns>
        public CIEXYZ LabToXyz(CIELABCS lab)
        {
            double f_Y_Yn = (lab.L + 16.0) / 116.0;  // f(Y/Yn)
            double Y_Yn = f_inv(f_Y_Yn); // Y/Yn
            double X_Xn = f_inv(lab.a / 500.0 + f_Y_Yn); // X/Xn
            double Z_Zn = f_inv(f_Y_Yn - lab.b / 200.0); // Z/Zn
            double X = _tristimulusFunc.X * X_Xn;
            double Y = _tristimulusFunc.Y * Y_Yn;
            double Z = _tristimulusFunc.Z * Z_Zn;

            return new CIEXYZ(X, Y, Z);
        }

        // 计算u'的公式
        private double uq(CIEXYZ xyz)
        {
            return 4.0 * xyz.X / (xyz.X + 15.0 * xyz.Y + 3 * xyz.Z);
        }
        // 计算v'的公式
        private double vq(CIEXYZ xyz)
        {
            return 9.0 * xyz.Y / (xyz.X + 15.0 * xyz.Y + 3 * xyz.Z);
        }
        public CIELUV XyzToLuv(CIEXYZ xyz)
        {
            double Xn = _tristimulusFunc.X;
            double Yn = _tristimulusFunc.Y;
            double Zn = _tristimulusFunc.Z;
            CIEXYZ XYZn = new CIEXYZ(Xn, Yn, Zn);

            double L = 116.0 * f(xyz.Y / Yn) - 16.0;
            double u = 13.0 * L * (uq(xyz) - uq(XYZn));
            double v = 13.0 * L * (vq(xyz) - vq(XYZn));

            return new CIELUV(L, u, v);
        }
        public CIEXYZ LuvToXyz(CIELUV luv)
        {
            double Xn = _tristimulusFunc.X;
            double Yn = _tristimulusFunc.Y;
            double Zn = _tristimulusFunc.Z;
            CIEXYZ XYZn = new CIEXYZ(Xn, Yn, Zn);

            double Y_Yn = f_inv((luv.L + 16.0) / 116.0); // Y/Yn
            double Y = Yn * Y_Yn;
            double uq_XYZ = luv.u / luv.L / 13.0 + uq(XYZn); // u'
            double vq_XYZ = luv.v / luv.L / 13.0 + vq(XYZn); // v'
            double XYZ_sum = 9.0 * Y / vq_XYZ; // X + Y + Z
            double X = uq_XYZ * XYZ_sum / 4.0;
            double Z = (XYZ_sum - X - 15.0 * Y) / 3.0;
            return new CIEXYZ(X, Y, Z);
        }
        public static CIERGB XyzToCieRgb(CIEXYZ xyz)
        {
            double[,] mat = {
                {0.41845516, -0.15865714, -0.08283193},
                {-0.09116486,  0.25242564,  0.01570531},
                { 0.00092086, -0.00254975,  0.17859499}
            };
            double R = mat[0, 0] * xyz.X + mat[0, 1] * xyz.Y + mat[0, 2] * xyz.Z;
            double G = mat[1, 0] * xyz.X + mat[1, 1] * xyz.Y + mat[1, 2] * xyz.Z;
            double B = mat[2, 0] * xyz.X + mat[2, 1] * xyz.Y + mat[2, 2] * xyz.Z;
            return new CIERGB(R, G, B);
        }
        public static CIEXYZ CieRgbToXyz(CIERGB rgb)
        {
            double[,] mat = {
                {2.768892, 1.751748, 1.130160},
                {1.000000, 4.590700, 0.060100},
                {0.000000, 0.056508, 5.594292}
            };
            double X = rgb.R * mat[0, 0] + rgb.G * mat[0, 1] + rgb.b * mat[0, 2];
            double Y = rgb.R * mat[1, 0] + rgb.G * mat[1, 1] + rgb.b * mat[1, 2];
            double Z = rgb.R * mat[2, 0] + rgb.G * mat[2, 1] + rgb.b * mat[2, 2];
            return new CIEXYZ(X, Y, Z);
        }

        double Ka
        {
            get
            {
                if (Observer == StandardObserver.CIE1931)
                {
                    switch (Illuminant)
                    {
                        case StandardIlluminant.A: return 185.20;
                        case StandardIlluminant.C: return 175.00;
                        case StandardIlluminant.D50: return 173.51;
                        case StandardIlluminant.D65: return 172.30;
                        case StandardIlluminant.D75: return 172.22;
                        case StandardIlluminant.F2: return 175.00;
                        case StandardIlluminant.F11: return 178.00;
                        case StandardIlluminant.F12: return 183.70;
                        default: break;
                    }
                }
                else
                {
                    switch (Illuminant)
                    {
                        case StandardIlluminant.A: return 186.30;
                        case StandardIlluminant.C: return 174.30;
                        case StandardIlluminant.D50: return 173.82;
                        case StandardIlluminant.D65: return 172.10;
                        case StandardIlluminant.D75: return 171.76;
                        case StandardIlluminant.F2: return 178.60;
                        case StandardIlluminant.F11: return 180.10;
                        case StandardIlluminant.F12: return 186.30;
                        default: break;
                    }
                }
                double Xn = _tristimulusFunc.X;
                double Yn = _tristimulusFunc.Y;
                double Zn = _tristimulusFunc.Z;
                return 175.0 / 198.04 * (Xn + Yn);
            }
        }
        double Kb
        {
            get
            {
                if (Observer == StandardObserver.CIE1931)
                {
                    switch (Illuminant)
                    {
                        case StandardIlluminant.A: return 38.40;
                        case StandardIlluminant.C: return 70.00;
                        case StandardIlluminant.D50: return 58.48;
                        case StandardIlluminant.D65: return 67.20;
                        case StandardIlluminant.D75: return 71.30;
                        case StandardIlluminant.F2: return 52.90;
                        case StandardIlluminant.F11: return 52.30;
                        case StandardIlluminant.F12: return 37.50;
                        default: break;
                    }
                }
                else
                {
                    switch (Illuminant)
                    {
                        case StandardIlluminant.A: return 38.20;
                        case StandardIlluminant.C: return 69.40;
                        case StandardIlluminant.D50: return 58.13;
                        case StandardIlluminant.D65: return 66.70;
                        case StandardIlluminant.D75: return 70.76;
                        case StandardIlluminant.F2: return 53.60;
                        case StandardIlluminant.F11: return 52.70;
                        case StandardIlluminant.F12: return 38.20;
                        default: break;
                    }
                }
                double Xn = _tristimulusFunc.X;
                double Yn = _tristimulusFunc.Y;
                double Zn = _tristimulusFunc.Z;
                return 70.0 / 218.11 * (Yn + Zn);
            }
        }
        public HunterLab XyzToHunterLab(CIEXYZ xyz)
        {
            double Xn = _tristimulusFunc.X;
            double Yn = _tristimulusFunc.Y;
            double Zn = _tristimulusFunc.Z;

            double tmp = Math.Sqrt(xyz.Y / Yn);
            double L = 100 * tmp;
            double a = Ka * (xyz.X / Xn - xyz.Y / Yn) / tmp;
            double b = Kb * (xyz.Y / Yn - xyz.Z / Zn) / tmp;
            return new HunterLab(L, a, b);
        }

        public CIEXYZ HunterLabToXyz(HunterLab lab)
        {
            double Xn = _tristimulusFunc.X;
            double Yn = _tristimulusFunc.Y;
            double Zn = _tristimulusFunc.Z;

            double sqrt_Y_Yn = lab.L / 100.0; // sqrt(Y/Yn)
            double Y_Yn = sqrt_Y_Yn * sqrt_Y_Yn; // Y/Yn
            double Y = Y_Yn * Yn;
            double X = (lab.a * sqrt_Y_Yn / Ka + Y_Yn) * Xn;  // (a * sqrt(Y/Yn) / Ka + Y/Yn) * Xn
            double Z = (Y_Yn - lab.b * sqrt_Y_Yn / Kb) * Zn;  // (Y/Yn - b * sqrt(Y/Yn) / Kb) * Zn

            return new CIEXYZ(X, Y, Z);
        }

        private double convert(double[] CL, CIEXYZ xyz)
        {
            double tmp = CL[0] * xyz.X + CL[1] * xyz.Y + CL[2] * xyz.Z;
            tmp = tmp / 100.0;

            if (tmp * 12.92 <= 0.04045)
                tmp = tmp * 12.92;
            else
                tmp = 1.055 * Math.Pow(tmp, 1.0 / 2.4) - 0.055;
            if (tmp < 0.0) tmp = 0.0;
            if (tmp > 1.0) tmp = 1.0;
            return tmp;
        }

        public sRGB XyzToSrgb(CIEXYZ XYZ)
        {
            // 线性变换矩阵
            double[][] mat = new double[][] {
                new double[] {3.13405134140307, -1.61702771078607, -0.49065220978742},
                new double[] {-0.97876272987415, 1.91614222843468, 0.03344962851486},
                new double[] { 0.07194257711719, -0.22897117960925, 1.40521830532745}
            };

            CIELABCS lab = XyzToLab(XYZ);
            double f_Y_Yn = (lab.L + 16.0) / 116.0;  // f(Y/Yn)
            double Y_Yn = f_inv(f_Y_Yn); // Y/Yn
            double X_Xn = f_inv(lab.a / 500.0 + f_Y_Yn); // X/Xn
            double Z_Zn = f_inv(f_Y_Yn - lab.b / 200.0); // Z/Zn
            double X = 96.42 * X_Xn;
            double Y = 100.0 * Y_Yn;
            double Z = 82.51 * Z_Zn;
            XYZ = new CIEXYZ(X, Y, Z);

            double R = convert(mat[0], XYZ);
            double G = convert(mat[1], XYZ);
            double B = convert(mat[2], XYZ);

            uint rgb = 0xFF000000;
            rgb |= ((uint)(R * 255) & 0xFF) << 16;
            rgb |= ((uint)(G * 255) & 0xFF) << 8;
            rgb |= ((uint)(B * 255) & 0xFF);
            return new sRGB((int)rgb);
        }
        public sRGB LabToSrgb(CIELABCS lab)
        {
            return XyzToSrgb(LabToXyz(lab));
        }


        /// <summary>
        /// 计算T（E313）
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public double GetTAstmE313(CIEXYZ xyz)
        {
            if (Illuminant != StandardIlluminant.C
                && Illuminant != StandardIlluminant.D50
                && Illuminant != StandardIlluminant.D65)
            {
                return double.NaN;
            }
            double xn = _tristimulusFunc.XYZ.x;
            double yn = _tristimulusFunc.XYZ.y;
            double Tx = (Observer == StandardObserver.CIE1931) ? 1000.0 : 900.0;

            return Tx * (xn - xyz.x) - 650 * (yn - xyz.y);
        }

        /// <summary>
        /// ASTM E313-2005 白度（AATCC 110-2005白度、甘茨白度与此公式相同，是其子公式）
        /// </summary>
        /// <param name="xyz"></param>
        /// <return>返回白度指数；如果不支持或是无效值，返回NaN</return>
        public double GetWhitenessAstmE313(CIEXYZ xyz)
        {
            if (Illuminant != StandardIlluminant.C
                && Illuminant != StandardIlluminant.D50
                && Illuminant != StandardIlluminant.D65)
            {
                return double.NaN;
            }

            double xn = _tristimulusFunc.XYZ.x;
            double yn = _tristimulusFunc.XYZ.y;

            double WI = xyz.Y + 800 * (xn - xyz.x) + 1700 * (yn - xyz.y);
            double T = GetTAstmE313(xyz);

            if (40.0 < WI && WI < (5.0 * xyz.Y - 280.0) && -3.0 < T && T < 3)
            {
                return WI;
            }
            return double.NaN;
        }

        /// <summary>
        /// 计算T（CIE/ISO）
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns></returns>
        public double GetT_CIEISO(CIEXYZ xyz)
        {
            if (Illuminant == StandardIlluminant.D65 && Observer == StandardObserver.CIE1964)
            {
                return GetTAstmE313(xyz);
            }
            return double.NaN;
        }

        /// <summary>
        /// ASTM CIE/ISO 白度
        /// </summary>
        /// <param name="xyz"></param>
        /// <return>返回白度指数；如果不支持或是无效值，返回NaN</return>
        public double GetWhiteness_CIEISO(CIEXYZ xyz)
        {
            if (Illuminant == StandardIlluminant.D65 && Observer == StandardObserver.CIE1964)
            {
                return GetWhitenessAstmE313(xyz);
            }
            return double.NaN;
        }

        // 获取X、Y、Z与A、G、B之间的变换系数
        private bool getAGBCoefficents(out double fXA, out double fXB, out double fZB)
        {
            //typedef double Coefficents[3];
            //typedef std::tuple < double, double, double> Coefficents;
            Dictionary<StandardIlluminant, double[]> cie1937 =
                new Dictionary<StandardIlluminant, double[]>();
            cie1937.Add(StandardIlluminant.A, new double[] { 1.0447, 0.0539, 0.3558 });  // A
            cie1937.Add(StandardIlluminant.D55, new double[] { 0.8061, 0.1504, 0.9209 });  // D55
            cie1937.Add(StandardIlluminant.D65, new double[] { 0.7701, 0.1804, 1.0889 });  // D65
            cie1937.Add(StandardIlluminant.D75, new double[] { 0.7446, 0.2047, 1.2256 });  // D75
            cie1937.Add(StandardIlluminant.C, new double[] { 0.7832, 0.1975, 1.1822 });   // C

            // CIE1964 (10度）
            Dictionary<StandardIlluminant, double[]> cie1964 =
                new Dictionary<StandardIlluminant, double[]>();
            cie1964.Add(StandardIlluminant.A, new double[] { 1.0571, 0.0544, 0.3520 });  // A
            cie1964.Add(StandardIlluminant.D55, new double[] { 0.8078, 0.1502, 0.9098 });  // D55
            cie1964.Add(StandardIlluminant.D65, new double[] { 0.7683, 0.1798, 1.0733 });  // D65
            cie1964.Add(StandardIlluminant.D75, new double[] { 0.7405, 0.2038, 1.2073 });  // D75
            cie1964.Add(StandardIlluminant.C, new double[] { 0.7772, 0.1957, 1.1614 });   // C

            if (Observer == StandardObserver.CIE1931)
            {
                if (cie1937.ContainsKey(Illuminant))
                {
                    fXA = cie1937[Illuminant][0];
                    fXB = cie1937[Illuminant][1];
                    fZB = cie1937[Illuminant][2];
                    return true;
                }
            }
            else
            {
                if (cie1964.ContainsKey(Illuminant))
                {
                    fXA = cie1964[Illuminant][0];
                    fXB = cie1964[Illuminant][1];
                    fZB = cie1964[Illuminant][2];
                    return true;
                }
            }
            fXA = fXB = fZB = double.NaN;
            return false;
        }

        /// <summary>
        /// Tappl （坦伯白度公式)
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns>返回Tappl白度指数；如果不支持所在光源和观察者角度返回NaN</returns>
        public double GetWhitenessTappl(CIEXYZ xyz)
        {
            double fXA, fXB, fZB;
            if (!getAGBCoefficents(out fXA, out fXB, out fZB))
                return double.NaN;
            return 1.0 / fZB * xyz.Z;
        }
        /// <summary>
        /// Taube （陶贝白度公式）
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns>返回Taube白度指数；如果不支持所在光源和观察者角度返回NaN</returns>
        public double GetWhitenessTaube(CIEXYZ xyz)
        {
            double fXA, fXB, fZB;
            if (!getAGBCoefficents(out fXA, out fXB, out fZB))
                return double.NaN;
            return 4.0 / fZB * xyz.Z - 3.0 * xyz.Y;
        }
        /// <summary>
        /// Hunter 1942 （亨特白度公式）
        /// </summary>
        /// <param name="lab"></param>
        /// <returns>返回Hunter 1942白度指数；如果不支持所在光源和观察者角度返回NaN</returns>
        public double GetWhitnessHunter1942(HunterLab lab)
        {
            if ((Observer == StandardObserver.CIE1931 && Illuminant == StandardIlluminant.C)
                || (Observer == StandardObserver.CIE1964 && Illuminant == StandardIlluminant.D65))
            {
                return 100 - Math.Sqrt(Math.Pow(100 - lab.L, 2) + Math.Pow(lab.a, 2)
                    + Math.Pow(lab.b, 2));
            }
            return double.NaN;
        }
        /// <summary>
        /// Hunter 1964 （亨特白度公式）
        /// </summary>
        /// <param name="lab"></param>
        /// <returns>返回Hunter 1964白度指数；如果不支持所在光源和观察者角度返回NaN</returns>
        public double GetWhitnessHunter1964(HunterLab lab)
        {
            if ((Observer == StandardObserver.CIE1931 && Illuminant == StandardIlluminant.C)
                || (Observer == StandardObserver.CIE1964 && Illuminant == StandardIlluminant.D65))
            {
                return lab.L - 3 * lab.b;
            }
            return double.NaN;
        }

        /// <summary>
        /// 计算R457白度
        /// </summary>
        /// <param name="spectrum">反射率数据</param>
        /// <returns>如果是A光源返回白度值，否则返回 NaN </returns>
        public double GetWhitnessR457(SpectrumCS spectrum)
        {
            if (Illuminant != StandardIlluminant.A)
                return double.NaN;

            // F(λ) 400-520nm
            double[] Flambda = new double[]
            {
                /* 0.0, 0.0, */ 1.0, 6.7, 18.2, 34.5, 57.6, 82.5, 100.0, 88.7, 53.1, 20.3, 5.6, 0.3, 0.0
            };
            double Kb = 100.0 / 468.5;  // 归化系数

            double sum = 0.0;
            int offset = spectrum.Range.IndexOf(400);
            for (int i = 0; i < Flambda.Length; i++)
            {
                sum += Flambda[i] * spectrum.Data[i + offset];
            }
            return Kb * sum;
        }

        /// <summary>
        ///  ASTM D1925-1970 黄度
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns>返回 ASTM D1925-1970 黄度指数；如果不支持所在光源和观察者角度返回NaN</returns>
        public double GetYellownessAstmD1925(CIEXYZ xyz)
        {
            if (Observer == StandardObserver.CIE1931 && Illuminant == StandardIlluminant.C)
            {
                return 100 * (1.28 * xyz.X - 1.06 * xyz.Z) / xyz.Y;
            }
            return double.NaN;
        }
        // 
        /// <summary>
        /// ASTM E313-2005 黄度指数
        /// </summary>
        /// <param name="xyz"></param>
        /// <returns>返回 ASTM E313-2005 黄度指数；如果不支持所在光源和观察者角度返回NaN</returns>
        public double GetYellownessAstmE313(CIEXYZ xyz)
        {
            double Cx, Cz;
            if (Observer == StandardObserver.CIE1931)
            {
                if (Illuminant == StandardIlluminant.C)
                {
                    Cx = 1.2769;
                    Cz = 1.0592;
                }
                else if (Illuminant == StandardIlluminant.D65)
                {
                    Cx = 1.2985;
                    Cz = 1.1335;
                }
                else
                {
                    return double.NaN;
                }
            }
            else
            { // CIE1964
                if (Illuminant == StandardIlluminant.C)
                {
                    Cx = 1.2871;
                    Cz = 1.0781;
                }
                else if (Illuminant == StandardIlluminant.D65)
                {
                    Cx = 1.3013;
                    Cz = 1.1498;
                }
                else
                {
                    return double.NaN;
                }
            }
            return 100 * (Cx * xyz.X - Cz * xyz.Z) / xyz.Y;
        }

        // 计算同色异谱指数
        public double GetMetamerismIndiex(SpectrumCS standard,  // 标样的光谱
            SpectrumCS test,  // 试样的光谱
            StandardIlluminant testIlluminant,   // 测试光源
            StandardObserver testObserver)  // 测试观察者角度
        {
            return GetMetamerismIndiex(standard, test, new FormulaCS(_spectrumRange, testIlluminant, testObserver));
        }

        public double GetMetamerismIndiex(SpectrumCS standard, SpectrumCS test, FormulaCS referenceFormula)
        {
            CIEXYZ XYZ_r1 = CalculateXyz(standard);
            CIEXYZ XYZ_r2 = CalculateXyz(test);

            CIEXYZ XYZ_t1 = referenceFormula.CalculateXyz(standard);
            CIEXYZ XYZ_t2 = referenceFormula.CalculateXyz(test);

            return GetMetamerismIndiex(XYZ_r1, XYZ_t1, XYZ_r2, XYZ_t2, referenceFormula);
        }

        /// <summary>
        /// 计算同色异谱指数
        /// </summary>
        /// <param name="r1">标样当前光源、观察者角度下的XYZ值</param>
        /// <param name="t1">标样formula2的光源、观察者角度下的XYZ值</param>
        /// <param name="r2">试样当前光源、观察者角度下的XYZ值</param>
        /// <param name="t2">试样formula2的光源、观察者角度下的XYZ值</param>
        /// <param name="formula2">与当前不同的另一种观察条件</param>
        /// <returns>同色异谱指数</returns>
        public double GetMetamerismIndiex(CIEXYZ r1, CIEXYZ t1, CIEXYZ r2, CIEXYZ t2, FormulaCS formula2)
        {
            double Xt2 = t2.X * (r1.X / r2.X);
            double Yt2 = t2.Y * (r1.Y / r2.Y);
            double Zt2 = t2.Z * (r1.Z / r2.Z);
            t2 = new CIEXYZ(Xt2, Yt2, Zt2);

            return new CIEDE1976(formula2.XyzToLab(t2), formula2.XyzToLab(t1)).dE;
        }

        // 计算沾色牢度的dEgs，如果设置无效则返回NaN
        public double GetStainingFastness_dEgs(CIELABCS standard, CIELABCS test)
        {
            if ((Observer == StandardObserver.CIE1931 && Illuminant == StandardIlluminant.C)
                || (Observer == StandardObserver.CIE1964 && Illuminant == StandardIlluminant.D65))
            {
                DeltaLab delta = test - standard;

                double dE = delta.dE;
                double dL = delta.dL;
                double dEgs = dE - 0.4 * Math.Sqrt(dE * dE - dL * dL);
                return Math.Round(dEgs, 2); // 保留两位有效小数
            }
            return double.NaN;
        }

        /// <summary>
        /// 计算沾色牢度等级（连续的）
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="test"></param>
        /// <returns>牢度等级；如果不支持，返回NaN</returns>
        public double GetStainingFastness_SSR(CIELABCS standard, CIELABCS test)
        {
            double dEgs = GetStainingFastness_dEgs(standard, test);
            if (double.IsNaN(dEgs))
                return double.NaN;

            double SSR = dEgs >= 4.26 ? (6.1 - 1.45 * Math.Log(dEgs)) : (5.0 - 0.23 * dEgs);
            SSR = Math.Round(SSR, 2); // 保留两位有效小数
            return SSR;
        }

        /// <summary>
        /// 沾色牢度保留两位小数后，等级划分如下
        /// 5,00 to 4,75     5
        /// 4,74 to 4,25     4-5
        /// 4,24 to 3,75     4
        /// 3,74 to 3,25     3-4
        /// 3,24 to 2,75     3
        /// 2,74 to 2,25     2-3
        /// 2,24 to 1,75     2
        /// 1,74 to 1,25     1-2
        /// < 1,25           1
        /// </summary>
        static readonly double[] SSRGradeRanges = {
             4.75, 4.25, 3.75, 3.25, 2.75, 2.25, 1.75, 1.25
        };

        /// <summary>
        /// 获取用于报表的沾色牢度等级
        /// </summary>
        /// <param name="standard">标样</param>
        /// <param name="test">试样</param>
        /// <returns>返回粘色牢度等级；如果是不支持光源和观察者角度，返回INVALID</returns>
        public ColourFastnessLevel GetReportedSSR(CIELABCS standard, CIELABCS test)
        {
            double SSR = GetStainingFastness_SSR(standard, test);
            if (SSR == double.NaN)
                return ColourFastnessLevel.INVALID;

            for (int i = 0; i < SSRGradeRanges.Length; ++i)
            {
                if (SSR >= SSRGradeRanges[i])
                    return (ColourFastnessLevel)(i);
            }
            return ColourFastnessLevel.LEVEL_1;
        }

        /// <summary>
        /// 计算变色牢度等级（连续的）
        /// </summary>
        /// <param name="standard">标样</param>
        /// <param name="test">试样</param>
        /// <returns>变色牢度等级（连续的）；如果不支持，返回NaN</returns>
        public double GetColorFastness_GSR(CIELABCS standard, CIELABCS test)
        {
            double dE_f = GetColorFastness_dEf(standard, test);
            if (double.IsNaN(dE_f))
                return double.NaN;

            double GSS = dE_f > 3.40 ? (5.0 - Math.Log10(dE_f / 0.85) / Math.Log10(2))
                : (5.0 - dE_f / 1.7);
            return Math.Round(GSS, 2); // 保留两位小数
        }

        /// <summary>
        /// 變色牢度等級表
        /// </summary>
        static readonly double[] GSRGradeRanges = {
            0.40, 1.25, 2.10, 2.95, 4.10, 5.80, 8.20, 11.60
        };

        /// <summary>
        /// 获取用于报表的变色牢度等级
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="test"></param>
        /// <returns>变色牢度等级；如果不支持，返回INVALID</returns>
        public ColourFastnessLevel GetReportedGSR(CIELABCS standard, CIELABCS test)
        {
            double dE_f = GetColorFastness_dEf(standard, test);
            if (double.IsNaN(dE_f))
                return ColourFastnessLevel.INVALID;
            for (int i = 0; i < GSRGradeRanges.Length; i++)
            {
                if (dE_f < GSRGradeRanges[i])
                    return (ColourFastnessLevel)i;
            }
            return ColourFastnessLevel.LEVEL_1;
        }

        // 重設三刺激函數
		private void resetTristimulusFunctions()
        {
            _tristimulusFunc = new TristimulusValues(
                ColorMatchingFunctions.GetByObserver(Observer),
                SpectralPowerDistribution.GetByIlluminant(Illuminant));
        }
        
        // 计算变色牢度试样和原样的色差；如果光源和观察者角度无效，返回NaN
        public double GetColorFastness_dEf(CIELABCS standard, CIELABCS test)
        {
            if (Observer != StandardObserver.CIE1964 || Illuminant != StandardIlluminant.D65)
                return double.NaN;

            DeltaLab  delta = test - standard;

            double dL = delta.dL;
            double dH_ab = delta.dH;
            double dC_ab = delta.dC;
            double h_m = (test.h + standard.h) / 2;
            if (Math.Abs(test.h - standard.h) > 180)
            {
                if (test.h + standard.h < 360)
                    h_m += 180;
                else  // (test.h + original.h >= 360)
                    h_m -= 180;
            }
            double C_m = (test.C + standard.C) / 2.0;
            double X = Math.Pow((360.0 - Math.Abs(h_m - 280.0)) / 30.0, 2);
            double D = dC_ab * C_m * Math.Exp(-X) / 100.0;
            double dC_k = dC_ab - D;
            double dH_k = dH_ab - D;
            double dC_f = dC_k / (1.0 + Math.Pow(20.0 * C_m / 1000.0, 2));
            double dH_f = dH_k / (1.0 + Math.Pow(10.0 * C_m / 1000.0, 2));
            double dE_f = Math.Sqrt(dL * dL + dC_f * dC_f + dH_f * dH_f);
            return Math.Round(dE_f, 2); // 保留两位小数
        }

        /// <summary>
        /// 根據反射率計算 K/S，具體算法參考《The Kubelka-Monk Theory and K/S》
        /// </summary>
        /// <param name="r">反射率</param>
        /// <returns>K/S</returns>
        double GetKDivideByS(double r)
        {
            return (1 - r) * (1 - r) / (2 * r);
        }

        /// <summary>
        /// 根據樣品的反射率數據計算 Color Value SUM。
        /// 算法參考 HunterLab 的《Strength Calculations》
        /// </summary>
        /// <param name="reflectances">樣品的反射率數據</param>
        /// <returns>返回Color Value SUM</returns>
        public double GetColorValueSum(SpectrumCS reflectances)
        {
            double sum = 0;
            foreach(double r in reflectances.Data)
            {
                sum += GetKDivideByS(r);
            }
            return sum / reflectances.Range.Count;
        }
        /// <summary>
        /// 計算 Color Value SWL
        /// 具體算法參考《Strength Calculations》
        /// </summary>
        /// <param name="reflectances">樣品的反射率數據</param>
        /// <param name="wavelengthOfMinimum">最小反射率的波長</param>
        /// <returns>返回最小反射率的K/S,即Color Value SWL</returns>
        public double GetColorValueSWL(SpectrumCS reflectances, ref int wavelengthOfMinimum)
        {
            wavelengthOfMinimum = reflectances.Range.Min;
            double minR = reflectances[wavelengthOfMinimum];

            for (int wavelength = reflectances.Range.Min; 
                wavelength <= reflectances.Range.Max;
                wavelength += reflectances.Range.Step)
            {
                if (minR > reflectances[wavelength])
                {
                    minR = reflectances[wavelength];
                    wavelengthOfMinimum = wavelength;
                }
            }
            return GetKDivideByS(minR);
        }

        /// <summary>
        /// 計算Average Strength（also called % Strength SUM）
        /// 算法參考《Strength Calculations》
        /// </summary>
        /// <param name="standard">標樣的反射率數據</param>
        /// <param name="test">試樣的反射率數據</param>
        /// <returns>Average Strength</returns>
        public double GetAverageStrength(SpectrumCS standard, SpectrumCS test)
        {
            return GetColorValueSum(test) / GetColorValueSum(standard) * 100.0;
        }
        /// <summary>
        /// 計算 %Strength-SWL
        /// 算法參考《Strength Calculations》
        /// </summary>
        /// <param name="standard">標樣的反射率數據</param>
        /// <param name="test">試樣的反射率數據</param>
        /// <param name="wavelengthOfMinimum">標樣最小反射率的波長</param>
        /// <returns>%strength-SWL</returns>
        public double GetPrecentStrengthSWL(SpectrumCS standard, SpectrumCS test, ref int wavelengthOfMinimum)
        {
            //if (standard.Range != test.Range)
            //    throw new ArgumentException("the range of standard must be equal the range of test.");

            WavelengthRange range = new WavelengthRange(Math.Max(standard.Range.Min, test.Range.Min),
                Math.Min(standard.Range.Max, test.Range.Max), 1);
            SpectrumCS s = standard.Interpolate(range);
            SpectrumCS t = test.Interpolate(range);

            double SWLstd = GetColorValueSWL(s, ref wavelengthOfMinimum);
            double SWLtest = GetKDivideByS(t[wavelengthOfMinimum]);
            return SWLtest / SWLstd * 100.0;
        }
        /// <summary>
        /// 計算%Strength
        /// 算法參考《Strength Calculations》
        /// </summary>
        /// <param name="standard">標樣的反射率數據</param>
        /// <param name="test">試樣的反射率數據</param>
        /// <returns>每個反射率的%Strength</returns>
        public double[] GetPrecentStrength(SpectrumCS standard, SpectrumCS test)
        {
            var range = standard.Range;
            double[] strength = new double[standard.Range.Count];
            int i = 0;
            for (int wavelength = range.Min; wavelength <= range.Max; wavelength += range.Step)
            {
                strength[i++] = GetKDivideByS(test[wavelength]) /
                    GetKDivideByS(standard[wavelength]);
            }
            return strength;
        }

        /// <summary>
        /// 获取遮盖度
        /// </summary>
        /// <param name="blackBackgroupd">黑底样品</param>
        /// <param name="whiteBackground">白底样品</param>
        /// <returns>返回遮盖度，已经转化为百分比</returns>
        public double GetOpacity(CIEXYZ whiteBaseSample, CIEXYZ blackBaseSample)
        {
            return blackBaseSample.Y / whiteBaseSample.Y;
        }
    }
}
