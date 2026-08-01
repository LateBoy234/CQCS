using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;
using Application = System.Windows.Application;

namespace ThreeNH.ColorQualityControl.Adapter
{
    public class SpecimenPropertyValueConverter<T> : IValueConverter
    {
        private Converter<T, string> converter;

        public SpecimenPropertyValueConverter(Converter<T, string> converter)
        {
            this.converter = converter;
        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "--";
            }
            return converter((T)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public Converter<T, string> Converter
        {
            get { return converter; }
        }
    }

    public class MunsellValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length == 2)
            {
                if (values[0] is double hValue)
                {
                    if (double.IsNaN(hValue))
                        return "--";
                    return hValue.ToString("F1") + values[1].ToString().Trim();
                }
            }
            return "--";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SpecimenAdapter : INotifyPropertyChanged
    {

        internal SystemDefaultConfig SystemDefaultConfig
        {
            get { return (Application.Current as App).SystemDefaultConfig; }
        }

        protected static readonly Dictionary<ChromaticityField, IValueConverter> _valueConverters =
            new Dictionary<ChromaticityField, IValueConverter>();

        protected static readonly Dictionary<ChromaticityField, AbsoluteStrengthConverter> _swlConverters =
           new Dictionary<ChromaticityField, AbsoluteStrengthConverter>();
        public Formula Formula { get; private set; }

        /// <summary>
        /// 引用的样品
        /// </summary>
        public Specimen Specimen { get; protected set; }
        MainWindow MainWindow
        {
            get { return System.Windows.Application.Current.MainWindow as MainWindow; }
        }

        /// <summary>
        /// 仿真色
        /// </summary>
        public SolidColorBrush PseudoColorBrush
        {
            get; protected set;
        }
        public Color PseudoColor
        {
            get { return PseudoColorBrush.Color; }
            set { PseudoColorBrush.Color = value; }
        }
        static SpecimenAdapter()
        {
            foreach (ChromaticityField field in Enum.GetValues(typeof(ChromaticityField)))
            {
                if (field.IsReflectance() || field.IsReflectanceDifference())
                {
                    _valueConverters[field] = SpectrumConverter.Converter;
                    continue;
                }

                if (field.IsAbsoluteStrengthField())
                {
                    _valueConverters[field] = new AbsoluteStrengthConverter();
                    continue;
                }
                IValueConverter valueConverter = null;
                switch (field)
                {
                    //case ChromaticityField.PtCoIndex: 
                    //     valueConverter = new SpecimenPropertyValueConverter<double>(x => double.IsNaN(x) ? "--" : x.ToString("F0"));
                    //     break;
                    case ChromaticityField.DateTime:
                        valueConverter = new SpecimenPropertyValueConverter<DateTime>(x => x.ToString());
                        break;
                    case ChromaticityField.x:
                    case ChromaticityField.y:
                    case ChromaticityField.z:
                    case ChromaticityField.dx:
                    case ChromaticityField.dy:
                    case ChromaticityField.LuminanceFactor:
                    case ChromaticityField.dLuminanceFactor:
                    case ChromaticityField.uq:
                    case ChromaticityField.vq:
                    case ChromaticityField.dvq:
                    case ChromaticityField.duq:
                        valueConverter = new SpecimenPropertyValueConverter<double>(x => double.IsNaN(x) ? "--" : x.ToString("F5"));
                        break;
                    case ChromaticityField.sR:
                    case ChromaticityField.sG:
                    case ChromaticityField.sB:
                        valueConverter = new SpecimenPropertyValueConverter<double>(x => x.ToString());
                        break;
                    case ChromaticityField.HazeC2:
                    case ChromaticityField.HazeA2:
                    case ChromaticityField.HazeD65:
                    case ChromaticityField.DeltaHazeC2:
                    case ChromaticityField.DeltaHazeA2:
                    case ChromaticityField.DeltaHazeD65:
                        valueConverter = new SpecimenPropertyValueConverter<double>(x => double.IsNaN(x) ? "--" : (x * 100).ToString("F2"));
                        break;
                    case ChromaticityField.SpecularComponent:
                        valueConverter = new SpecimenPropertyValueConverter<SpecularComponent>(x => x == SpecularComponent.Unknown ? "--" : x.ToString());
                        break;
                    case ChromaticityField.GardnerIndex:
                    case ChromaticityField.IronCobaltIndex:
                    case ChromaticityField.TransmittanceC2:
                    case ChromaticityField.TransmittanceA2:
                    case ChromaticityField.TransmittanceD65:
                    case ChromaticityField.Clarity:
                    case ChromaticityField.DeltaClarity:
                    case ChromaticityField.C_A:
                    case ChromaticityField.C_E:
                    case ChromaticityField.C_I:
                    case ChromaticityField.C_T:
                    case ChromaticityField.M_A:
                    case ChromaticityField.M_E:
                    case ChromaticityField.M_I:
                    case ChromaticityField.M_T:
                    case ChromaticityField.Y_A:
                    case ChromaticityField.Y_E:
                    case ChromaticityField.Y_I:
                    case ChromaticityField.Y_T:
                    case ChromaticityField.K_A:
                    case ChromaticityField.K_E:
                    case ChromaticityField.K_I:
                    case ChromaticityField.K_T:
                        valueConverter = new SpecimenPropertyValueConverter<double>(x => double.IsNaN(x) || x < 0 ? "--" : x.ToString("F2"));
                        break;
                    default:
                        if (field.IsPercent())
                            valueConverter = new SpecimenPropertyValueConverter<double>(x => double.IsNaN(x) ? "--" : x.ToString("P"));
                        else if (field.IsDoubleValue())
                            valueConverter = new SpecimenPropertyValueConverter<double>(x => double.IsNaN(x) ? "--" : x.ToString("F2"));
                        else if (field.IsStringValue())
                            valueConverter = new SpecimenPropertyValueConverter<string>(x => x);
                        else if (field.IsEnumValue())
                            valueConverter = new EnumItemConverter();
                        else if (field.IsIntValue())
                            valueConverter = new SpecimenPropertyValueConverter<int>(x => x.ToString());
                        else if (field.IsChromaticityIndexDiff())
                            valueConverter = new SpecimenPropertyValueConverter<double>(x => double.IsNaN(x) ? "--" : x.ToString("F2"));
                        else
                            valueConverter = new SpecimenPropertyValueConverter<object>(x => x.ToString());
                        break;
                }

                _valueConverters[field] = valueConverter;
            }
        }

        protected static CIEXYZ GetXyz(Specimen specimen, Formula formula, SpecularComponent sc)
        {
            if (specimen == null || specimen.GetColorData(sc) == null)
                return null;
            return specimen.GetColorData(sc).XYZ(formula);
        }

        protected static Color GetColor(CIEXYZ xyz, Formula formula)
        {
            if (xyz != null)
            {
                var rgb = formula.XyzToSrgb(xyz);
                return Color.FromRgb(rgb.R, rgb.G, rgb.B);
            }
            return new Color();
        }

        public SpecimenAdapter(Specimen specimen,
            Formula formula,
            ChromaticitySetting setting,
            SpecularComponent sc)
            : this(specimen)
        {
            CalculateValues(setting, sc, formula);
        }

        protected SpecimenAdapter(Specimen specimen)
        {
            this.Specimen = specimen;
            WI_E313 = double.NaN;
            T_E313 = double.NaN;
            WI_CIE_ISO = double.NaN;
            T_CIE_ISO = double.NaN;
            WI_Hunter1942 = double.NaN;
            WI_Hunter1960 = double.NaN;
            WI_Tapple = double.NaN;
            WI_Taube = double.NaN;
            WI_R457 = double.NaN;
            YI_D1925 = double.NaN;
            YI_E313 = double.NaN;
            Gloss8 = specimen.Gloss8 == null ? double.NaN : (double)specimen.Gloss8;
            PseudoColorBrush = new SolidColorBrush();
            Clarity = specimen.Clarity;

        }

        /// <summary>
        /// 根据参考生成同色异谱字段的属性名
        /// </summary>
        /// <param name="referenceFormula">参考设置</param>
        /// <returns>属性名</returns>
        public static string MakeMIName(Formula referenceFormula)
        {
            return string.Format("MI({0}/{1})", referenceFormula.Illuminant,
                 referenceFormula.Observer == StandardObserver.CIE1931 ? "2\u00B0" : "10\u00B0");
        }

        public static string FormatMI(double mi)
        {
            if (double.IsNaN(mi))
                return "--";
            return mi.ToString("F2");
        }


        // 用于颜色排序
        public int ColorValue
        {
            get
            {
                if (Lab != null)
                {
                    return (int)(Math.Abs(Lab.L) + Math.Abs(Lab.a) + Math.Abs(Lab.b));
                }
                else if (XYZ != null)
                {
                    return (int)(XYZ.Y * 100.0 + XYZ.x * 100.0 + XYZ.y * 100.0);
                }
                return 0;
            }
        }
        public string Name
        {
            get { return Specimen.Name; }
        }

        public int ID
        {
            get { return Specimen.ID; }
        }

        public DateTime DateTime
        {
            get { return Specimen.DateTime; }
            //get { return DateTime.Now; }
        }

        public string Material
        {
            get { return Specimen.Material; }
        }

        public string InstrumentName
        {
            get { return Specimen.Instrument; }
        }
        //颜色配方
        public string ColorFormula
        {
            get { return Specimen.ColorFormula; }
        }

        public UVFlag UV
        {
            get { return Specimen.UV; }
        }

        public MeasurementCaliber MeasurementCaliber
        {
            get
            {
                if (Specimen.Instrument == "MCR10")
                {
                    return MeasurementCaliber.Phi1;
                }
                else if (Specimen.Instrument == "BTS20")
                {
                    return MeasurementCaliber.Phi50;
                }
                return Specimen.MeasurementCaliber;
            }
        }

        public string IlluminantAndObserver
        {
            get { return string.Format("{0}/{1}", Formula.Illuminant, Formula.Observer.Description()); }
        }

        Spectrum spectrum = null;
        public Spectrum Spectrum
        {
            get { return spectrum; }
            set
            {
                spectrum = value;
            }
        }

        CIELAB _lab = null;
        public CIELAB Lab
        {
            get { return _lab; }
            protected set { _lab = value; }
        }

        HunterLab _hunterLab = null;
        public HunterLab HunterLab
        {
            get { return _hunterLab; }
            protected set { _hunterLab = value; }
        }

        private MunsellHVC _munsellHVC;
        public MunsellHVC MunsellHVC
        {
            get => _munsellHVC;
            protected set => _munsellHVC = value;
        }


        CIEXYZ _xyz = null;
        public CIEXYZ XYZ
        {
            get { return _xyz; }
            protected set { _xyz = value; }
        }

        CIELUV _luv = null;
        public CIELUV Luv
        {
            get { return _luv; }
            protected set { _luv = value; }
        }

        CIERGB _rgb = null;
        public CIERGB CIERGB
        {
            get { return _rgb; }
            protected set { _rgb = value; }
        }

        DIN99Lab _din99Lab = null;

        public DIN99Lab Din99Lab
        {
            get { return _din99Lab; }
            protected set { _din99Lab = value; }
        }
      
        public SpecimenDatabase Database
        {
            get { return MainWindow.Database; }
        }

        public object GetPropertyValue(ChromaticityField field)
        {
            return GetPropertyValue(field.ToString());
        }

        public object GetPropertyValue(string propertyName)
        {
            ChromaticityField field;
            if (Enum.TryParse(propertyName, out field))
            {
                if (field.IsReflectance())
                {
                    var wavelength = field.ToWavelength();
                    if (Spectrum != null && Spectrum.Range.Contains(wavelength))
                    {
                        if (Spectrum.Range.Contains(wavelength))
                        {
                            return Spectrum[wavelength];
                        }
                    }

                    return double.NaN;
                }
                else if (field.IsReflectanceDifference())
                {
                    var wavelength = field.ToWavelength();
                    var test = this as TestSpecimenAdapter;
                    if (test != null && test.ReflectanceDifferences != null
                                     && test.ReflectanceDifferences.Range.Contains(wavelength))
                    {
                        return test.ReflectanceDifferences[wavelength];
                    }

                    return double.NaN;

                }
            }
            var propertyInfo = GetType().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                var value = propertyInfo.GetValue(this, null);
                return propertyInfo.GetValue(this, null);
            }
            return null;
        }


        public virtual string GetFormatedPropertyValue(ChromaticityField field, string defaultReturn = "--")
        {
            object value = GetPropertyValue(field);
            if (value != null)
            {
                if (field == ChromaticityField.MH || field == ChromaticityField.dMH)
                {
                    var values = new object[] { value, MunsellHVC?.Hstr };
                    var converter = new MunsellValueConverter();
                    return converter.Convert(values, typeof(string), null, CultureInfo.CurrentCulture) as string;
                }
                else if (field.IsAbsorbanceField())
                {
                    int wavelength = field.ToWavelength();

                    var converter = new AbsorbanceConverter();
                    var values = new object[] { spectrum, Specimen.IsTransmission };
                    return converter.Convert(values, null, wavelength, CultureInfo.CurrentCulture) as string;
                }
                else
                {
                    var converter = GetPropertyValueConverter(field);
                    return converter.Convert(value, typeof(string), null, CultureInfo.CurrentCulture) as string;
                }
            }
            else
            {
                return defaultReturn;
            }
        }


        public virtual string GetFormatedPropertyValue(string propertyName, string defaultReturn = "--")
        {
            ChromaticityField field;
            if (!Enum.TryParse(propertyName, out field))
            {
                return defaultReturn;
            }

            object value = GetPropertyValue(propertyName);
            if (value != null)
            {
                IValueConverter converter = GetPropertyValueConverter(field);
                return converter.Convert(value, typeof(string), null, CultureInfo.CurrentCulture) as string;
            }

            return "--";
        }

        public static IValueConverter GetPropertyValueConverter(ChromaticityField field)
        {
            return _valueConverters[field];
        }


        /// <summary>
        /// CIE Lab L
        /// </summary>
        public double L { get { return Lab != null ? Lab.L : (Luv != null ? Luv.L : double.NaN); } }
        /// <summary>
        /// CIE Lab a
        /// </summary>
        public double a { get { return Lab != null ? Lab.a : double.NaN; } }
        /// <summary>
        /// CIE Lab b
        /// </summary>
        public double b { get { return Lab != null ? Lab.b : double.NaN; } }
        /// <summary>
        /// CIE LCh C
        /// </summary>
        public double C { get { return Lab != null ? Lab.C : double.NaN; } }
        /// <summary>
        /// CIE LCH h
        /// </summary>
        public double h { get { return Lab != null ? Lab.h : double.NaN; } }
        /// <summary>
        /// CIE Luv u
        /// </summary>
        public double u { get { return Luv != null ? Luv.u : double.NaN; } }
        /// <summary>
        /// CIE Luv v
        /// </summary>
        public double v { get { return Luv != null ? Luv.v : double.NaN; } }

        public double uq { get { return Luv != null ? Luv.uq : double.NaN; } }

        public double vq { get { return Luv != null ? Luv.vq : double.NaN; } }
        /// <summary>
        /// CIE XYZ X
        /// </summary>
        public double X { get { return XYZ != null ? XYZ.X : double.NaN; } }
        /// <summary>
        /// CIE XYZ Y
        /// </summary>
        public double Y { get { return XYZ != null ? XYZ.Y : double.NaN; } }
        /// <summary>
        /// CIE XYZ Z
        /// </summary>
        public double Z { get { return XYZ != null ? XYZ.Z : double.NaN; } }
        /// <summary>
        /// CIE Yxy x
        /// </summary>
        public double x { get { return XYZ != null ? XYZ.x : double.NaN; } }
        /// <summary>
        /// CIE Yxy y
        /// </summary>
        public double y { get { return XYZ != null ? XYZ.y : double.NaN; } }
        /// <summary>
        /// CIE Yxy z
        /// </summary>
        public double z { get { return XYZ != null ? XYZ.z : double.NaN; } }
        /// <summary>
        /// CIE RGB R
        /// </summary>
        public double R { get { return CIERGB != null ? CIERGB.R : double.NaN; } }
        /// <summary>
        /// CIE RGB G
        /// </summary>
        public double G { get { return CIERGB != null ? CIERGB.G : double.NaN; } }
        /// <summary>
        /// CIE RGB B
        /// </summary>
        public double B { get { return CIERGB != null ? CIERGB.B : double.NaN; } }
        /// <summary>
        /// Hunter L
        /// </summary>
        public double HL { get { return HunterLab != null ? HunterLab.L : double.NaN; } }
        /// <summary>
        /// Hunter a
        /// </summary>
        public double Ha { get { return HunterLab != null ? HunterLab.a : double.NaN; } }
        /// <summary>
        /// Hunter b
        /// </summary>
        public double Hb { get { return HunterLab != null ? HunterLab.b : double.NaN; } }
        /// <summary>
        /// WI(ASTM E313-2005)
        /// </summary>
        public double WI_E313 { get; protected set; }
        /// <summary>
        /// T(ASTM E313-2005)
        /// </summary>
        public double T_E313 { get; protected set; }
        /// <summary>
        /// WI(CIE/ISO)
        /// </summary>
        public double WI_CIE_ISO { get; protected set; }
        /// <summary>
        /// T(CIE/ISO)
        /// </summary>
        public double T_CIE_ISO { get; protected set; }
        /// <summary>
        /// WI(Hunter 1942)
        /// </summary>
        public double WI_Hunter1942 { get; protected set; }
        /// <summary>
        /// WI(Hunter 1960)
        /// </summary>
        public double WI_Hunter1960 { get; protected set; }
        /// <summary>
        /// WI(Tappl)
        /// </summary>
        public double WI_Tapple { get; protected set; }
        /// <summary>
        /// WI(Taube)
        /// </summary>
        public double WI_Taube { get; protected set; }
        /// <summary>
        /// WI(R457)
        /// </summary>
        public double WI_R457 { get; protected set; }

        /// <summary>
        /// 伯格白度
        /// </summary>
        public double WI_Berger { get; protected set; }

        public double WI_Granz { get; protected set; }

        /// <summary>
        /// WI(Gost 16873)
        /// </summary>
        public double WI_GOST_16873 { get; protected set; }
        /// <summary>
        /// YI(ASTM D1925-1970)
        /// </summary>
        public double YI_D1925 { get; protected set; }
        /// <summary>
        /// YI(ASTM E313-2005)
        /// </summary>
        public double YI_E313 { get; protected set; }
        /// <summary>
        /// sRGB-R
        /// </summary>
        public double sR { get { return PseudoColor.R; } }
        /// <summary>
        /// sRGB-G
        /// </summary>
        public double sG { get { return PseudoColor.G; } }
        /// <summary>
        /// sRGB-B
        /// </summary>
        public double sB { get { return PseudoColor.B; } }
        /// <summary>
        /// DIN99 Lab L
        /// </summary>
        public double L99 { get { return Din99Lab != null ? Din99Lab.L : double.NaN; } }
        /// <summary>
        /// DIN99 Lab a
        /// </summary>
        public double a99 { get { return Din99Lab != null ? Din99Lab.a : double.NaN; } }
        /// <summary>
        /// DIN99 Lab b
        /// </summary>
        public double b99 { get { return Din99Lab != null ? Din99Lab.b : double.NaN; } }
        /// <summary>
        /// DIN99 Lab C
        /// </summary>
        public double C99 { get { return Din99Lab != null ? Din99Lab.C : double.NaN; } }
        /// <summary>
        /// DIN99 Lab h
        /// </summary>
        public double h99 { get { return Din99Lab != null ? Din99Lab.h : double.NaN; } }
        /// <summary>
        /// 亮度因子
        /// </summary>
        public double LuminanceFactor { get { return XYZ?.Y / 100.0 ?? double.NaN; } }

        public double MH => MunsellHVC?.H ?? double.NaN;
        public double MV { get { return MunsellHVC?.V ?? double.NaN; } }
        public double MC { get { return MunsellHVC?.C ?? double.NaN; } }
        /// <summary>
        /// 反射透射标志
        /// </summary>
        public string TransmissionFlag
        {
            get { return Specimen.IsTransmission ? S._("Transmissive") : S._("Reflective"); }
        }


        // 数据所属的测光方式只可能是SCI或SCE
        public SpecularComponent SpecularComponent
        {
            get; protected set;
        }

        public double? GardnerIndex { get; protected set; }

        public double? IronCobaltIndex { get; protected set; }

        public double? PtCoIndex { get; protected set; }

        public double? Haze { get; protected set; }

        public double MY_Carbon { get; protected set; }

        public double MC_Carbon { get; protected set; }

        public double dM_Carbon { get; protected set; }

        public double ITA { get; protected set; }

        public double Klett { get; protected set; }
        /// <summary>
        /// 糖度值
        /// </summary>
        public double ICUMSA { get; protected set; } = 0.00;

        public double Sayblot { get; protected set; }

        public double AstmD1500 { get; protected set; }

        public double PrecentStrengthSWL { get; protected set; } // % Strength SWL
        public double WavelengthOfPrecentStrengthSWL { get; protected set; } // % Strength SWL

        #region 中国药典

        public double ChpGY { get; protected set; }
        public double ChpYG { get; protected set; }
        public double ChpY { get; protected set; }
        public double ChpOY { get; protected set; }
        public double ChpOR { get; protected set; }
        public double ChpBR { get; protected set; }
        #endregion

        #region 欧洲药典
        public double EU_B { get; protected set; }
        public double EU_BY { get; protected set; }
        public double EU_Y { get; protected set; }
        public double EU_GY { get; protected set; }
        public double EU_R { get; protected set; }
        #endregion

        #region 美国药典
        private int _uspValue;

        public int USP_Value
        {
            get => _uspValue;
            protected set => _uspValue = value;
        }

        public string USP_LetterCode     // 只读辅助属性，供界面或日志用
        {
            get
            {
                int v = USP_Value;          // 0-19
                if (v < 0 || v > 19) return "--";

                char letter = (char)('A' + v);   // 0->A ... 19->T
                return $"{letter}";
            }
        }
        public string USP => USP_LetterCode;
        #endregion

        /// <summary>
        /// 计算所有的值
        /// </summary>
        protected virtual void CalculateValues(ChromaticitySetting setting, SpecularComponent sc, Formula formula)
        {
            Formula = formula;
            SpecularComponent = sc;
            Spectrum = Specimen.GetSpectrum(sc);
            XYZ = GetXyz(Specimen, formula, sc);
            PseudoColor = GetColor(XYZ, formula);

            if (Specimen.HazeDatas != null)
            {
                if (setting.DisplayFields.Contains(ChromaticityField.HazeC2))
                {
                    HazeC2 = GetHazeData(StandardIlluminant.C, StandardObserver.CIE1931)?.Haze ?? double.NaN;
                }
                if (setting.DisplayFields.Contains(ChromaticityField.HazeA2))
                {
                    HazeA2 = GetHazeData(StandardIlluminant.A, StandardObserver.CIE1931)?.Haze ?? double.NaN;
                }
                if (setting.DisplayFields.Contains(ChromaticityField.HazeD65))
                {
                    HazeD65 = GetHazeData(StandardIlluminant.D65, StandardObserver.CIE1931)?.Haze ?? double.NaN;
                }
                if (setting.DisplayFields.Contains(ChromaticityField.TransmittanceC2))
                {
                    TransmittanceC2 = GetHazeData(StandardIlluminant.C, StandardObserver.CIE1931)?.Transmittance * 100 ?? double.NaN;
                }
                if (setting.DisplayFields.Contains(ChromaticityField.TransmittanceA2))
                {
                    TransmittanceA2 = GetHazeData(StandardIlluminant.A, StandardObserver.CIE1931)?.Transmittance * 100 ?? double.NaN;
                }
                if (setting.DisplayFields.Contains(ChromaticityField.TransmittanceD65))
                {
                    TransmittanceD65 = GetHazeData(StandardIlluminant.D65, StandardObserver.CIE1931)?.Transmittance * 100 ?? double.NaN;
                }
                if (setting.DisplayFields.Contains(ChromaticityField.Clarity))
                {
                    TransmittanceD65 = Specimen.Clarity;
                }
            }
            if (setting.DisplayFields.Contains(ChromaticityField.Clarity))
            {
                Clarity = Specimen.Clarity;
            }

            if (XYZ == null)
            {
                return;
            }
            Lab = (setting.NeedLab || setting.NeedDIN99Lab) ? formula.XyzToLab(XYZ) : null;
            Luv = setting.NeedLuv ? GetCIELUV(XYZ, formula) : null;
            CIERGB = setting.NeedRGB ? Formula.XyzToCieRgb(XYZ) : null;
            HunterLab = setting.NeedHunterLab ? formula.XyzToHunterLab(XYZ) : null;
            Din99Lab = setting.NeedDIN99Lab ? formula.LabToDin99(Lab) : null;
            MunsellHVC = setting.NeedMunsell ? Formula.XyzToMunsell(XYZ) : null;
            ColorSpecimen.HazeData hazeData = null;
            foreach (var field in setting.DisplayFields)
            {
                if (field.IsNeedStandard())
                    continue;

                switch (field)
                {
                    case ChromaticityField.WI_E313:
                        WI_E313 = formula.GetWhitenessAstmE313(XYZ);
                        break;
                    case ChromaticityField.T_E313:
                        T_E313 = formula.GetTAstmE313(XYZ);
                        break;
                    case ChromaticityField.WI_CIE_ISO:
                        WI_CIE_ISO = formula.GetWhiteness_CIEISO(XYZ);
                        break;
                    case ChromaticityField.T_CIE_ISO:
                        T_CIE_ISO = formula.GetT_CIEISO(XYZ);
                        break;
                    case ChromaticityField.WI_Hunter1942:
                        WI_Hunter1942 = formula.GetWhitnessHunter1942(HunterLab);
                        break;
                    case ChromaticityField.WI_Hunter1960:
                        WI_Hunter1960 = formula.GetWhitnessHunter1964(HunterLab);
                        break;
                    case ChromaticityField.WI_Tapple:
                        WI_Tapple = formula.GetWhitenessTappl(XYZ);
                        break;
                    case ChromaticityField.WI_Taube:
                        WI_Taube = formula.GetWhitenessTaube(XYZ);
                        break;
                    case ChromaticityField.WI_R457:
                        if (Spectrum != null)
                            WI_R457 = formula.GetWhitnessR457(Spectrum);
                        else
                            WI_R457 = double.NaN;
                        break;
                    case ChromaticityField.WI_Berger:
                        WI_Berger = formula.GetWhitenessBerger(XYZ);
                        break;
                    case ChromaticityField.WI_Granz:
                        WI_Granz = formula.GetWhitenessGanz(XYZ);
                        break;
                    case ChromaticityField.WI_GOST_16873:
                        WI_GOST_16873 = Lab == null ? double.NaN : formula.GetWhitenessGost16873(Lab);
                        break;
                    case ChromaticityField.YI_E313:
                        YI_E313 = formula.GetYellownessAstmE313(XYZ);
                        break;
                    case ChromaticityField.YI_D1925:
                        YI_D1925 = formula.GetYellownessAstmD1925(XYZ);
                        break;
                    case ChromaticityField.GardnerIndex:
                    case ChromaticityField.IronCobaltIndex:
                        {
                            // 铁钴指数与 Gardner 指数算法相同，仅显示名称不同
                            double? index;
                            if (formula.Illuminant == StandardIlluminant.C &&
                                formula.Observer == StandardObserver.CIE1931)
                            {
                                index = Specimen.IsTransmission
                                    ? formula.CalculateGardnerIndex(XYZ)
                                    : (double?)null;
                            }
                            else if (Spectrum != null)
                            {
                                var c2Formula = new Formula(StandardIlluminant.C, StandardObserver.CIE1931);
                                var tmp = c2Formula.CalculateXyz(Spectrum);
                                index = c2Formula.CalculateGardnerIndex(tmp);
                            }
                            else
                            {
                                index = null;
                            }

                            if (index != null && (double)index < 0.0)
                            {
                                index = null;
                            }

                            if (field == ChromaticityField.GardnerIndex)
                                GardnerIndex = index;
                            else
                                IronCobaltIndex = index;
                        }
                        break;
                    case ChromaticityField.PtCoIndex:
                        if (Spectrum != null)
                        {
                            PtCoIndex = Specimen.IsTransmission ? Formula.CalculatePtCoIndex(Spectrum, (int)SystemDefaultConfig.ICUMSAParameter.PathLength) : (double?)null;
                        }
                        else
                        {
                            PtCoIndex = null;
                        }
                        break;

                    case ChromaticityField.ICUMSA:
                        ICUMSA = Spectrum != null ? Formula.clralgo_calculate_ICUMSA(Spectrum, SystemDefaultConfig.ICUMSAParameter.IUThickness == 0 ? 1 : SystemDefaultConfig.ICUMSAParameter.IUThickness,
                                                                                     SystemDefaultConfig.ICUMSAParameter.IUConcentration == 0 ? 1 : SystemDefaultConfig.ICUMSAParameter.IUConcentration) : 0.00;
                        break;

                   
                    case ChromaticityField.MY_Carbon:
                        var xyz = XYZ != null ? XYZ : Lab != null ? Formula.LabToXyz(Lab) : null;
                        MY_Carbon = xyz == null ? double.NaN : xyz.CalculateMy(Formula);
                        break;
                    case ChromaticityField.MC_Carbon:
                        var xyz1 = XYZ != null ? XYZ : Lab != null ? Formula.LabToXyz(Lab) : null;
                        MC_Carbon = xyz1 == null ? double.NaN : xyz1.CalculateMc(Formula);
                        break;
                    case ChromaticityField.dM_Carbon:
                        dM_Carbon = MY_Carbon == double.NaN ? double.NaN : MC_Carbon - MY_Carbon;
                        break;
                    case ChromaticityField.ITA:
                        var lab = spectrum != null ? Formula.CalculateLab(spectrum) : XYZ != null ? Formula.XyzToLab(XYZ) : Lab;
                        ITA = lab == null ? double.NaN : lab.CalculateITA();
                        break;
                    case ChromaticityField.Klett:
#if FALSE
                        if (spectrum == null)
                            Klett = double.NaN;
                        else
                        {
                            var values = new List<double>() { spectrum[400], spectrum[410], spectrum[420], spectrum[430], spectrum[440], spectrum[450] };
                            Klett = 100 - values.Average() * 100;
                        }
#endif
                        var ptCoIndex = Specimen.IsTransmission ? Formula.CalculatePtCoIndex(Spectrum, (int)SystemDefaultConfig.ICUMSAParameter.PathLength) : (double?)null;
                        if (ptCoIndex == null)
                            Klett = double.NaN;
                        else
                        {
                            Klett = Specimen.IsTransmission ? Formula.Calculateklett(XYZ) : 0.0;
                        }

                        break;
                    case ChromaticityField.Sayblot:
                        if (XYZ != null)
                            Sayblot = Formula.CalculateSayblot(XYZ);
                        else
                            Sayblot = double.NaN;
                        break;
                    case ChromaticityField.AstmD1500:
                        if (XYZ != null)
                            AstmD1500 = Formula.CalculateAdtmD1500(XYZ);
                        else
                            AstmD1500 = double.NaN;
                        break;
                    case ChromaticityField.C_T:
                    case ChromaticityField.M_T:
                    case ChromaticityField.Y_T:
                    case ChromaticityField.K_T:
                        var result = Tnh.Density.Formula.CalculateCmyk(Spectrum, Tnh.Density.DensityStatus.ISO_T);
                        if (result != null)
                        {
                            C_T = result[0];
                            M_T = result[1];
                            Y_T = result[2];
                            K_T = result[3];
                        }

                        break;
                    case ChromaticityField.C_E:
                    case ChromaticityField.M_E:
                    case ChromaticityField.Y_E:
                    case ChromaticityField.K_E:
                        result = Tnh.Density.Formula.CalculateCmyk(Spectrum, Tnh.Density.DensityStatus.ISO_E);
                        if (result != null)
                        {
                            C_E = result[0];
                            M_E = result[1];
                            Y_E = result[2];
                            K_E = result[3];
                        }
                        break;
                    case ChromaticityField.C_I:
                    case ChromaticityField.M_I:
                    case ChromaticityField.Y_I:
                    case ChromaticityField.K_I:
                        result = Tnh.Density.Formula.CalculateCmyk(Spectrum, Tnh.Density.DensityStatus.ISO_I);
                        if (result != null)
                        {
                            C_I = result[0];
                            M_I = result[1];
                            Y_I = result[2];
                            K_I = result[3];
                        }
                        break;
                    case ChromaticityField.C_A:
                    case ChromaticityField.M_A:
                    case ChromaticityField.Y_A:
                    case ChromaticityField.K_A:
                        result = Tnh.Density.Formula.CalculateCmyk(Spectrum, Tnh.Density.DensityStatus.ISO_A);
                        if (result != null)
                        {
                            C_A = result[0];
                            M_A = result[1];
                            Y_A = result[2];
                            K_A = result[3];
                        }
                        break;
                    case ChromaticityField.PrecentStrengthSWL:
                        if (Spectrum != null && Specimen != null)
                        {
                            int wavelength = 0;
                            PrecentStrengthSWL = formula.GetColorValueSWL(Spectrum, ref wavelength, Specimen.IsTransmission);
                            WavelengthOfPrecentStrengthSWL = wavelength;
                        }
                        else if (Spectrum != null && Specimen == null)
                        {
                            int wavelength = 0;
                            PrecentStrengthSWL = formula.GetColorValueSWL(Spectrum, ref wavelength, false);
                            WavelengthOfPrecentStrengthSWL = wavelength;
                        }
                        break;

                    #region 8度光泽度
                        case ChromaticityField.Gloss8:
                        if (Specimen.SCI == null || Specimen.SCE == null || MainWindow.WSCI == null || MainWindow.WSCE == null)
                            break;
                            Gloss8 = Formula.CalculateEightDegreesGloss(Specimen.SCI.Spectrum, Specimen.SCE.Spectrum, MainWindow.WSCI.Spectrum, MainWindow.WSCE.Spectrum);
                        break;
                    #endregion
                    //药典的值获取
                    #region 中国药典
                    case ChromaticityField.ChpGY:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        
                        ChpGY = XYZ != null ? Math.Round(Formula.CalculateColorScale(0, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.ChpYG:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        ChpYG = XYZ != null ? Math.Round(Formula.CalculateColorScale(1, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.ChpY:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        ChpY = XYZ != null ? Math.Round(Formula.CalculateColorScale(2, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.ChpOY:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        ChpOY = XYZ != null ? Math.Round(Formula.CalculateColorScale(3, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.ChpOR:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        ChpOR = XYZ != null ? Math.Round(Formula.CalculateColorScale(4, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.ChpBR:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        ChpBR = XYZ != null ? Math.Round(Formula.CalculateColorScale(5, Spectrum), 3) : 0.0;
                        break;
                    #endregion

                    #region 欧洲药典
                    case ChromaticityField.EU_B:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        EU_B = XYZ != null ? Math.Round(Formula.CalculateEUColorScale(0, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.EU_BY:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        EU_BY = XYZ != null ? Math.Round(Formula.CalculateEUColorScale(1, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.EU_Y:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        EU_Y = XYZ != null ? Math.Round(Formula.CalculateEUColorScale(2, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.EU_GY:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        EU_GY = XYZ != null ? Math.Round(Formula.CalculateEUColorScale(3, Spectrum), 3) : 0.0;
                        break;
                    case ChromaticityField.EU_R:
                        if (Spectrum == null)
                        {
                            break;
                        }
                        EU_R = XYZ != null ? Math.Round(Formula.CalculateEUColorScale(4, Spectrum), 3) : 0.0;
                        break;
                    #endregion

                    #region 美国药典
                    case ChromaticityField.USP:
                        {
                            if (Spectrum == null)
                            {
                                break;
                            }
                            USP_Value = XYZ != null ? Formula.CalculateUSAColorScale(Spectrum) : 0;
                        }
                        break;
                        #endregion

                }
            }
        }

        /// <summary>
        /// xyz转Luv
        /// </summary>
        /// <param name="xyz"></param>
        /// <param name="formula">c++类库接口</param>
        /// <returns></returns>
        private CIELUV GetCIELUV(CIEXYZ xyz, Formula formula)
        {
            CIELUV cIELUV = new CIELUV();

            cIELUV.L = formula.XyzToLuv(XYZ).L;

            cIELUV.u = formula.XyzToLuv(XYZ).u;
            cIELUV.v = formula.XyzToLuv(XYZ).v;

            cIELUV.uq = 4.0f * xyz.X / (xyz.X + 15.0f * xyz.Y + 3 * xyz.Z);
            cIELUV.vq = 9.0f * xyz.Y / (xyz.X + 15.0f * xyz.Y + 3 * xyz.Z);

            return cIELUV;
        }
        public double HazeC2 { get; set; }

        public double HazeA2 { get; set; }

        private ColorSpecimen.HazeData GetHazeData(StandardIlluminant illuminant, StandardObserver observer)
        {
            if (Specimen.IsTransmission && (Specimen.GetHazeData(illuminant, observer) is ColorSpecimen.HazeData hazeData)) // 仪器上传的雾度记录
            {
                return hazeData;
            }
            return null;
        }

        public double HazeD65 { get; set; }

        public double Gloss8 { get; set; }

        public double Clarity { get; set; }

        public double C_I { get; set; }
        public double M_I { get; set; }
        public double Y_I { get; set; }
        public double K_I { get; set; }

        public double C_E { get; set; }
        public double M_E { get; set; }
        public double Y_E { get; set; }
        public double K_E { get; set; }

        public double C_T { get; set; }
        public double M_T { get; set; }
        public double Y_T { get; set; }
        public double K_T { get; set; }


        public double C_A { get; set; }
        public double M_A { get; set; }
        public double Y_A { get; set; }
        public double K_A { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double TransmittanceC2 { get; set; }

        public double TransmittanceA2 { get; set; }

        public double TransmittanceD65 { get; set; }

        private double CalculateTransmittance(StandardIlluminant illuminant, StandardObserver observer)
        {
            var formula = new Formula(illuminant, observer);
            var xyz = GetXyz(Specimen, formula, SpecularComponent);
            if (xyz == null) return double.NaN;
            return xyz.Y;
        }
    }
}
