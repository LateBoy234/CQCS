using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl;
using ThreeNH.ColorQualityControl.Annotations;
using System.Collections.Generic;

namespace ThreeNH.Controls
{
    public enum SpecimentInputControlValueType
    {
        Reflectance = 0,
        ColorSpace,
        //Transmittance,
        //CIELab,
        //CIEXYZ,
        //HunterLab
    }

    public enum SpecimentColorSpaceType
    {
        CIELab,
        CIEXYZ,
        HunterLab,
        CIERGB
    }

    public enum SpecimentTransmittanceType
    {
        Reflective,
        Transmissive
    }


    internal class SpecimentInputControlValueTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(string))
            {
                switch ((SpecimentInputControlValueType)value)
                {
                    case SpecimentInputControlValueType.Reflectance: return S._("Label_Reflectance");
                    case SpecimentInputControlValueType.ColorSpace: return S._("Label_ColorSpace");
                        //case SpecimentInputControlValueType.CIELab: return "CIE Lab";
                        //case SpecimentInputControlValueType.CIEXYZ: return "CIE XYZ";
                        //case SpecimentInputControlValueType.HunterLab: return "Hunter Lab";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class WavelengthRangeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WavelengthRange)
            {
                var range = (WavelengthRange)value;
                return string.Format("{0}~{1}nm", range.Min, range.Max);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                var s = (string)value;
                var match = Regex.Match(s, @"(\d{3})~(\d{3})nm");
                if (match.Success)
                {
                    int min = int.Parse(match.Groups[1].Value);
                    int max = int.Parse(match.Groups[2].Value);
                    if (min < max)
                    {
                        return new WavelengthRange(min, max);
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// SpecimenInputControl.xaml 的交互逻辑
    /// </summary>
    public partial class SpecimenInputControl : UserControl, INotifyPropertyChanged
    {
        private WavelengthRange _wavelengthRange = new WavelengthRange(400, 700, 10);
        private string _material;

        public SpecimenInputControl()
        {
            Loaded += OnLoaded;
            InitializeComponent();
            DataContext = this;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            sciSpectrum.Minimum = sceSpectrum.Minimum = unknownSpectrum.Minimum = WavelengthRange.Min;
            sciSpectrum.Maximum = sceSpectrum.Maximum = unknownSpectrum.Maximum = WavelengthRange.Max;
        }

        private void OnSpecularComponentChanged(
            object sender, ValueChangedEventArgs<SpecularComponent> e)
        {
            if (e.NewValue == SpecularComponent.SCI)
            {
                sciGroupBox.Visibility = Visibility.Visible;
                sceGroupBox.Visibility = Visibility.Collapsed;
                unknownGroupBox.Visibility = Visibility.Collapsed;
            }
            else if (e.NewValue == SpecularComponent.SCE)
            {
                sciGroupBox.Visibility = Visibility.Collapsed;
                sceGroupBox.Visibility = Visibility.Visible;
                unknownGroupBox.Visibility = Visibility.Collapsed;
            }
            else if (e.NewValue == SpecularComponent.SCIAndSCE)
            {
                sciGroupBox.Visibility = Visibility.Visible;
                sceGroupBox.Visibility = Visibility.Visible;
                unknownGroupBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                unknownGroupBox.Visibility = Visibility.Visible;
                sciGroupBox.Visibility = Visibility.Collapsed;
                sceGroupBox.Visibility = Visibility.Collapsed;
            }
        }

        private void OnDataTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
                return;

            ComboBox comboBox = sender as ComboBox;

            var valueType = (SpecimentInputControlValueType)comboBox.SelectedIndex;
            if (valueType == SpecimentInputControlValueType.Reflectance)
            {
                wavelengthRangeGroupBox.Visibility = Visibility.Visible;
                sciSpectrum.Visibility = Visibility.Visible;
                sceSpectrum.Visibility = Visibility.Visible;
                unknownSpectrum.Visibility = Visibility.Visible;

                sciColorValue.Visibility = Visibility.Collapsed;
                sceColorValue.Visibility = Visibility.Collapsed;
                unknownColorValue.Visibility = Visibility.Collapsed;
                illuminantAndObserver.Visibility = Visibility.Collapsed;
                colorSpaceLabel.Visibility = Visibility.Collapsed;
                colorSpaceComboBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                wavelengthRangeGroupBox.Visibility = Visibility.Collapsed;
                sciSpectrum.Visibility = Visibility.Collapsed;
                sceSpectrum.Visibility = Visibility.Collapsed;
                unknownSpectrum.Visibility = Visibility.Collapsed;

                sciColorValue.Visibility = Visibility.Visible;
                sceColorValue.Visibility = Visibility.Visible;
                unknownColorValue.Visibility = Visibility.Visible;
                illuminantAndObserver.Visibility = Visibility.Visible;

                colorSpaceLabel.Visibility = Visibility.Visible;
                colorSpaceComboBox.Visibility = Visibility.Visible;
            }
        }

        public string Material
        {
            get => _material;
            set
            {
                if (!Equals(_material, value))
                {
                    _material = value;
                    OnPropertyChanged(nameof(Material));
                }
            }
        }

        IEnumerable<string> _materals;
        public IEnumerable<string> Materials
        {
            get { return _materals; }
            set
            {
                if (!Equals(value, _materals))
                {
                    _materals = value;
                    OnPropertyChanged(nameof(Materials));
                }
            }
        }
        /// <summary>
        /// 用指定的標樣初始化,只能在控件加載以後調用
        /// </summary>
        /// <param name="specimen"></param>
        public void SetSpecimen(Specimen specimen)
        {
            if (IsInitialized && specimen != null)
            {
                nameTxtBox.Text = specimen.Name;
                measurementCaliberControl.MeasurementCaliber = specimen.MeasurementCaliber;
                uvFlagComboBox.UvFlag = specimen.UV;
                RemarkTextBox.Text = specimen.Remark;
                ColorFormulaTextBox.Text = specimen.ColorFormula;


                specularComponentControl.SpecularComponent = specimen.SpecularComponent;
                if (specimen.GetSpectrum(SpecularComponent.SCI) != null ||
                    specimen.GetSpectrum(SpecularComponent.SCE) != null ||
                    specimen.GetSpectrum(SpecularComponent.Unknown) != null)
                {
                    valueTypeComboBox.SelectedIndex = (int)(specimen.IsTransmission
                        ? SpecimentInputControlValueType.ColorSpace
                        : SpecimentInputControlValueType.Reflectance);

                    var sci = specimen.GetSpectrum(SpecularComponent.SCI);
                    var sce = specimen.GetSpectrum(SpecularComponent.SCE);
                    var clr = specimen.GetSpectrum(SpecularComponent.Unknown);
                    sciSpectrum.Spectrum = sci;
                    sceSpectrum.Spectrum = sce;
                    unknownSpectrum.Spectrum = clr;
                    if (sci != null)
                    {
                        WavelengthRange = sci.Range;
                    }
                    else if (sce != null)
                    {
                        WavelengthRange = sce.Range;
                    }
                    else if (clr != null)
                    {
                        WavelengthRange = clr.Range;
                    }
                }
                else
                {
                    valueTypeComboBox.SelectedIndex = (int)SpecimentColorSpaceType.CIELab;
                    Formula[] formulas;
                    if (specimen.SCI != null)
                    {
                        formulas = specimen.SCI.Formulas;
                    }
                    else if (specimen.SCE != null)
                    {
                        formulas = specimen.SCE.Formulas;
                    }
                    else if (specimen.Clr != null)
                    {
                        formulas = specimen.Clr.Formulas;
                    }
                    else
                    {
                        formulas = null;
                    }
                    if (formulas != null && formulas.Length > 0)
                    {
                        illuminantControl.Illuminant = formulas[0].Illuminant;
                        observerControl.Observer = formulas[0].Observer;

                        var sciLab = specimen.GetColorData(SpecularComponent.SCI)?.Lab(formulas[0]);
                        var sceLab = specimen.GetColorData(SpecularComponent.SCE)?.Lab(formulas[0]);
                        var unknownLab = specimen.GetColorData(SpecularComponent.Unknown)?.Lab(formulas[0]);

                        sciColorValue.ColorSpace = ColorSpace.CIE_LAB;
                        sciColorValue.ColorValue = sciLab;
                        sceColorValue.ColorSpace = ColorSpace.CIE_LAB;
                        sceColorValue.ColorValue = sceLab;
                        unknownColorValue.ColorSpace = ColorSpace.CIE_LAB;
                        unknownColorValue.ColorValue = unknownLab;
                    }
                }
            }
        }

        public ValueType ValueType
        {
            get { return (SpecimentInputControlValueType)valueTypeComboBox.SelectedIndex; }
            set { valueTypeComboBox.SelectedIndex = (int)value; }
        }

        public SpecularComponent ScMode
        {
            get { return specularComponentControl.SpecularComponent; }
            set { specularComponentControl.SpecularComponent = value; }
        }

        /// <summary>
        /// 獲取用戶輸入的樣品
        /// </summary>
        /// <returns></returns>
        public Specimen GetSpecimen()
        {
            if (string.IsNullOrWhiteSpace(nameTxtBox.Text))
            {
                MessageBox.Show(S._("PleaseInputName"));
                nameTxtBox.Focus();
                return null;
            }

            Specimen specimen = new Specimen();
            specimen.Name = nameTxtBox.Text;
            specimen.Remark = RemarkTextBox.Text;
            specimen.UV = uvFlagComboBox.UvFlag;
            specimen.MeasurementCaliber = measurementCaliberControl.MeasurementCaliber;
            specimen.ColorFormula = ColorFormulaTextBox.Text;
            specimen.Material = Material;

            var valueType = (SpecimentInputControlValueType)valueTypeComboBox.SelectedIndex;
            if (specularComponentControl.SpecularComponent == SpecularComponent.SCI ||
                specularComponentControl.SpecularComponent == SpecularComponent.SCIAndSCE)
            {
                ColorData sci = new ColorData();
                if (valueType == SpecimentInputControlValueType.Reflectance)
                {
                    sci.Spectrum = sciSpectrum.Spectrum;
                }
                else
                {
                    Formula formula = new Formula(illuminantControl.Illuminant, observerControl.Observer);

                    CIEXYZ xyz = null;
                    object colorValue = sciColorValue.ColorValue;
                    if (colorValue is CIEXYZ)
                    {
                        xyz = colorValue as CIEXYZ;
                        if ((xyz.X < 0 || xyz.X > 150) || (xyz.Y < 0 || xyz.Y > 150) || (xyz.Z < 0 || xyz.Z > 150))
                        {
                            MessageBox.Show($"{S._("InvalidValue")}X[0~150],Y[0~150],Z[0~150]");
                            return null;
                        }
                    }
                    else if (colorValue is CIELAB lab)
                    {
                        if ((lab.L < 0 || lab.L > 150) || (lab.a < -150 || lab.a > 150) || (lab.b < -150 || lab.b > 150))
                        {
                            MessageBox.Show($"{S._("InvalidValue")}L[0~150],a[-150~150],b[-150~150]");
                            return null;
                        }
                        xyz = formula.LabToXyz(colorValue as CIELAB);
                    }
                    else if (colorValue is HunterLab hunterLab)
                    {
                        if ((hunterLab.L < 0 || hunterLab.L > 150) || (hunterLab.a < -150 || hunterLab.a > 150) || (hunterLab.b < -150 || hunterLab.b > 150))
                        {
                            MessageBox.Show($"{S._("InvalidValue")}L[0~150],a[-150~150],b[-150~150]");
                            return null;
                        }
                        xyz = formula.HunterLabToXyz(colorValue as HunterLab);
                    }
                    else if (colorValue is CIERGB cieRGB)
                    {
                        if ((cieRGB.R > 255 || cieRGB.R < 0) || (cieRGB.G > 255 || cieRGB.G < 0) || (cieRGB.B > 255 || cieRGB.B < 0))
                        {
                            MessageBox.Show($"{S._("InvalidValue")}L[0~255],a[0-255],b[0-255]");
                            return null;
                        }
                        xyz = Formula.CieRgbToXyz(cieRGB);
                    }
                    else
                    {
                        MessageBox.Show(S._("PleaseInputSciData"));
                        return null;
                    }
                    sci.Set(formula.Observer, formula.Illuminant, xyz);
                }
                specimen.SCI = sci;
            }
            if (specularComponentControl.SpecularComponent == SpecularComponent.SCE ||
                specularComponentControl.SpecularComponent == SpecularComponent.SCIAndSCE)
            {
                ColorData sce = new ColorData();
                if (valueType == SpecimentInputControlValueType.Reflectance)
                {
                    sce.Spectrum = sceSpectrum.Spectrum;
                }
                else
                {
                    Formula formula = new Formula(illuminantControl.Illuminant, observerControl.Observer);

                    CIEXYZ xyz = null;
                    object colorValue = sceColorValue.ColorValue;
                    if (colorValue is CIEXYZ)
                    {
                        xyz = colorValue as CIEXYZ;
                        if ((xyz.X < 0 || xyz.X > 150) || (xyz.Y < 0 || xyz.Y > 150) || (xyz.Z < 0 || xyz.Z > 150))
                        {
                            MessageBox.Show($"{S._("InvalidValue")}X[0~150],Y[0~150],Z[0~150]");
                            return null;
                        }
                    }
                    else if (colorValue is CIELAB lab)
                    {
                        if ((lab.L < 0 || lab.L > 150) || (lab.a < -150 || lab.a > 150) || (lab.b < -150 || lab.b > 150))
                        {
                            MessageBox.Show($"{S._("InvalidValue")}L[0~150],a[-150~150],b[-150~150]");
                            return null;
                        }
                        xyz = formula.LabToXyz(lab);
                    }
                    else if (colorValue is HunterLab hunterLab)
                    {
                        if ((hunterLab.L < 0 || hunterLab.L > 150) || (hunterLab.a < -150 || hunterLab.a > 150) || (hunterLab.b < -150 || hunterLab.b > 150))
                        {
                            MessageBox.Show($"{S._("InvalidValue")}L[0~150],a[-150~150],b[-150~150]");
                            return null;
                        }
                        xyz = formula.HunterLabToXyz(hunterLab);
                    }
                    else if (colorValue is CIERGB cieRGB)
                    {
                        if ((cieRGB.R > 255 || cieRGB.R < 0) || (cieRGB.G > 255 || cieRGB.G < 0) || (cieRGB.B > 255 || cieRGB.B < 0))
                        {
                            MessageBox.Show($"{S._("InvalidValue")}L[0~255],a[0-255],b[0-255]");
                            return null;
                        }
                        xyz = Formula.CieRgbToXyz(cieRGB);
                    }
                    else
                    {
                        MessageBox.Show(S._("PleaseInputSceData"));
                        return null;
                    }
                    sce.Set(formula.Observer, formula.Illuminant, xyz);
                }
                specimen.SCE = sce;
            }

            if (specularComponentControl.SpecularComponent == SpecularComponent.Unknown)
            {
                ColorData clr = new ColorData();
                if (valueType == SpecimentInputControlValueType.Reflectance)
                {
                    clr.Spectrum = unknownSpectrum.Spectrum;
                }
                else
                {
                    Formula formula = new Formula(illuminantControl.Illuminant, observerControl.Observer);

                    CIEXYZ xyz = null;
                    object colorValue = unknownColorValue.ColorValue;
                    if (colorValue is CIEXYZ)
                        xyz = colorValue as CIEXYZ;
                    else if (colorValue is CIELAB)
                        xyz = formula.LabToXyz(colorValue as CIELAB);
                    else if (colorValue is HunterLab)
                        xyz = formula.HunterLabToXyz(colorValue as HunterLab);
                    else
                    {
                        MessageBox.Show(S._("PleaseInputColorData"));
                        return null;
                    }
                    clr.Set(formula.Observer, formula.Illuminant, xyz);
                }
                specimen.Clr = clr;
            }

            var isTransmission = (SpecimentTransmittanceType)transmissionCombobox.SelectedIndex;
            specimen.IsTransmission = isTransmission == SpecimentTransmittanceType.Transmissive;

            return specimen;
        }

        public WavelengthRange WavelengthRange
        {
            get => _wavelengthRange;
            set
            {
                if (value.Equals(_wavelengthRange)) return;
                _wavelengthRange = value;
                OnPropertyChanged();
            }
        }

        private void WavelengthRange_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sciSpectrum.Minimum = WavelengthRange.Min;
            sciSpectrum.Maximum = WavelengthRange.Max;

            sceSpectrum.Minimum = WavelengthRange.Min;
            sceSpectrum.Maximum = WavelengthRange.Max;

            unknownSpectrum.Minimum = WavelengthRange.Min;
            unknownSpectrum.Maximum = WavelengthRange.Max;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnColorSpaceTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
                return;
            ComboBox comboBox = sender as ComboBox;
            var colorSpace = (SpecimentColorSpaceType)comboBox.SelectedIndex;
            switch (colorSpace)
            {
                case SpecimentColorSpaceType.CIELab: // CIE LAB
                    sciColorValue.ColorSpace = sceColorValue.ColorSpace = ColorSpace.CIE_LAB;
                    break;
                case SpecimentColorSpaceType.CIEXYZ: // CIE XYZ
                    sciColorValue.ColorSpace = sceColorValue.ColorSpace = ColorSpace.CIE_XYZ;
                    break;
                case SpecimentColorSpaceType.HunterLab: // Hunter Lab
                    sciColorValue.ColorSpace = sceColorValue.ColorSpace = ColorSpace.HUNTER_LAB;
                    break;
                case SpecimentColorSpaceType.CIERGB:
                    sciColorValue.ColorSpace = sceColorValue.ColorSpace = ColorSpace.CIE_RGB;
                    break;
            }
        }

        private void OnTransmissionTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
