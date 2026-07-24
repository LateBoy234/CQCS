using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// MetamerismIndiexDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MetamerismIndiexDialog : Window, INotifyPropertyChanged
    {
        private StandardSpecimen _standard = null;
        private TestSpecimen _test = null;

        public MetamerismIndiexDialog(MainWindow mainWindow)
        {
            Owner = mainWindow;
            MainWindow = mainWindow;
            Database = mainWindow.Database;
            Observer = mainWindow.Observer;
            Illuminant = mainWindow.Illuminant;
            SpecularComponent = SpecularComponent.SCIAndSCE;

            InitializeComponent();
            DataContext = this;

            ReferenceIlluminant1 = Database.ObserverAndIlluminantSetting.SecondaryIlluminant;
            ReferenceIlluminant2 = Database.ObserverAndIlluminantSetting.TertiaryIlluminant;
            ReferenceObserver1 = Database.ObserverAndIlluminantSetting.SecondaryObserver;
            ReferenceObserver2 = Database.ObserverAndIlluminantSetting.TertiaryObserver;

            _standardComboBox.ItemsSource =
                Database.GetStandards(mainWindow.SpecularComponent, Observer, Illuminant);
            _standardComboBox.SelectedValuePath = "Specimen";
            _standardComboBox.SelectedValue = mainWindow.CurrentStandard;

        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyProertyChanged(string popertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(popertyName));
        }

        public MainWindow MainWindow { get; private set; }

        public SpecimenDatabase Database { get; private set; }

        public StandardObserver Observer { get; private set; }

        public StandardIlluminant Illuminant { get; private set; }

        StandardIlluminant _illuminant1 = StandardIlluminant.A;
        public StandardIlluminant ReferenceIlluminant1
        {
            get { return _illuminant1; }
            set
            {
                if (value != _illuminant1)
                {
                    _illuminant1 = value;
                    NotifyProertyChanged("ReferenceIlluminant1");
                    UpdateMI();
                }
                SCIMI1Label = SCEMI1Label = string.Format("MI-{0}", value);
                NotifyProertyChanged("SCIMI1Label");
                NotifyProertyChanged("SCEMI1Label");
            }
        }

        StandardIlluminant _illuminant2 = StandardIlluminant.F2;
        public StandardIlluminant ReferenceIlluminant2
        {
            get { return _illuminant2; }
            set
            {
                if (value != _illuminant2)
                {
                    _illuminant2 = value;
                    NotifyProertyChanged("ReferenceIlluminant2");
                    UpdateMI();
                }
                SCIMI2Label = SCEMI2Label = string.Format("MI-{0}", value);
                NotifyProertyChanged("SCIMI2Label");
                NotifyProertyChanged("SCEMI2Label");
            }
        }

        StandardObserver _observer1 = StandardObserver.CIE1964;
        public StandardObserver ReferenceObserver1
        {
            get { return _observer1; }
            set
            {
                if (value != _observer1)
                {
                    _observer1 = value;
                    NotifyProertyChanged("ReferenceObserver1");
                    UpdateMI();
                }
            }
        }

        StandardObserver _observer2 = StandardObserver.CIE1964;
        public StandardObserver ReferenceObserver2
        {
            get { return _observer2; }
            set
            {
                if (value != _observer2)
                {
                    _observer2 = value;
                    NotifyProertyChanged("ReferenceObserver2");
                    UpdateMI();
                }
            }
        }

        public SpecularComponent SpecularComponent { get; set; }

        public string SCIMI1Label { get; set; }
        public string SCIMI1 { get; set; }
        public string SCIMI2Label { get; set; }
        public string SCIMI2 { get; set; }
        public string SCEMI1Label { get; set; }
        public string SCEMI1 { get; set; }
        public string SCEMI2Label { get; set; }
        public string SCEMI2 { get; set; }

        void UpdateMI()
        {
            SCIMI1 = null;
            SCIMI2 = null;
            SCEMI1 = null;
            SCEMI2 = null;

            if (_standard != null && _test != null)
            {

                var formula = Database.GetFormula(Observer, Illuminant);
                if (_standard.SCI != null && _test.SCI != null)
                {
                    if (_standard.SCI.Spectrum != null && _test.SCI.Spectrum != null)
                    {
                        SCIMI1 = formula.GetMetamerismIndiex(_standard.SCI.Spectrum,
                            _test.SCI.Spectrum, ReferenceIlluminant1, ReferenceObserver1).ToString("F3");
                        SCIMI2 = formula.GetMetamerismIndiex(_standard.SCI.Spectrum,
                            _test.SCI.Spectrum, ReferenceIlluminant2, ReferenceObserver2).ToString("F3");
                    }
                }
                if (_standard.SCE != null && _test.SCE != null)
                {
                    if (_standard.SCE.Spectrum != null && _test.SCE.Spectrum != null)
                    {
                        SCIMI1 = formula.GetMetamerismIndiex(_standard.SCE.Spectrum,
                            _test.SCE.Spectrum, ReferenceIlluminant1, ReferenceObserver1).ToString("F3");
                        SCIMI2 = formula.GetMetamerismIndiex(_standard.SCE.Spectrum,
                            _test.SCE.Spectrum, ReferenceIlluminant2, ReferenceObserver2).ToString("F3");
                    }
                }
            }
            NotifyProertyChanged("SCIMI1");
            NotifyProertyChanged("SCIMI2");
            NotifyProertyChanged("SCEMI1");
            NotifyProertyChanged("SCEMI2");
        }

        private void OnSpecularComponentChanged(object sender, ValueChangedEventArgs<SpecularComponent> e)
        {
            SpecularComponent = e.NewValue;
            NotifyProertyChanged("SCIVisiblity");
            NotifyProertyChanged("SCEVisiblity");
        }

        public Visibility SCIVisiblity
        {
            get
            {
                if (SpecularComponent == SpecularComponent.SCI 
                    || SpecularComponent == SpecularComponent.SCIAndSCE)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        public Visibility SCEVisiblity
        {
            get
            {
                if (SpecularComponent == SpecularComponent.SCE 
                    || SpecularComponent == SpecularComponent.SCIAndSCE)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        private void OnStandardComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _spectrumPlotControl.Clear();

            if (e.AddedItems.Count == 0)
            {
                _standard = null;
                _testComboBox.ItemsSource = null;
                return;
            }

            _standard = (e.AddedItems[0] as StandardSpecimenAdapter).StandardSpecimen;
            if (_standard != null)
            {
                if (_standard.GetSpectrum(SpecularComponent.SCI) != null)
                {
                    _spectrumPlotControl.AddStandard(
                        _standard.GetSpectrum(SpecularComponent.SCI), S._("Standard_SCI"), Brushes.Blue);
                }
                if (_standard.GetSpectrum(SpecularComponent.SCE) != null)
                {
                    _spectrumPlotControl.AddStandard(
                        _standard.GetSpectrum(SpecularComponent.SCE), S._("Standard_SCE"), Brushes.DarkBlue);
                }

                _testComboBox.SelectedValuePath = "Specimen";
                _testComboBox.ItemsSource = Database.GetTests(_standard,
                    MainWindow.SpecularComponent, Observer, Illuminant);

                if (MainWindow.CurrentTest != null && MainWindow.CurrentTest.StandardSpecimen == _standard)
                {
                    _testComboBox.SelectedValue = MainWindow.CurrentTest;
                }
            }
        }

        private void OnTestComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _spectrumPlotControl.RemoveAllTests();

            TestSpecimenAdapter testAdapter = _testComboBox.SelectedItem as TestSpecimenAdapter;
            if (testAdapter != null)
            {
                _test = testAdapter.TestSpecimen;

                if (_test.GetSpectrum(SpecularComponent.SCI) != null)
                {
                    _spectrumPlotControl.AddTest(
                        _test.GetSpectrum(SpecularComponent.SCI), S._("Test_SCI"), Brushes.Green);
                }
                if (_test.GetSpectrum(SpecularComponent.SCE) != null)
                {
                    _spectrumPlotControl.AddTest(
                        _test.GetSpectrum(SpecularComponent.SCE), S._("Test_SCE"), Brushes.DarkGreen);
                }
            }

            UpdateMI();
        }
    }
}
