using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
using System.Windows.Shapes;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorQualityControl.Annotations;
using ThreeNH.ColorSpecimen;
using ThreeNH.Controls;
using ThreeNH.Helper;
using ThreeNH.Optical;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace ThreeNH.ColorQualityControl.UI
{
    /// <summary>
    /// LibraryColorInputWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LibraryColorInputWindow : Window, INotifyPropertyChanged
    {
        private ColorDatabase _db;
        private string _currentLibrary;
        private DataRow _dataRow;
        private Brush _sampleColor;
        private DataView _dataView;

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

        private bool _autoClearAfterStored;

        public LibraryColorInputWindow(IInstrument instrument)
        {
            Instrument = instrument;
            IsConnected = instrument!=null&& instrument.IsOpen;
            LibraryNames = ColorLibraryInfo.GetLibraryNames().ToArray();
            if (LibraryNames.Contains(ColorLibraryInfo.LastUsedLibrary))
            {
                CurrentLibrary = ColorLibraryInfo.LastUsedLibrary;
            }
            else if (LibraryNames.Length == 1)
            {
                CurrentLibrary = LibraryNames[0];
            }

            if (Instrument is IMeasurementNotification measurementNotification)
            {
                measurementNotification.MeasurementReceived += OnMeasurementReceived;
            }

            AutoClearAfterStored = ColorLibraryInfo.AutoClearAfterStored;
            InitializeComponent();

            DataContext = this;
        }

        private void OnMeasurementReceived(object sender, MeasurementNotificationArgs e)
        {
            Dispatcher.Invoke(new Action<MeasurementNotificationArgs>(args =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    AddNewColor(args.Result);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                finally
                {
                    Mouse.OverrideCursor = Cursors.Arrow;
                }
            }), e);
        }

        private void AddNewColor(Specimen sample)
        {
            _db.Colors.RejectChanges();

            var number = NumberTextBox.Text;

            sample.Name = number;
            sample.Remark = RemarkTextBox.Text;
            sample.ColorFormula = ColorFormulaTextBox.Text;
            sample.Material = MaterialComboBox.Text;

            var row =_db.ToColorRow(sample);
            row["Number"] = number;
            DataRow = row;
            if (DataRow.RowState == DataRowState.Detached)
            {
                _db.Colors.Rows.Add(DataRow);
            }

            if (string.IsNullOrWhiteSpace(number))
            {
                MessageBox.Show(S._("PleaseInputTheColorNumber"));
                NumberTextBox.Focus();
                return;
            }

            _db.Update(_db.Colors);

            _notifier.ShowSuccess(S._("HasStored"));
            if (AutoClearAfterStored)
            {
                ClearInputs();
            }
        }

        public string[] LibraryNames { get; set; }

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

        public DataRow DataRow
        {
            get => _dataRow;
            set
            {
                if (Equals(value, _dataRow)) return;
                _dataRow = value;
                OnPropertyChanged();

                UpdateDisplay();
            }
        }

        private void Libraries_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            Materials.Clear();
            if (e.AddedItems.Count > 0)
            {
                DateTime timestamp = DateTime.Now;
                _db?.Dispose();
                _db = new ColorDatabase(ColorLibraryInfo.GetDbConnectionString(
                    e.AddedItems[0] as string));
                TimeSpan span = DateTime.Now - timestamp;
                Trace.TraceInformation($"加载数据用时{span.TotalMilliseconds:F1}ms");
                timestamp = DateTime.Now;
                _db.UpdateColorsLab(null, StandardIlluminant.D65, StandardObserver.CIE1964);
                _dataView = new DataView(_db.Colors, null, "Number", DataViewRowState.OriginalRows);
                Trace.TraceInformation($"更新Lab值用时：{span.TotalMilliseconds:F1}ms");
                ColorLibraryInfo.LastUsedLibrary = e.AddedItems[0] as string;
                var materials =
                    (from _ in _db.Colors.AsEnumerable()
                        select _.Field<string>("Material")).Distinct();
                foreach (var material in materials)
                {
                    if (!string.IsNullOrWhiteSpace(material))
                        Materials.Add(material);
                }
            }
            else
            {
                _db = null;
            }
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void MeasureButton_OnClick(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var sample = Instrument.Measure();
                AddNewColor(sample);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }


        private void UpdateDisplay()
        {
            SpectrumPlotControl.Clear();

            if (DataRow != null)
            {
                var illuminant = (StandardIlluminant) DataRow.Field<int>("Illuminant");
                var observer = (StandardObserver) DataRow.Field<int>("Observer");
                double l = DataRow.Field<double>("L");
                double a = DataRow.Field<double>("a");
                double b = DataRow.Field<double>("b");
                var srgb = Formula.LabToSrgb(
                    new CIELAB(l, a, b));
                SampleColor = new SolidColorBrush(Color.FromRgb(
                    srgb.R, srgb.G, srgb.B));

                var scMode = (SpecularComponent) DataRow.Field<int>("ScMode");
                var clr = ColorDatabase.ToColorData(DataRow.Field<byte[]>("ColorData"))[(int) scMode];
                if (clr?.Spectrum != null)
                {
                    SpectrumPlotControl.AddStandard(clr.Spectrum, "");
                }

                SpectrumPlotGroupBox.Header =
                    scMode == SpecularComponent.Unknown ? S._("Label_SpectrumDiagram") : S._("Label_SpectrumDiagram") + $"({scMode.Description()})";
                PseudoColorGroupBox.Header = S._("PseudoColor") +
                    $"({illuminant}/{observer.Description()})";
            }
            else
            {
                SampleColor = Brushes.Transparent;
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

        private bool _isConnected;
        /// <summary>
        /// 试样色
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (Equals(value, _isConnected)) return;
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        private void Update_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataRow != null)
            {
                if (SaveChange())
                {
                    _notifier.ShowSuccess(S._("HasStored"));
                }
            }
        }

        private bool SaveChange()
        {
            try
            {
                var number = NumberTextBox.Text;

                if (string.IsNullOrWhiteSpace(number))
                {
                    MessageBox.Show(S._("PleaseInputTheColorNumber"));
                    NumberTextBox.Focus();
                    return false;
                }

                if (!_db.Colors.Rows.Contains(DataRow["Id"]))
                {
                    _db.Colors.Rows.Add(DataRow);
                }

                _db.Update(_db.Colors);

                var material = DataRow.Field<string>("Material");
                if (!string.IsNullOrWhiteSpace(material) && !Materials.Contains(material))
                {
                    Materials.Add(material);
                }

                if (AutoClearAfterStored)
                {
                    ClearInputs();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataRow != null)
            {
                try
                {
                    DataRow.Delete();
                    _db.Update(_db.Colors);
                    DataRow = null;
                    UpdateDisplay();

                    _notifier.ShowSuccess(S._("HasDeleted"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void Search_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NumberTextBox.Text))
            {
                MessageBox.Show(S._("PleaseInputNumber"));
                NumberTextBox.Focus();
                return;
            }

            var timestamp = DateTime.Now;
            var number = NumberTextBox.Text.Trim();
            int index = _dataView.Find(number);
            if (index < 0)
            {
                DataRow = null;
                _notifier.ShowInformation( S._("CannotFoundSpecifiedColorNumber"));
            }
            else
            {
                DataRow = (_dataView[index] as DataRowView)?.Row;
            }

            var span = DateTime.Now - timestamp;
            Trace.TraceInformation($"查找用时{span.TotalMilliseconds:F1}");
        }

        public ObservableCollection<string> Materials { get; set; } = new ObservableCollection<string>();

        public IMeasurement Instrument;

        protected override void OnClosed(EventArgs e)
        {
            if (Instrument is IMeasurementNotification measurementNotification)
            {
                measurementNotification.MeasurementReceived -= OnMeasurementReceived;
            }
            _db?.Dispose();
            _notifier.Dispose();
            ColorLibraryInfo.AutoClearAfterStored = AutoClearAfterStored;
            base.OnClosed(e);
        }

        private void InputButton_OnClick(object sender, RoutedEventArgs e)
        {
            StandardInputDialog dlg = new StandardInputDialog(
                Instrument, SpecimentInputControlValueType.ColorSpace, SpecularComponent.Unknown, Materials);
            dlg.Owner = this;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                try
                {

                    if (_db==null)
                    {
                        MessageBox.Show(S._("PleaseSetCurrentLibrary"), Title,
                                              MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    DataRow = _db.ToColorRow(dlg.Specimen);
                    _db.Colors.Rows.Add(DataRow);
                    _db.Update(_db.Colors);
                }
                catch
                {
                    MessageBox.Show( S._("ColorNumberInvalid"), Title,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ClearInputs_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
        }

        private void ClearInputs()
        {
            DataRow = null;
            NumberTextBox.Text = null;
            MaterialComboBox.Text = null;
            RemarkTextBox.Text = null;
            ColorFormulaTextBox.Text = null;
            DateTimeTextBlock.Text = null;
        }

        public bool AutoClearAfterStored
        {
            get => _autoClearAfterStored;
            set
            {
                if (value == _autoClearAfterStored) return;
                _autoClearAfterStored = value;
                OnPropertyChanged();
            }
        }
    }
}