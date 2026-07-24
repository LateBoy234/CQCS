using ColorQualityControl.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace ThreeNH.ColorQualityControl.UI
{
    class MatchedColorSampleNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Specimen)
            {
                if (value is TestSpecimen test)
                {
                    if (test.StandardSpecimen != null)
                    {
                        return $"{test.StandardSpecimen.Name}/{test.Name}";
                    }
                    else
                    {
                        return $"/{test.Name}";
                    }
                }
                else if (value is StandardSpecimen standard)
                {
                    return standard.Name;
                }
                //测量完成的试样数据名称显示为Sample加当前测试时间
                return $"Sample-{((Specimen)value).Name}";
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MatchedColorItem
    {
        public string Number { get; set; }
        public string Remark { get; set; }
        public string ColorFormula { get; set; }
        public DateTime DateTime { get; set; }
        public string Material { get; set; }
        public StandardIlluminant Illuminant { get; set; }
        public StandardObserver Observer { get; set; }
        public CIELAB Lab { get; set; }
        public CIEDE1976 Delta { get; set; }
        public byte[] ColorData { get; set; }
        public SpecularComponent ScMode { get; set; }

    }

    /// <summary>
    /// ColorMatchWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ColorMatchWindow : Window, INotifyPropertyChanged
    {

        private ColorDatabase _db;
        private List<MatchedColorItem> _dataView;
        private string _currentLibrary;
        private StandardIlluminant _illuminant = StandardIlluminant.D65;
        private StandardObserver _observer = StandardObserver.CIE1964;
        private SpecularComponent _scMode = SpecularComponent.Unknown;
        private double? _labL;
        private double? _labA;
        private double? _labB;
        private string _sampleName;
        private MatchedColorItem _selectedColorRow;
        private double? _dL;
        private double? _dA;
        private double? _dB;
        private double? _dE;
        private Brush _sampleColor;
        private Brush _storedSampleColor;
        private Specimen _selectedSample;
        private string[] _materials;
        private string _selectedMaterial;
        /* * */
        Notifier _notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 10,
                offsetY: 10);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(1.5),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        public ColorMatchWindow(SpecimenDatabase file = null, string libraryName = null)
        {
            LibraryNames = ColorLibraryInfo.GetLibraryNames().ToArray();
            if (libraryName == null)
            {
                if (LibraryNames.Contains(ColorLibraryInfo.LastUsedLibrary))
                {
                    CurrentLibrary = ColorLibraryInfo.LastUsedLibrary;
                }
                else if (LibraryNames.Length == 1)
                {
                    CurrentLibrary = LibraryNames[0];
                }
            }
            else
            {
                CurrentLibrary = libraryName;
            }
            if (file != null)
            {
                Samples.AddRange(file.StandardSpecimens);
                Samples.AddRange(file.TestSpecimens);
            }
            int unknownCount = Samples.Count(_ => _.Clr != null);
            int sciCount = Samples.Count(_ => _.SCI != null);
            int sceCount = Samples.Count(_ => _.SCE != null);
            if (unknownCount >= sciCount && unknownCount >= sceCount)
            {
                _scMode = SpecularComponent.Unknown;
            }
            else if (sciCount >= sceCount)
            {
                _scMode = SpecularComponent.SCI;
            }
            else
            {
                _scMode = SpecularComponent.SCE;
            }

            if (Instrument is IMeasurementNotification measurementNotification)
            {
                measurementNotification.MeasurementReceived += OnMeasurementReceived;
            }

            InitializeComponent();

            DataContext = this;
            DeltaELimit = 5.0;
        }

        private void OnMeasurementReceived(object sender, MeasurementNotificationArgs e)
        {
            Dispatcher.Invoke(new Action<MeasurementNotificationArgs>((args) =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    var result = args.Result;
                    result.Name = result.DateTime.ToString("yyyy-MM-dd HHmmss");
                    ScMode = result.SpecularComponent == SpecularComponent.SCIAndSCE
                        ? SpecularComponent.SCI
                        : result.SpecularComponent;
                    Samples.Add(result);
                    SelectedSample = result;

                    Search();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                finally
                {
                    Mouse.OverrideCursor = Cursors.Arrow;
                }
            }), e);

        }

        public string[] LibraryNames { get; }

        public StandardIlluminant Illuminant
        {
            get => _illuminant;
            set
            {
                if (value == _illuminant) return;
                _illuminant = value;
                OnPropertyChanged();
            }
        }

        public StandardObserver Observer
        {
            get => _observer;
            set
            {
                if (value == _observer) return;
                _observer = value;
                OnPropertyChanged();
            }
        }

        public SpecularComponent ScMode
        {
            get => _scMode;
            set
            {
                if (value == _scMode) return;
                _scMode = value;
                OnPropertyChanged();
            }
        }

        public ObservableList<Specimen> Samples { get; } = new ObservableList<Specimen>();

        public string CurrentLibrary
        {
            get => _currentLibrary;
            set
            {
                if (value == _currentLibrary) return;
                _currentLibrary = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCurrentSampleChanged(object sender, SelectionChangedEventArgs e)
        {
            SpectrumPlotControl.RemoveAllStandards();

            CIELAB lab = null;
            if (e.AddedItems.Count > 0)
            {
                var sample = (Specimen)e.AddedItems[0];
                lab = sample.GetColorData(ScMode)?.Lab(new Formula(Illuminant, Observer));
                var spectrum = sample.GetSpectrum(ScMode);
                if (spectrum != null)
                {
                    SpectrumPlotControl.AddStandard(spectrum, sample.Name);
                }
                else if (lab == null)
                {
                    var clr = sample.GetColorData(ScMode);
                    if (clr?.Formulas.Length == 1)
                    {
                        var formuls = clr.Formulas[0];
                        Illuminant = formuls.Illuminant;
                        Observer = formuls.Observer;
                        lab = clr.Lab(formuls);
                    }
                }
            }
            Lab_L = lab?.L;
            Lab_a = lab?.a;
            Lab_b = lab?.b;
        }

        private void UpdateSampleColor()
        {
            if (Lab_L != null && Lab_a != null && Lab_b != null)
            {
                var srgb = Formula.LabToSrgb(
                    new CIELAB((double)Lab_L, (double)Lab_a, (double)Lab_b));
                SampleColor = new SolidColorBrush(Color.FromRgb(
                    srgb.R, srgb.G, srgb.B));
            }
        }

        private void OnCurrentLibraryChanged(object sender, SelectionChangedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            if (e.AddedItems.Count > 0)
            {
                _db?.Dispose();
                _db = new ColorDatabase(ColorLibraryInfo.GetDbConnectionString(
                    e.AddedItems[0] as string));
                _db.UpdateColorsLab(_scMode == SpecularComponent.SCIAndSCE ? SpecularComponent.SCI : _scMode,
                    Illuminant, Observer);
                ColorLibraryInfo.LastUsedLibrary = e.AddedItems[0] as string;

                var materials = new List<string>();
                materials.Add("----");
                materials.AddRange(
                        (from row in _db.Colors.AsEnumerable()
                         let m = row.Field<string>("Material")
                         where !string.IsNullOrWhiteSpace(m)
                         select m).Distinct());
                Materials = materials.ToArray();
            }
            else
            {
                _db = null;
                _dataView = null;
            }

            SelectedMaterial = null;

            Mouse.OverrideCursor = Cursors.Arrow;
        }


        public double? Lab_L
        {
            get => _labL;
            set
            {
                if (value.Equals(_labL)) return;
                _labL = value;
                OnPropertyChanged();

                UpdateSampleColor();
            }
        }

        public double? Lab_a
        {
            get => _labA;
            set
            {
                if (value.Equals(_labA)) return;
                _labA = value;
                OnPropertyChanged();
                UpdateSampleColor();
            }
        }

        public double? Lab_b
        {
            get => _labB;
            set
            {
                if (value.Equals(_labB)) return;
                _labB = value;
                OnPropertyChanged();

                UpdateSampleColor();
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            Search();
        }
        private double _deltaELimit;
        public double DeltaELimit
        {
            get => _deltaELimit;
            set
            {
                if (!DataValidate.isNumberWithPoint(value.ToString()) || value <= 0) return;
                _deltaELimit = value;
                OnPropertyChanged();
            }
        }
        private void Search()
        {
            try
            {
                if (_db == null)
                {
                    MessageBox.Show(S._("PleaseSelectedAColorLibrary"));
                    return;
                }

                if (Lab_L != null && Lab_a != null && Lab_b != null)
                {
                    var target = new CIELAB((double)Lab_L, (double)Lab_a, (double)Lab_b);

                    DateTime timestamp = DateTime.Now;
                    double limits = DeltaELimit;
                    IEnumerable<MatchedColorItem> query;
                    if (string.IsNullOrWhiteSpace(SelectedMaterial) || SelectedMaterial == "----")
                    {
                        query =
                            from _ in _db.Colors.AsEnumerable()
                            let lab = new CIELAB(_.Field<double>("L"), _.Field<double>("a"), _.Field<double>("b"))
                            let delta = new CIEDE1976(target, lab)
                            where delta.dE <= limits
                            orderby delta.dE
                            select new MatchedColorItem
                            {
                                Number = _.Field<string>("Number"),
                                DateTime = _.Field<DateTime>("DateTime"),
                                Material = _.Field<string>("Material"),
                                Remark = _.Field<string>("Remark"),
                                ColorFormula = _.Field<string>("ColorFormula"),
                                Illuminant = (StandardIlluminant)_.Field<int>("Illuminant"),
                                Observer = (StandardObserver)_.Field<int>("Observer"),
                                Lab = lab,
                                Delta = delta,
                                ColorData = _.Field<byte[]>("ColorData"),
                                ScMode = (SpecularComponent)_.Field<int>("ScMode")
                            };
                    }
                    else
                    {
                        var material = SelectedMaterial;
                        query =
                            from _ in _db.Colors.AsEnumerable()
                            let lab = new CIELAB(_.Field<double>("L"), _.Field<double>("a"), _.Field<double>("b"))
                            let delta = new CIEDE1976(target, lab)
                            where delta.dE <= limits && string.Compare(_.Field<string>("Material"), material, StringComparison.CurrentCultureIgnoreCase) == 0
                            orderby delta.dE
                            select new MatchedColorItem
                            {
                                Number = _.Field<string>("Number"),
                                DateTime = _.Field<DateTime>("DateTime"),
                                Material = _.Field<string>("Material"),
                                Remark = _.Field<string>("Remark"),
                                ColorFormula = _.Field<string>("ColorFormula"),
                                Illuminant = (StandardIlluminant)_.Field<int>("Illuminant"),
                                Observer = (StandardObserver)_.Field<int>("Observer"),
                                Lab = lab,
                                Delta = delta,
                                ColorData = _.Field<byte[]>("ColorData"),
                                ScMode = (SpecularComponent)_.Field<int>("ScMode")
                            };
                    }

                    DataView = query.ToList();
                    var span = DateTime.Now - timestamp;
                    Trace.TraceInformation($"查询匹配颜色共花 {span.TotalMilliseconds:F3}ms");

                    if (DataView.Count > 0)
                    {
                        SelectedColorRow = DataView[0];
                    }
                    else
                    {
                        SelectedColorRow = null;
                        _notifier.ShowInformation(S._("CannotFindMatchedColor"));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"存样库查寻异常：{ex.Message}");
            }

        }

        public List<MatchedColorItem> DataView
        {
            get => _dataView;
            set
            {
                _dataView = value;
                OnPropertyChanged();
            }
        }

        public MatchedColorItem SelectedColorRow
        {
            get => _selectedColorRow;
            set
            {
                if (Equals(value, _selectedColorRow)) return;
                _selectedColorRow = value;
                OnPropertyChanged();
                SpectrumPlotControl.RemoveAllTests();

                if (value != null)
                {
                    dL = value.Delta.dL;
                    dA = value.Delta.da;
                    dB = value.Delta.db;
                    if (dL != null && dA != null && dB != null)
                    {
                        dE = Math.Sqrt((double)dL * (double)dL + (double)dA * (double)dA + (double)dB * (double)dB);
                    }

                    RefreshToleranceChart();

                    var clrs = ColorDatabase.ToColorData(_selectedColorRow.ColorData);
                    int clrIndex = (int)_selectedColorRow.ScMode;
                    if (clrs[clrIndex]?.Spectrum != null)
                    {
                        SpectrumPlotControl.AddTest(clrs[clrIndex].Spectrum, _selectedColorRow.Number);
                    }
                }
                else
                {
                    dL = null;
                    dA = null;
                    dB = null;
                    dE = null;
                    RefreshToleranceChart();
                }

                UpdateStoredSampleColor();


            }
        }

        void RefreshToleranceChart()
        {
            if (Lab_L == null || Lab_a == null || Lab_b == null)
            {
                ToleranceChartHelper.Show(ToleranceChartView, Illuminant, Observer, ScMode, null);
                return;
            }

            var standard = SelectedSample ?? ToleranceChartHelper.CreateSpecimenFromLab(
                new CIELAB((double)Lab_L, (double)Lab_a, (double)Lab_b), Illuminant, Observer, ScMode);

            var samples = new List<Specimen>();
            if (_selectedColorRow != null)
            {
                var matched = ToleranceChartHelper.CreateSpecimenFromColorData(
                    _selectedColorRow.ColorData, _selectedColorRow.ScMode, _selectedColorRow.Number)
                    ?? ToleranceChartHelper.CreateSpecimenFromLab(
                        _selectedColorRow.Lab, _selectedColorRow.Illuminant, _selectedColorRow.Observer, _selectedColorRow.ScMode);
                if (matched != null)
                    samples.Add(matched);
            }

            ToleranceChartHelper.Show(ToleranceChartView, Illuminant, Observer, ScMode, standard, samples);
        }

        void UpdateStoredSampleColor()
        {
            if (_selectedColorRow != null)
            {
                var srgb = Formula.LabToSrgb(
                    new CIELAB(_selectedColorRow.Lab.L,
                        _selectedColorRow.Lab.a,
                        _selectedColorRow.Lab.b));
                StoredSampleColor = new SolidColorBrush(Color.FromRgb(
                    srgb.R, srgb.G, srgb.B));
            }
        }

        public double? dL
        {
            get => _dL;
            set
            {
                if (value.Equals(_dL)) return;
                _dL = value;
                OnPropertyChanged();
            }
        }

        public double? dA
        {
            get => _dA;
            set
            {
                if (value.Equals(_dA)) return;
                _dA = value;
                OnPropertyChanged();
            }
        }

        public double? dB
        {
            get => _dB;
            set
            {
                if (value.Equals(_dB)) return;
                _dB = value;
                OnPropertyChanged();
            }
        }

        public double? dE
        {
            get => _dE;
            set
            {
                if (value.Equals(_dE)) return;
                _dE = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 试样色
        /// </summary>
        public Brush SampleColor
        {
            get => _sampleColor;
            set
            {
                if (Equals(value, _sampleColor)) return;
                _sampleColor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 存样色
        /// </summary>
        public Brush StoredSampleColor
        {
            get => _storedSampleColor;
            set
            {
                if (Equals(value, _storedSampleColor)) return;
                _storedSampleColor = value;
                OnPropertyChanged();
            }
        }

        private void OnConditionsChanged(object sender, SelectionChangedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _db?.UpdateColorsLab(ScMode, Illuminant, Observer);
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void MeasureButton_OnClick(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var result = Instrument.Measure();
                result.Name = result.DateTime.ToString("yyyy-MM-dd HHmmss");
                ScMode = result.SpecularComponent == SpecularComponent.SCIAndSCE
                    ? SpecularComponent.SCI
                    : result.SpecularComponent;
                Samples.Add(result);
                SelectedSample = result;

                Search();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                //底取消覆盖，让光标回到系统当前应有的状态（尊重后续可能的其他覆盖）
                Mouse.OverrideCursor = null;
            }
        }

        public Specimen SelectedSample
        {
            get => _selectedSample;
            set
            {
                if (Equals(value, _selectedSample)) return;
                _selectedSample = value;
                OnPropertyChanged();
            }
        }

        public string[] Materials
        {
            get => _materials;
            set
            {
                if (Equals(value, _materials)) return;
                _materials = value;
                OnPropertyChanged();
            }
        }

        public string SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                if (value == _selectedMaterial) return;
                _selectedMaterial = value;
                OnPropertyChanged();
            }
        }

        public IMeasurement Instrument => ((MainWindow)App.Current.MainWindow).Colorimeter;

        private void OnMaterialsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        protected override void OnClosed(EventArgs e)
        {
            if (Instrument is IMeasurementNotification measurementNotification)
            {
                measurementNotification.MeasurementReceived -= OnMeasurementReceived;
            }
            _db?.Dispose();
            _notifier.Dispose();
            base.OnClosed(e);
        }

        private void Print_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void PrintToWord_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void ExportToExcel_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void ImportAsStandards_OnClick(object sender, RoutedEventArgs e)
        {

        }
        MainWindow MainWindow
        {
            get { return Application.Current.MainWindow as MainWindow; }
        }

    }
}
