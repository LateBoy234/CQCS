using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommunicationBase;
using ThreeNH.Controls;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// WriteStandardDialog.xaml 的交互逻辑
    /// </summary>
    public partial class StandardInputDialog : Window
    {
        private IMeasurement _colorimeter;

        public StandardInputDialog(WavelengthRange wavelengthRange)
        {
            InitializeComponent();
            specimenInputControl.WavelengthRange = wavelengthRange;
        }

        public StandardInputDialog(IMeasurement colorimeter, 
            SpecimentInputControlValueType valueType=SpecimentInputControlValueType.Reflectance,
            SpecularComponent scMode = SpecularComponent.SCIAndSCE,
            IEnumerable<string> materials=null)
        {
            _colorimeter = colorimeter;
            InitializeComponent();
            MeasureButton.Visibility = Visibility.Visible;
            if (_colorimeter is IDeviceInfo deviceInfo)
            {
                specimenInputControl.WavelengthRange = deviceInfo.WavelengthRange; //_colorimeter.WavelengthRange;
            }
            else
            {
                specimenInputControl.WavelengthRange = new WavelengthRange(400, 700, 10);
            }

            specimenInputControl.ScMode = scMode;
            specimenInputControl.ValueType = valueType;
            specimenInputControl.Materials = materials;
        }


        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
             _specimen = specimenInputControl.GetSpecimen();
            
            if (_specimen == null)
                return;
            _specimen.ID = _specimenID;
            DialogResult = true;
        }
        private int _specimenID;
        public Specimen _specimen;
        public Specimen Specimen
        {
            get
            {
                return _specimen;
            }
            set
            {
                  _specimenID = value.ID;
                  specimenInputControl.SetSpecimen(value);
            }
        }

        private void OnClickMeasureButton(object sender, RoutedEventArgs e)
        {
            if (_colorimeter == null)
            {
                MessageBox.Show("仪器未连接");
                return;
            }
            var oldSpecimen = specimenInputControl.GetSpecimen();
            var name = oldSpecimen?.Name;
             var id = _specimenID;
             var formula = oldSpecimen?.ColorFormula;
            var remark = oldSpecimen?.Remark;
            var material=oldSpecimen?.Material;
            var uv=oldSpecimen?.UV;
            var caliber = oldSpecimen?.MeasurementCaliber;
            //判空,要不提示后继续测量
            if (string.IsNullOrEmpty(name)) return;
            try
            {
                var specimen = _colorimeter.Measure();
                specimen.Name = name;
                if (specimen.Clr?.Spectrum != null)
                {
                    specimenInputControl.WavelengthRange = specimen.Clr.Spectrum.Range;
                }
                specimenInputControl.ScMode = specimen.SpecularComponent;
                specimen.ID = id;
                specimen.ColorFormula = formula;
                specimen.Material = material;
                specimen.Remark = remark;
                if (uv!=null)
                {
                    specimen.UV = (UVFlag)uv;
                }
                if (caliber!=null)
                {
                    specimen.MeasurementCaliber = (MeasurementCaliber)caliber;
                }

                Specimen = specimen;
            }
            catch (CommunicationException exception)
            {
                MessageBox.Show($"测量失败：{S._(exception.Message)}");
            }
        }

    }
}
