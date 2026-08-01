using ClosedXML.Excel;
using MahApps.Metro.Controls;//MetroWindow
using PanTongColorMatchSearch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorLibrary;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.ColorSpecimen;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.Spectrophotometer;
using ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer;
using ThreeNH.YL30;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using Tolerance = ThreeNH.DataModel.Tolerance;
//using System.Windows.Forms;

namespace ThreeNH.ColorQualityControl.UI.ColourAtla
{
    public class ToastInfo
    {
        private Window _win;
        private Notifier _notifier;
        private bool _isDisposed = false;

        // 延迟初始化，避免窗口未准备好就创建 Notifier
        private Notifier GetNotifier()
        {
            if (_notifier == null && _win != null && _win.IsLoaded)
            {
                _notifier = new Notifier(cfg =>
                {
                    cfg.PositionProvider = new WindowPositionProvider(
                        parentWindow: _win,
                        corner: Corner.TopRight,
                        offsetX: 10,
                        offsetY: 10
                    );
                    cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                        notificationLifetime: TimeSpan.FromSeconds(3),
                        maximumNotificationCount: MaximumNotificationCount.FromCount(5)
                    );
                    cfg.Dispatcher = Application.Current.Dispatcher;
                });
            }
            return _notifier;
        }
        public void SetWindow(Window window)
        {
            _win = window;
        }
        public void ShowToastInfo(string msg)
        {
            // 关键检查：窗口是否有效
            if (_isDisposed || _win == null || !_win.IsLoaded)
                return;

            var notifier = GetNotifier();
            notifier?.ShowInformation(msg);
        }

        public void ShowToastError(string msg)
        {
            if (_isDisposed || _win == null || !_win.IsLoaded)
                return;

            var notifier = GetNotifier();
            notifier?.ShowError(msg);
        }

        public void DisposeToastNotificationsObject()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _notifier?.Dispose();
            _notifier = null;
        }

        public ToastInfo(Window win)
        {
            _win = win;
            // 窗口关闭时自动清理
            if (win != null)
            {
                win.Closed += (s, e) => DisposeToastNotificationsObject();
            }
        }
    }
    public class MatchedColorItem
    {
        public CIELAB Lab { get; set; }
        public Specimen Sample { get; set; }
        public string Name { get; set; }
        public double Delta { get; set; }
    }
    /// <summary>
    /// PanTongMatch.xaml 的交互逻辑
    /// </summary>
    public partial class PanTongMatch : Window, INotifyPropertyChanged
    {
        #region Property Begin
        public List<StandardIlluminant> IlluminantList { get; set; }
        public StandardIlluminant CurrentIllum { get; set; }
        public ObservableList<string> ObserverList { get; set; }
        public StandardObserver CurrentObserEnumType { get; private set; }

        private string _obser;
        public string CurrentObser
        {
            get => _obser;
            set
            {
                _obser = value;
                CurrentObserEnumType = value == "2" ? StandardObserver.CIE1931 : StandardObserver.CIE1964;
            }
        }

        public ObservableCollection<string> SpecularComponentList { get; set; } = new ObservableCollection<string>();
        private string _currentSpecCom;
        public string CurrentSpecCom
        {
            get => _currentSpecCom;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                _currentSpecCom = value;
                NotifyOfPropertyChanged(nameof(CurrentSpecCom));
            }
        }

        private List<string> _filterKeyList;
        public List<string> FilterList { get; set; }
        public string CurrentFilter { get; set; }

        public ObservableCollection<ColourAtlaCollection> ColourAtlaCollections { get; set; } = new ObservableCollection<ColourAtlaCollection>();

        private ColourAtlaCollection _colourAtlaCollection;
        public ColourAtlaCollection ColourAtlaCollection
        {
            get => _colourAtlaCollection;
            set
            {
                _colourAtlaCollection = value;
                NotifyOfPropertyChanged(nameof(ColourAtlaCollection));
                if (value != null)
                {
                    SpecularComponentList.Clear();
                    if (value.Name.Contains("SCI"))
                    {
                        IfNot45_0 = true;
                        SpecularComponentList.Add("SCI");
                    }
                    else if (value.Name.Contains("I+E"))
                    {
                        IfNot45_0 = true;
                        SpecularComponentList.Add("SCI");
                        SpecularComponentList.Add("SCE");
                    }
                    else if (value.Name.Contains("450"))
                    {
                        IfNot45_0 = false;
                        SpecularComponentList.Add("45/0");
                    }
                    CurrentSpecCom = SpecularComponentList.FirstOrDefault();
                }
            }
        }

        private double? _labL;
        public double? Lab_L
        {
            get => _labL;
            set
            {
                _labL = value;
                NotifyOfPropertyChanged(nameof(Lab_L));
            }
        }

        private double? _labA;
        public double? Lab_a
        {
            get => _labA;
            set
            {
                _labA = value;
                NotifyOfPropertyChanged(nameof(Lab_a));
            }
        }

        private double? _labB;//测量得到的
        public double? Lab_b
        {
            get => _labB;
            set
            {
                _labB = value;
                NotifyOfPropertyChanged(nameof(Lab_b));
            }
        }
        private string _sampleName;
        private MatchedColorItem _selectedColorRow;
        private Specimen _selectedSample;
        private ObservableCollection<Specimen> _sampleList;
        public ObservableCollection<Specimen> SampleList
        {
            get => _sampleList;
            set
            {
                _sampleList = value;
                NotifyOfPropertyChanged(nameof(SampleList));
            }
        }
        public Specimen CurrentSampleName { get; set; }

        private int _currentSampleIndex;
        public int CurrentSampleIndex
        {
            get => _currentSampleIndex;
            set
            {
                _currentSampleIndex = value;
                NotifyOfPropertyChanged(nameof(CurrentSampleIndex));
            }
        }
        private bool _progressEnable;
        public bool IsProgressRingActiveLilin
        {
            get => _progressEnable;
            set
            {
                _progressEnable = value;
                NotifyOfPropertyChanged(nameof(IsProgressRingActiveLilin));
            }
        }

        public List<string> ExcelFileNameList { get; set; }
        private List<string> _excelFilePathList;
        public string ExcelFilePath;

        /// <summary>
        /// 试样色
        /// </summary>
        private Brush _targetColor;
        public Brush TargetColor
        {
            get => _targetColor;
            set
            {
                _targetColor = value;
                NotifyOfPropertyChanged(nameof(TargetColor));
            }
        }
        /// <summary>
        /// 存样色
        /// </summary>
        private Brush _matchedColor;
        public Brush MatchedBestSampleColor
        {
            get => _matchedColor;
            set
            {
                _matchedColor = value;
                NotifyOfPropertyChanged(nameof(MatchedBestSampleColor));
            }
        }


        public IList<PanTongColor> PanTongColorResult => ColourAtlaCollection.ColourAtlas;
        public IList<PanTongColor> ColorResult_Coated { get; private set; }
        public IList<PanTongColor> ColorResul_Uncoated { get; private set; }
        public string CurrentFormula { get; private set; }
        private List<string> _nameList;
        private List<MatchedColorItem> _matchColorList;


        private CreateTableFromConfig _createTableFromConfig;

        private DataTable _dataTable;
        public DataTable DataTableLilin
        {
            get => _dataTable;
            set
            {
                _dataTable = value;
                NotifyOfPropertyChanged(nameof(DataTableLilin));
            }
        }
        private Specimen _sample;
        private CurrentSetting _currentSetting;
        #endregion Property End
        private IInstrument _instrument;
        private ToastInfo _toastInfo;
        public bool IfNot45_0;
        public PanTongMatch(CurrentSetting currentSetting, IInstrument instrument, Specimen specimen = null)
        {
            _currentSetting = currentSetting;
            _instrument = instrument;
            InitializeComponent();
            DataContext = this;
            InitViewList();
            LoadColourAtlaCollections();
            ObtainIllumObser();
            _createTableFromConfig = new CreateTableFromConfig(currentSetting, this);
            DataTableLilin = TableAdapter.CreateTableFromKnownTable(_createTableFromConfig.DataTable);
            InitCommands();
            InitContextMenu();
            _toastInfo = new ToastInfo(this);
            //如果有传入样品，则直接显示
            if (specimen != null)
            {
                _sample = specimen;
                SampleList.Add(_sample);
                CurrentSampleName = specimen;
                CurrentSampleIndex = SampleList.Count - 1;
            }
            RefreshToleranceChart();
        }

        private void LoadColourAtlaCollections()
        {

            var optical = _instrument?.OpticalStructure == "45/0" ? "450" : "D8";
            var directory = "Resources";

            foreach (var file in Directory.GetFiles(directory, "*.Json"))
            {
                if (_instrument == null)
                {
                    var colourAtla = ColourAtlaCollection.Load(file);
                    ColourAtlaCollections.Add(colourAtla);
                }
                else if (file.Contains(optical))
                {
                    var colourAtla = ColourAtlaCollection.Load(file);
                    ColourAtlaCollections.Add(colourAtla);
                }
            }
            ColourAtlaCollection = ColourAtlaCollections.FirstOrDefault();
        }

        protected override void OnActivated(EventArgs e)
        {
            _toastInfo.SetWindow(this);

        }
        private void ObtainIllumObser()
        {
            CurrentIllum = _currentSetting.IlluObs.Illuminant;
            CurrentObserEnumType = _currentSetting.IlluObs.Observer;
            CurrentObser = CurrentObserEnumType == StandardObserver.CIE1931 ? "2" : "10";

            // 色库 setter 已根据 JSON 库的实际数据确定测光方式。离线状态下没有
            // instrument，不能再用 instrument/IfNot45_0 覆盖为 45/0。
            string preferredSpecCom = _currentSetting.SpecCom == SpecularComponent.SCE ? "SCE" :
                (IfNot45_0 ? "SCI" : "45/0");
            if (SpecularComponentList.Contains(preferredSpecCom))
                CurrentSpecCom = preferredSpecCom;
            else if (string.IsNullOrEmpty(CurrentSpecCom))
                CurrentSpecCom = SpecularComponentList.FirstOrDefault();
        }
        private void InitViewList()
        {
            SampleList = new ObservableCollection<Specimen>();
            if (_instrument == null)
            {
                ExcelFileNameList = new List<string>(
                   new string[]
                   {
                        "YS3060_D8_PT_Solid Coated",
                        "YS3060_D8_PT_Solid Uncoated",
                          "YD5010_450_Solid Coated",
                        "YD5010_450_Solid Uncoated",
                   }
                   );
            }
            else
            {
                IfNot45_0 = _instrument.OpticalStructure != "45/0";

                if (IfNot45_0)
                {
                    ExcelFileNameList = new List<string>(
                        new string[]
                        {
                        "YS3060_D8_PT_Solid Coated",
                        "YS3060_D8_PT_Solid Uncoated",
                        }
                        );
                }
                else
                {
                    ExcelFileNameList = new List<string>(
                        new string[]
                        {
                        "YD5010_450_Solid Coated",
                        "YD5010_450_Solid Uncoated",
                        }
                        );
                }
            }

            _excelFilePathList = ExcelFileNameList.Select(x => Path.Combine(System.Environment.CurrentDirectory, "Resources", x) + ".xlsx").ToList();
            ExcelFilePath = _excelFilePathList.First();
            IsProgressRingActiveLilin = false;


            IlluminantList = new List<StandardIlluminant>();
            for (int i = 0; i < (int)StandardIlluminant.Unknown; i++)
            {
                IlluminantList.Add((StandardIlluminant)i);
            }
            ObserverList = new ObservableList<string>()
            {
                "2",
                "10",
            };
            if (IfNot45_0)
            {
                SpecularComponentList.Clear();
                SpecularComponentList.Add("SCI");
                SpecularComponentList.Add("SCE");
            }
            else
            {
                SpecularComponentList.Clear();
                SpecularComponentList.Add("45/0");
            }

            _filterKeyList = new List<string>(new string[]
            {
                "CIEDE1976",
                "CIEDE1994",
                "CIEDE2000",
                "CMCDE",

                "ColorDiff_CIELAB_L",
                "ColorDiffFormula_DL1994",
                "ColorDiffFormula_DL2000",
                "ColorDiffFormula_DLcmc",

                "ColorDiff_CIELAB_a",

                "ColorDiff_CIELAB_b",
            }
            );
            FilterList = _filterKeyList.AsEnumerable().Select(x => S._(x)).ToList();
            CurrentFilter = FilterList.First();

        }


        private IList<PanTongColor> GetPanTongColors(int type = 0)
        {
            ExcelFilePath = _excelFilePathList.ElementAt(type);
            if (PanTongColorResult != null)
            {
                PanTongColorResult.Clear();
            }
            PanTongColorFileConfig config = new PanTongColorFileConfig
            {
                IfNot45_0 = IfNot45_0,
                FilePath = ExcelFilePath,
                SheetName = null,
                StartRow = 2,
                EndRow = -1,
                SampleNameColumn = "B",
                SampleRemarksColumn = "C",
                LabStartColumn = IfNot45_0 ? "D" : "F",
                YxyStartColumn = null,
                SpectraStartColumn = IfNot45_0 ? "G" : "I",
                WavelengthRange = new WavelengthRange(400, 700, 10),
                ScModeSetting = ScModeSetting.SciFollowWithSce,
                TestDateTimeColumn = null,
                IllumObserRemarks = null,
            };
            PanTongColorFileRead panTongColorFileRead = new PanTongColorFileRead(config);
            return panTongColorFileRead.ReadPanTongColorFromSheet();
        }

        #region Addtional Function Begin
        public ICommand SetMatchToCurrentStandard { get; set; }
        public ICommand SetCurSamToCurrentStandard { get; set; }
        private void InitCommands()
        {
            SetMatchToCurrentStandard = new RelayCommand(ExeSetMatchToCurrentStandard, null);
            SetCurSamToCurrentStandard = new RelayCommand(ExeSetCurSamToCurrentStandard, null);
        }
        private List<MenuItem> _contextMenuItems;
        public List<MenuItem> ContextMenuItemsLilin
        {
            get => _contextMenuItems;
            private set
            {
                _contextMenuItems = value;
            }
        }
        private void InitContextMenu()
        {
            ContextMenuItemsLilin = new List<MenuItem>(new MenuItem[]
            {
                new MenuItem(),
                new MenuItem(),
            });
            ContextMenuItemsLilin.ElementAt(0).Header = S._("SetMatchToBeStdLilin");
            ContextMenuItemsLilin.ElementAt(0).Command = SetMatchToCurrentStandard;
            ContextMenuItemsLilin.ElementAt(0).InputGestureText = "Ctrl+M";

            ContextMenuItemsLilin.ElementAt(1).Header = S._("SetCurSamToBeStdLilin");
            ContextMenuItemsLilin.ElementAt(1).Command = SetCurSamToCurrentStandard;
        }
        private bool TwoRowEquals(DataRow row1, DataRow row2)
        {
            if (row1.ItemArray.Length != row2.ItemArray.Length)
            {
                return false;
            }
            for (int i = 0; i < row1.ItemArray.Length; i++)
            {
                if (!row1.ItemArray.ElementAt(i).Equals(row2.ItemArray.ElementAt(i)))
                {
                    return false;
                }
            }
            return true;
        }
        public int SelectedItemInDataTablePos(DataRowView view, DataTable dataTable)
        {
            int num = -1, i = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                if (TwoRowEquals(row, view.Row))
                {
                    num = i;
                    break;
                }
                i++;
            }
            return num;
        }
        private void ExeSetMatchToCurrentStandard(object obj)
        {
            DataGrid dataGrid = obj as DataGrid;
            var selectedItems = dataGrid?.SelectedItems.OfType<DataRowView>().ToArray();
            if (selectedItems == null || !selectedItems.Any())
            {
                return;
            }
            int num = SelectedItemInDataTablePos(selectedItems[0], DataTableLilin);
            if (num != -1)
            {
                bool bl = MessageBoxShow.DialogSetToBeStandardEnsure(_nameList?.ElementAt(num));
                if (!bl)
                {
                    return;
                }
                Specimen sample = _createTableFromConfig.SampleList.ElementAt(num);
                SetToBeGlobalStandardSpecimen(sample);
                _toastInfo.ShowToastInfo(S._("PanTongStdSuccessLilin") + ":" + _nameList?.ElementAt(num));
            }
            else
            {
                _toastInfo.ShowToastInfo(S._("PanTongStdFailLilin"));
            }
        }
        private void ExeSetCurSamToCurrentStandard(object obj)
        {
            if (_sample == null) return;
            bool bl = MessageBoxShow.DialogSetToBeStandardEnsure(_sample.Name);
            if (bl)
            {
                SetToBeGlobalStandardSpecimen(_sample);
                _toastInfo.ShowToastInfo(S._("PanTongStdSuccessLilin") + ":" + _sample.Name);
            }
        }
        public void SetToBeGlobalStandardSpecimen(Specimen specimen)
        {
            StandardSpecimen standard = new StandardSpecimen(specimen);
            _currentSetting.MainWindowObj.AddStandard(standard);
            _currentSetting.MainWindowObj.CurrentStandard = standard;
        }

        #endregion Addtional Function End

        private static readonly object Lock = new object();

        private void OnKeyWordSearchClicked(object sender, RoutedEventArgs args)
        {
            KeyWordSearch keyWordSearch = new KeyWordSearch(PanTongColorResult, this);
            keyWordSearch.ShowDialog();
        }
        private void MeasureButton_OnClick(object sender, RoutedEventArgs args)
        {
            if (_instrument == null)
            {
                MessageBox.Show(S._("S_FailToConnectTheInstrument"));
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                IsProgressRingActiveLilin = true;//(ThreeNH.GratingSpectrometer.Spectrometer)
                var type = _instrument.GetType();
                Specimen result = null;
                switch (_currentSetting.MainWindowObj.InstrumentType)
                {
                    case InstrumentType.CR:
                        if (_instrument is SQCX.Infrastructure.Instruments.ColorReader.BluetoothDongleSpectrometer crDongleSpectrometer)
                        {
                            result = crDongleSpectrometer.Measure();
                        }
                        else
                        {
                            result = ((SQCX.Infrastructure.Instruments.ColorReader.Spectrometer)_instrument).Measure();
                        }
                        break;
                    case InstrumentType.YS:

                        if (IfNot45_0)
                        {
                            result = ((Spectrometer)_instrument).Measure(SpecularComponent.SCIAndSCE,
                                ((ISystemSetting)_instrument).UvMode);
                        }
                        else
                        {
                            result = ((Spectrometer)_instrument).MeasureSample();
                        }
                        break;
                    case InstrumentType.BTS:
                        if (_instrument is ThreeNH.SQCX.Infrastructure.Instruments.BTS.BluetoothDongleSpectrometer btsDongleSpectrometer)
                        {
                            result = btsDongleSpectrometer.Measure();
                        }
                        else
                        {
                            if (IfNot45_0)
                            {
                                result = ((ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer)_instrument).Measure(ThreeNH.SQCX.Infrastructure.Instruments.BTS.MeasurementType.Standard, SpecularComponent.SCIAndSCE);
                            }
                            else
                            {
                                result = ((ThreeNH.SQCX.Infrastructure.Instruments.BTS.Spectrometer)_instrument).Measure();
                            }
                            if (result.IsTransmission)
                            {
                                _toastInfo.ShowToastInfo(S._("Label_Transmittance") + ": " + S._("TheOperationIsNotSupported"));
                                return;
                            }
                        }

                        break;
                    case InstrumentType.PTS:
                        if (_instrument is PTS.BluetoothDongleSpectrometer ptsDongleSpectrometer)
                        {
                            result = ptsDongleSpectrometer.Measure();
                        }
                        else
                        {
                            if (IfNot45_0)
                            {
                                result = ((PTS.Spectrometer)_instrument).Measure(PTS.MeasurementType.Standard, SpecularComponent.SCIAndSCE);
                            }
                            else
                            {
                                result = ((PTS.Spectrometer)_instrument).Measure();
                            }
                        }
                        if (result.IsTransmission)
                        {
                            _toastInfo.ShowToastInfo(S._("Label_Transmittance") + ": " + S._("TheOperationIsNotSupported"));
                            return;
                        }
                        break;
                    case InstrumentType.JSON:
                        if (_instrument is SQCX.Infrastructure.Instruments.Spectrophotometer.TS8450 jsonSpectrometer)
                        {
                            result = jsonSpectrometer.Measure();
                        }
                        if (result.IsTransmission)
                        {
                            _toastInfo.ShowToastInfo(S._("Label_Transmittance") + ": " + S._("TheOperationIsNotSupported"));
                            return;
                        }
                        break;
                    case InstrumentType.YL30:
                        if (_instrument is SQCX.Infrastructure.Instruments.ThreeNH.YL30.SpectrometerForYL30 YLSpectrometer)
                        {
                            result = YLSpectrometer.Measure();
                        }
                        if (result.IsTransmission)
                        {
                            _toastInfo.ShowToastInfo(S._("Label_Transmittance") + ": " + S._("TheOperationIsNotSupported"));
                            return;
                        }
                        break;
                    default:
                        return;
                }
                //((.)_instrument).
                //Measure(SpecularComponent.SCIAndSCE,UVFlag.None);//20191114 lilin 一定要异步的形式才能调用
                if (result == null)
                {
                    _toastInfo.ShowToastInfo(S._("MenuItem_InstrumentType") + ": " + S._("TheOperationIsNotSupported"));
                    return;
                }

                result.Name = result.DateTime.ToString("yyyy-MM-dd HHmmss") +
                    string.Format("__{0}_{1}_{2}__", CurrentIllum.ToString(),
                    CurrentObser,
                    IfNot45_0 ? "SCI&SCE" : "45_0") +
                    string.Format("({0}{1})", "No.", SampleList.Count.ToString());

                if (SampleGetSpectrumData(result) == null) return;

                SampleList.Add(result);
                CurrentSampleIndex = SampleList.Count - 1;

            }
            catch (Exception e)
            {
                _toastInfo.ShowToastInfo(S._(e.Message));
            }
            finally
            {
                IsProgressRingActiveLilin = false;
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }


        private CIELAB SampleObtainLab(Specimen sample)
        {
            if (sample != null)
            {
                Spectrum spe = SampleGetSpectrumData(sample);
                return Formula.CalculateLab(spe, CurrentIllum, CurrentObserEnumType);
            }
            else if (Lab_L != null && Lab_a != null && Lab_b != null)
            {
                return new CIELAB(Lab_L.GetValueOrDefault(), Lab_a.GetValueOrDefault(), Lab_b.GetValueOrDefault());
            }
            return null;
        }
        private Spectrum SampleGetSpectrumData(Specimen specimen)
        {
            if (CurrentSpecCom == "SCE" && specimen.SCE != null)
            {
                return specimen.SCE.Spectrum;
            }
            else if (CurrentSpecCom == "SCI" && specimen.SCI != null)
            {
                return specimen.SCI.Spectrum;
            }
            else if (CurrentSpecCom == "45/0" && specimen.Clr != null)
            {
                return specimen.Clr.Spectrum;
            }
            else
            {
                _toastInfo.ShowToastError(S._("MenuItem_SpecularComponent") + ":"
    + S._("Tip_SCISCEDisplaySwitch"));
                return null;
            }
        }
        private void RefreshSpectrumPlotSelectSample()
        {
            SpectrumPlotControl.BeginInit();
            SpectrumPlotControl.RemoveAllStandards();
            SpectrumPlotControl.RemoveAllTests();
            if (_sample == null)
            {
                SpectrumPlotControl.EndInit();
                return;
            }
            var spe = SampleGetSpectrumData(_sample);
            if (spe != null)
            {
                SpectrumPlotControl.AddStandard(spe, "Sample");
            }
            SpectrumPlotControl.EndInit();
        }
        private void RefreshSpectrumPlotSelectMatchedItem(Specimen specimen, int number, string name)
        {
            foreach (var tmp in _alreadySelectedIndexList)
            {
                if (tmp == number) return;
            }
            var spe = SampleGetSpectrumData(specimen);
            if (spe != null)
            {
                SpectrumPlotControl.AddTest(spe, "No." + number.ToString() + ":" + name);
            }
        }
        private void OnSelectSampleItem(object sender, RoutedEventArgs args)
        {
            IEnumerable<Specimen> sampleArr = from sam in SampleList
                                              where sam.Equals(CurrentSampleName)
                                              select sam;
            if (sampleArr.Count() > 0)
            {
                _sample = (Specimen)sampleArr?.First();

                CIELAB _lab = SampleObtainLab(_sample);
                Lab_L = _lab.L;
                Lab_a = _lab.a;
                Lab_b = _lab.b;
                SpectrumPlotControl.Clear();
                RefreshSpectrumPlotSelectSample();
                RefreshToleranceChart();
            }

        }

        private void OnSpecComChanged(object sender, RoutedEventArgs args)
        {
            if (_sample == null) return;
            var lab = SampleObtainLab(_sample);
            Lab_L = lab.L;
            Lab_a = lab.a;
            Lab_b = lab.b;
        }
        private void OnIlluminantChanged(object sender, RoutedEventArgs args)
        {
            if (_sample == null) return;
            var lab = SampleObtainLab(_sample);
            Lab_L = lab.L;
            Lab_a = lab.a;
            Lab_b = lab.b;
        }
        private void OnObserverChanged(object sender, RoutedEventArgs args)
        {
            if (_sample == null) return;
            var lab = SampleObtainLab(_sample);
            Lab_L = lab.L;
            Lab_a = lab.a;
            Lab_b = lab.b;
        }
        public CIELAB PanTongColorDataObtainLab(PanTongColor panTongColor)
        {
            Spectrum _spectralData = ObtainSpectra(panTongColor);
            if (_spectralData == null)
                return null;
            return Formula.CalculateLab(_spectralData, CurrentIllum, CurrentObserEnumType);
        }
        private Spectrum ObtainSpectra(PanTongColor panTongColor)
        {
            if (panTongColor == null || panTongColor.SpecimenData == null)
                return null;

            ColorData colorData;
            switch (CurrentSpecCom)
            {
                case "SCI":
                    colorData = panTongColor.SpecimenData.SCI;
                    break;
                case "SCE":
                    colorData = panTongColor.SpecimenData.SCE;
                    break;
                case "45/0":
                    colorData = panTongColor.SpecimenData.Clr;
                    break;
                default:
                    return null;
            }

            if (colorData == null || colorData.Spectrum == null)
                return null;

            int begin, interval, max;
            begin = colorData.Spectrum.Range.Min;
            interval = colorData.Spectrum.Range.Step;
            max = colorData.Spectrum.Range.Max;

            Spectrum _spectralData = new Spectrum(new WavelengthRange(begin, max, interval), colorData.Spectrum.Data);
            return _spectralData;
        }
        public Specimen GenerateSampleFromSpectra(PanTongColor panTongColor)
        {
            Spectrum spectrum = ObtainSpectra(panTongColor);
            if (spectrum == null)
                return null;

            Specimen sample = new Specimen();
            sample.Name = panTongColor.SampleNames;
            if (CurrentSpecCom == "SCI")
            {
                sample.SCI = new ColorData();
                sample.SCI.Spectrum = spectrum;
            }
            else if (CurrentSpecCom == "SCE")
            {
                sample.SCE = new ColorData();
                sample.SCE.Spectrum = spectrum;
            }
            else
            {
                sample.Clr = new ColorData();
                sample.Clr.Spectrum = spectrum;
            }
            return sample;
        }
        private double CalDeltaLab(CIELAB lab1, CIELAB lab2, Specimen sample)
        {
            ThreeNH.DataModel.Tolerance tolerance = _currentSetting.MainWindowObj.Database.GetTolerance(
                _currentSetting.MainWindowObj.SingleStandardInShowing,
                new ObserverIlluminantPair(CurrentObserEnumType, CurrentIllum));
            var colorSetting = _currentSetting.ToleranceSet;
            double Kl = 1, Kc = 1, Kh = 1;
            if (CurrentFilter == FilterList.First() || CurrentFilter == FilterList.ElementAt(4) ||
                CurrentFilter == FilterList.ElementAt(8) || CurrentFilter == FilterList.ElementAt(9))
            {
                var tmp = new CIEDE1976(lab1, lab2);
                if (CurrentFilter == FilterList.First()) return tmp.dE;
                else if (CurrentFilter == FilterList.ElementAt(4)) return tmp.dL;
                else if (CurrentFilter == FilterList.ElementAt(8)) return tmp.da;
                else return tmp.db;
            }
            else if (CurrentFilter == FilterList.ElementAt(1) || CurrentFilter == FilterList.ElementAt(5))
            {
                Kl = tolerance.KL94;
                Kc = tolerance.KC94;
                Kh = tolerance.KH94;
                var tmp = new CIEDE1994(lab1, lab2,
                    Kl, Kc, Kh);
                if (CurrentFilter == FilterList.ElementAt(1)) return tmp.dE;
                else return tmp.dL;
            }
            else if (CurrentFilter == FilterList.ElementAt(2) || CurrentFilter == FilterList.ElementAt(6))
            {
                Kl = tolerance.KL00;
                Kc = tolerance.KC00;
                Kh = tolerance.KH00;
                var tmp = new CIEDE2000(lab1, lab2,
                    Kl, Kc, Kh);
                if (CurrentFilter == FilterList.ElementAt(2)) return tmp.dE;
                else return tmp.dL;
            }
            else //if (CurrentFilter == _filterList.ElementAt(3) || CurrentFilter == _filterList.ElementAt(7))
            {
                Kl = tolerance.KLcmc;
                Kc = tolerance.KCcmc;
                var tmp = new CMCDE(lab1, lab2,
                    Kl, Kc);
                if (CurrentFilter == FilterList.ElementAt(3)) return tmp.dE;
                else return tmp.dL;
            }

        }
        public Brush GetPseudoColorFromLab(double l, double a, double b)
        {
            var srgb = Formula.LabToSrgb(new CIELAB(l, a, b));
            return new SolidColorBrush(Color.FromRgb(
                srgb.R, srgb.G, srgb.B));
        }
        public Brush GetPseudoColorFromLab(CIELAB lab)
        {
            var srgb = Formula.LabToSrgb(lab);
            return new SolidColorBrush(Color.FromRgb(
                srgb.R, srgb.G, srgb.B));
        }
        private double fabsfunc(double f)
        {
            return f < 0 ? -f : f;
        }
        private void OnSearchClick(object sender, RoutedEventArgs args)
        {
            if (PanTongColorResult == null)
            {
                _toastInfo.ShowToastInfo(S._("NoPanTongColorDataLilin"));
                return;
            }
            if (PanTongColorResult.Count < 1)
            {
                _toastInfo.ShowToastInfo(S._("NoPanTongColorDataLilin"));
                return;
            }
            if (Lab_L == null || Lab_a == null || Lab_b == null)
            {
                _toastInfo.ShowToastInfo(S._("NoSampleColorDataLilin"));
                return;
            }
            var stdLab = new CIELAB(Lab_L.GetValueOrDefault(), Lab_a.GetValueOrDefault(), Lab_b.GetValueOrDefault());
            StandardSpecimen std = null;
            if (_sample != null)
                std = new StandardSpecimen(_sample);
            else
            {
                var specimen = new Specimen();
                var colorData = new ColorData(CurrentIllum, CurrentObserEnumType, stdLab);
                if (CurrentSpecCom == "SCI")
                    specimen.SCI = colorData;
                else if (CurrentSpecCom == "SCE")
                    specimen.SCE = colorData;
                else specimen.Clr = colorData;
                std = new StandardSpecimen(specimen);
            }

            SpectrumPlotControl.RemoveAllTests();
            _alreadySelectedIndexList.Clear();
            try
            {
                RefreshToleranceChart();
            }
            catch (Exception ex)
            {
                _toastInfo.ShowToastInfo(ex.Message);
                return;
            }

            DateTime timestamp = DateTime.Now;
            IEnumerable<MatchedColorItem> query;
            try
            {
                query =
                    from pantong_ in PanTongColorResult.AsEnumerable()
                    let lab = PanTongColorDataObtainLab(pantong_)
                    let sample = GenerateSampleFromSpectra(pantong_)
                    where lab != null && sample != null
                    let delta = CalDeltaLab(new CIELAB((double)Lab_L, (double)Lab_a, (double)Lab_b), lab, sample)
                    where !double.IsNaN(delta) && !double.IsInfinity(delta)
                    orderby fabsfunc(delta)
                    select new MatchedColorItem
                    {
                        Name = pantong_.SampleNames + (CurrentSpecCom != "SCE" ? "" : ".SCE"),
                        Lab = new CIELAB(lab.L, lab.a, lab.b),
                        Sample = sample,
                        Delta = fabsfunc(delta),
                    };

                var list = query.ToList();
                _nameList = new List<string>();
                _matchColorList = new List<MatchedColorItem>();
                _createTableFromConfig.SampleList.Clear();
                _createTableFromConfig.DataTable.Rows.Clear();


                for (int count = 0; count < 5; count++)
                {
                    if (count + 1 < list.Count())
                    {
                        _createTableFromConfig.SampleList.Add(list.ElementAt(count).Sample);//
                        var row = _createTableFromConfig.CreateDataRow(_createTableFromConfig.DataTable, list.ElementAt(count).Sample, std);
                        _createTableFromConfig.DataTable.Rows.Add(row);
                        _nameList.Add(list.ElementAt(count).Name);
                    }
                }

                _matchColorList = list.Take(5).ToList();
                var span = DateTime.Now - timestamp;
                _toastInfo.ShowToastInfo(string.Format(S._("SearchTime$0$ShowLilin"), span.TotalMilliseconds));
                if (_createTableFromConfig.SampleList.Count == 0)
                {
                    _toastInfo.ShowToastInfo(S._("CannotFindMatchedColorLilin"));
                }
                else
                {
                    TargetColor = GetPseudoColorFromLab(new CIELAB((double)Lab_L, (double)Lab_a, (double)Lab_b));
                    MatchedBestSampleColor = GetPseudoColorFromLab(list.First().Lab);
                    DataTableLilin.Rows.Clear();
                    TableAdapter.RowAdaptFromKnownTable(_nameList, ref _dataTable, _createTableFromConfig.DataTable);
                }
            }
            catch (Exception e)
            {
                _toastInfo.ShowToastInfo(e.Message);
            }
        }
        private List<int> _alreadySelectedIndexList = new List<int>();
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if ((args.AddedItems == null || args.AddedItems.Count == 0)
                && (args.RemovedItems == null || args.RemovedItems.Count == 0))
            {
                return;
            }

            if (args.AddedItems != null && args.AddedItems.Count > 0)
            {
                int num = SelectedItemInDataTablePos((DataRowView)args.AddedItems[0], DataTableLilin);
                if (num != -1)
                {
                    CurrentFormula = CurrentFilter + ": " + _matchColorList.ElementAt(num).Delta.ToString();
                    NotifyOfPropertyChanged(nameof(CurrentFormula));
                    MatchedBestSampleColor = GetPseudoColorFromLab(_matchColorList.ElementAt(num).Lab);
                    _toastInfo.ShowToastInfo(S._("CurrentMatchColorChangeLilin") + ":" + _matchColorList.ElementAt(num).Name);
                }
            }

            // 修复多选后单选仍保留旧项：原逻辑只追加 AddedItems，改为与 DataGrid 当前选中项同步
            _alreadySelectedIndexList.Clear();
            foreach (var item in ResultsDataGrid.SelectedItems)
            {
                int c = SelectedItemInDataTablePos((DataRowView)item, DataTableLilin);
                if (c != -1)
                {
                    _alreadySelectedIndexList.Add(c);
                }
            }

            SpectrumPlotControl.RemoveAllTests();
            foreach (int c in _alreadySelectedIndexList)
            {
                var specimen = _matchColorList.ElementAt(c).Sample;
                var spe = SampleGetSpectrumData(specimen);
                if (spe != null)
                {
                    SpectrumPlotControl.AddTest(spe, "No." + c.ToString() + ":" + _matchColorList.ElementAt(c).Name);
                }
            }
            RefreshToleranceChart();
        }
        #region 图表区 begin
        void RefreshToleranceChart()
        {
            if (_sample == null)
            {
                ToleranceChartHelper.Show(ToleranceChartView, CurrentIllum, CurrentObserEnumType,
                    ToleranceChartHelper.ParseSpecularComponent(CurrentSpecCom), null);
                return;
            }

            Tolerance tolerance = null;
            try
            {
                tolerance = _currentSetting.MainWindowObj.Database.GetTolerance(
                    _currentSetting.MainWindowObj.SingleStandardInShowing,
                    new ObserverIlluminantPair(CurrentObserEnumType, CurrentIllum));
            }
            catch
            {
                // 无标样容差时使用默认容差框
            }

            var samples = _alreadySelectedIndexList
                .Where(i => i >= 0 && _matchColorList != null && i < _matchColorList.Count)
                .Select(i => _matchColorList[i].Sample)
                .ToList();

            ToleranceChartHelper.Show(
                ToleranceChartView,
                CurrentIllum,
                CurrentObserEnumType,
                ToleranceChartHelper.ParseSpecularComponent(CurrentSpecCom),
                _sample,
                samples,
                tolerance);
        }
        #endregion 图表区 end
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyOfPropertyChanged(string proStr = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(proStr));
        }

        private void ResultsDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (DataTableLilin == null || DataTableLilin.Rows.Count == 0) return;
            if (e.Column.SortMemberPath != "dL" && e.Column.SortMemberPath != "da" && e.Column.SortMemberPath != "db" && e.Column.SortMemberPath != "dE") return;
            DataTable dt1 = DataTableLilin.Copy();
            foreach (DataRow row in dt1.Rows)
            {
                double value = Convert.ToDouble(row[e.Column.SortMemberPath]);
                row[e.Column.SortMemberPath] = Math.Abs(value);
            }
            dt1.DefaultView.Sort = $"{e.Column.SortMemberPath} asc";
            dt1 = dt1.DefaultView.ToTable();
            List<string> nameList = new List<string>();
            foreach (DataRow row in dt1.Rows)
            {
                nameList.Add(row["色卡名称"].ToString());
            }
            DataTable dt2 = DataTableLilin.Clone();

            int index = 0;
            while (index < nameList.Count)
            {
                foreach (DataRow row in DataTableLilin.Rows)
                {
                    if (row["色卡名称"].ToString() == nameList[index])
                    {
                        dt2.Rows.Add(row.ItemArray);
                        index++;
                        if (index == nameList.Count)
                        {
                            break;
                        }
                    }
                }
            }
            DataTableLilin = dt2.DefaultView.ToTable();
            e.Handled = true;
        }
    }
}
