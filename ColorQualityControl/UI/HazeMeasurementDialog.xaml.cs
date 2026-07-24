using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.ColorSpecimen;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// HazeMeasurementDialog.xaml 的交互逻辑
    /// </summary>
    public partial class HazeMeasurementDialog : Window, INotifyPropertyChanged
    {
        private IInstrument _instrument;
        private HazeMeasurementWizardPage pageT1;
        private HazeMeasurementWizardPage pageT2;
        private HazeMeasurementWizardPage pageT3;
        private HazeMeasurementWizardPage pageT4;
        private Visibility _measureRefBtnVisibility;

        private ColorData T1 { get; set; } // 入射光总量
        private ColorData T2 { get; set; } // 样品透射光总量
        private ColorData T3 { get; set; } // 仪器散射光量
        private ColorData T4 { get; set; } // 样品和仪器散射光量

        public Specimen Result { get; private set; }

        public Tuple<ColorData, ColorData> HazeRef {get {return new Tuple<ColorData, ColorData>(T1, T3); } }

        public HazeMeasurementDialog(IInstrument instrument, Tuple<ColorData, ColorData> hazeRef)
        {
            _instrument = instrument;
            T1 = hazeRef?.Item1;
            T3 = hazeRef?.Item2;
            InitializeComponent();
            DataContext = this;
        }

        void setContent(UIElement element)
        {
            _panel.Children.Clear();
            _panel.Children.Add(element);
        }

        // Step 2
        private void ShowPageStep2()
        {
            Title = S._("MeasureHazeReference");
            pageT1 = new HazeMeasurementWizardPage(
                _instrument, 
                S._("Step2MeasureIncidentLight"),
                S._("PutReflectanceStandardInPosition"));
            pageT1.Return += PageStep2OnReturn;

            setContent(pageT1);

        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (T1 == null || T3 == null)
            {
                ShowPageStep1();
            }
            else
            {
                ShowPageStep3();
            }
        }

        private void PageStep2OnReturn(object sender, ReturnEventArgs<ColorData> e)
        {
            T1 = e.Result;

            ShowPageStep3();
        }
        // Step 1
        private void ShowPageStep1()
        {
            Title = S._("MeasureHazeReference");
            pageT3 = new HazeMeasurementWizardPage(
                _instrument,
                S._("Step1MeasureLightScatteredByInstrument"),
                S._("PutLightTrapInPosition"));
            pageT3.Return += PageStep1OnReturn;
            
            MeasureRefBtnVisibility = Visibility.Hidden;
            setContent(pageT3);

        }
        
        private void PageStep1OnReturn(object sender, ReturnEventArgs<ColorData> e)
        {
            T3 = e.Result;

            ShowPageStep2();
        }

        // Step 4
        private void ShowPageStep4()
        {
            Title = S._("MeasureHaze");
            pageT2 = new HazeMeasurementWizardPage(
                _instrument,
                S._("Step2MeasureTotalLightTransmittedBySpecimen"),
                S._("PutSpecimenAndReflectanceInPosition"));
            pageT2.Return += PageStep4OnReturn;
            setContent(pageT2);

           
        }

        private void PageStep4OnReturn(object sender, ReturnEventArgs<ColorData> e)
        {
            T2 = e.Result;

            
            if (T1 != null && T2 != null && T3 != null && T4 != null)
            {
                Specimen specimen = new Specimen();
                specimen.Instrument = _instrument.Model;
                specimen.OpticalStruct = _instrument.OpticalStructure;
                specimen.InstrumentSN = _instrument.SN;
                specimen.DateTime = DateTime.Now;
                specimen.IsTransmission = true;
                var setting = _instrument as ISystemSetting;
                if (setting != null)
                {
                    specimen.MeasurementCaliber = setting.Aperture;
                    specimen.LensPosition = setting.LensPosition;
                    specimen.UV = setting.UvMode;
                }
                specimen.Clr = T2;

                var illuminants = new StandardIlluminant[]
                {
                    StandardIlluminant.C, StandardIlluminant.A, StandardIlluminant.D65
                };
                //var hazeDatas = new HazeData[3];
                //for (int i = 0; i < 3; i++)
                //{
                //    var formula = new Formula(illuminants[i], StandardObserver.CIE1931);

                //    var t1 = T1.XYZ(formula);
                //    var t3 = T3.XYZ(formula);
                //    var t2 = T2.XYZ(formula);
                //    var t4 = T4.XYZ(formula);

                //    hazeDatas[0].Haze = Formula.CalculateHaze(t1.Y, t2.Y, t3.Y, t4.Y);
                //    hazeDatas[0].Transmittance = t2.Y / t1.Y;
                //    hazeDatas[0].Illuminant = illuminants[i];
                //    hazeDatas[0].Observer = StandardObserver.CIE1931;
                //}

                //specimen.HazeDatas = hazeDatas;

                var hazeDatas = new List<HazeData>();
                for (int i = 0; i < 3; i++)
                {
                    var hazeData = new HazeData();
                    var formula = new Formula(illuminants[i], StandardObserver.CIE1931);

                    var t1 = T1.XYZ(formula);
                    var t3 = T3.XYZ(formula);
                    var t2 = T2.XYZ(formula);
                    var t4 = T4.XYZ(formula);

                    hazeData.Haze = Formula.CalculateHaze(t1.Y, t2.Y, t3.Y, t4.Y);
                    hazeData.Transmittance = t2.Y / t1.Y;
                    hazeData.Illuminant = illuminants[i];
                    hazeData.Observer = StandardObserver.CIE1931;
                    hazeDatas.Add(hazeData);
                }

                specimen.HazeDatas = hazeDatas.ToArray();

                Result = specimen;
                DialogResult = true;
            }
        }

        // Step 3
        private void ShowPageStep3()
        {
            Title = S._("MeasureHaze");
            pageT4 = new HazeMeasurementWizardPage(
                _instrument,
                S._("Step1MeasureLightScatteredByInstrumentAndSpecimen"),
                S._("PutSpecimenAndLightTrapInPosition"));
            pageT4.Return += PageStep3OnReturn;
            setContent(pageT4);
             
            MeasureRefBtnVisibility = Visibility.Visible;
        }

        private void PageStep3OnReturn(object sender, ReturnEventArgs<ColorData> e)
        {
            T4 = e.Result;

            ShowPageStep4();
        }

        
        private void MeasureRef_OnClick(object sender, RoutedEventArgs e)
        {
            ShowPageStep1();
        }

        public Visibility MeasureRefBtnVisibility
        {
            get { return _measureRefBtnVisibility; }
            private set
            {
                if (value == _measureRefBtnVisibility) return;
                _measureRefBtnVisibility = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
