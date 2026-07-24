using LiveCharts.Defaults;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Presentation;
using ThreeNH.ColorQualityControl.Converters;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorQualityControl.UI.ColourAtla;
using ThreeNH.ColorQualityControl.Specimens.Import;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        #region 阶段0-Job 数据与选择状态 (→ JobDocumentService / SpecimenSelectionState)

        void InitializeData()
        {

        }

        // 观察者/光源公式由 Job 设置驱动；自 Facade 迁出以免 MainWindow.cs 膨胀（阶段4-T02）
        private Formula formula = new Formula();

        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        private SpecularComponent specularCmponent;

        public SpecularComponent SpecularComponent
        {
            get { return specularCmponent; }
            set
            {
                if (value != specularCmponent)
                {
                    specularCmponent = value;
                    if (SpecularComponentComboBox != null)
                    {
                        SpecularComponentComboBox.SelectedItem = value;
                    }
                }
            }
        }

        public ObserverIlluminantPair ObserverIlluminant
        {
            get
            {
                return new ObserverIlluminantPair(Formula.Observer, Formula.Illuminant);
            }
            set
            {
                if (value.Observer != Formula.Observer || value.Illuminant != Formula.Illuminant)
                {
                    Formula = Database.GetFormula(value.Observer, value.Illuminant);
                    IlluminantComboBox.SelectedItem = value;
                }
            }
        }

        public StandardIlluminant Illuminant => Formula.Illuminant;

        public StandardObserver Observer => Formula.Observer;

        public ChromaticitySetting ChromaticitySetting
            => Database.GetChromaticitySetting(SingleStandardInShowing);

        public ToleranceSetting ToleranceSetting
            => Database.GetToleranceSetting(SingleStandardInShowing);

        private void InitDisplay()
        {
            _session.JobDocument.ApplySystemDefaults(SystemDefaultConfig);
            InitDisplayUi();
        }

        /// <summary>Job 默认设置就绪后，初始化界面控件（阶段1-T22）。</summary>
        private void InitDisplayUi()
        {
            IsAutoNaming = SystemDefaultConfig.IsAutoNaming;
            SpectrumPlot.Visibility = SystemDefaultConfig.ShowSpectrumPlot ? Visibility.Visible : Visibility.Collapsed;
            AbsorbancePlot.Visibility = SystemDefaultConfig.ShowSpectrumPlot ? Visibility.Collapsed : Visibility.Visible;

            // 初始化测光方式下拉列表
            if (Colorimeter is ISystemSetting)
            {
                if (((ISystemSetting)Colorimeter).SpecularComponent == SpecularComponent.SCIAndSCE)
                {
                    SpecularComponentComboBox.SelectedItem = SpecularComponent.SCI;
                }
                else
                {
                    SpecularComponentComboBox.SelectedItem = ((ISystemSetting)Colorimeter).SpecularComponent;
                }
            }

            // 初始化光源切换列表
            ResetIlluminantComboBox(Database.ObserverAndIlluminantSetting);
            // 设置默认显示的累计图；带上系统容差以便默认 ΔE 系列画出上限虚线
            StatisticsPlot.RangeTolerance = Database.GetTolerance(
                null, new ObserverIlluminantPair(Observer, Illuminant));
            StatisticsPlot.Show(ChromaticityField.dEab.Name(), test => test.dEab);
            ResetDisplayedFields();

            StatusText = _("Ready");

            OnCurrentStandardChanged();
        }

        private void ResetIlluminantComboBox(ObserverAndIlluminantSetting setting)
        {
            IlluminantComboBox.Items.Clear();
            IlluminantComboBox.Items.Add(setting.Primary);
            if (setting.SecondaryIlluminant != StandardIlluminant.Unknown)
                IlluminantComboBox.Items.Add(setting.Secondary);
            if (setting.TertiaryIlluminant != StandardIlluminant.Unknown)
                IlluminantComboBox.Items.Add(setting.Tertiary);
            IlluminantComboBox.SelectedItem = setting.Primary;
        }

        internal ColorData WSCI { get; set; }

        internal ColorData WSCE { get; set; }

        internal SpecimenDatabase Database => _session.JobDocument.Database;

        public bool ShowSpectrumPlot
        {
            get => SystemDefaultConfig.ShowSpectrumPlot;
            set
            {
                SystemDefaultConfig.ShowSpectrumPlot = value;
                NotifyPropertyChanged(nameof(ShowSpectrumPlot));
                SpectrumPlot.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                AbsorbancePlot.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public bool ShowChromaticity
        {
            get => SystemDefaultConfig.ShowChromaticity;
            set
            {
                SystemDefaultConfig.ShowChromaticity = value;
                NotifyPropertyChanged(nameof(ShowChromaticity));
            }
        }

        #endregion

        private TreeViewItem FindTreeViewItem(ItemCollection items, Specimen specimen)
            => SpecimenTreePresenter.FindTreeViewItem(items, specimen);

        private static int IndexOfTreeViewItem(ItemCollection items, Specimen specimen)
            => SpecimenTreePresenter.IndexOfTreeViewItem(items, specimen);

        private SpecimenWrap FindStandard(StandardSpecimen standard) => _treePresenter.FindStandard(standard);

        private SpecimenWrap FindTest(TestSpecimen test) => _treePresenter.FindTest(test);

        public bool ShowingColorDiffReport => CurrentStandard != null;

        private SpecimenWrap[] GetAllStandardWraps()
        {
            List<SpecimenWrap> list = new List<SpecimenWrap>();
            foreach (TreeViewItem item in StandardGroupTreeItem.Items)
            {
                list.Add(item.DataContext as SpecimenWrap);
            }
            return list.ToArray();
        }

        private SpecimenWrap[] GetTestWraps(StandardSpecimen standard)
        {
            List<SpecimenWrap> list = new List<SpecimenWrap>();
            ItemCollection items = null;
            if (standard != null)
            {
                items = FindTreeViewItem(StandardGroupTreeItem.Items, standard).Items;
            }
            else
            {
                items = OrphanTestsTreeItem.Items;
            }
            foreach (TreeViewItem item in items)
            {
                list.Add(item.DataContext as SpecimenWrap);
            }

            return list.ToArray();
        }

        private SpecimenWrap[] GetAllTestWraps()
        {
            List<SpecimenWrap> list = new List<SpecimenWrap>();
            foreach (TreeViewItem item in AllTestsTreeItem.Items)
            {
                list.Add(item.DataContext as SpecimenWrap);
            }
            return list.ToArray();
        }


        void ObserverAndIlluminantExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ObserverAndIlluminantDialog dlg = new ObserverAndIlluminantDialog();
            dlg.Owner = this;
            dlg.Setting = Database.ObserverAndIlluminantSetting;

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                Database.ObserverAndIlluminantSetting = dlg.Setting;
                SystemDefaultConfig.ObserverAndIlluminantSetting = dlg.Setting;
                OnObserverAndIlluminantSettingChanged();
            }
        }

        /// <summary>
        /// 创建ContextData为SpecimenAdapter的TreeViewItem的Header（当前标样树仍用）
        /// </summary>
        private static object CreateSpecimenItemHeader(bool isStandard)
            => SpecimenTreePresenter.CreateSpecimenItemHeader(isStandard);

        private void OnStandardDataGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            //MessageBox.Show(row.ToString() + "\r\n" + row.Item.ToString());
            if (row != null && row.Item is SpecimenWrap)
            {
                StandardSpecimen std = (row.Item as SpecimenWrap).Specimen as StandardSpecimen;
                CurrentStandard = std;
                e.Handled = true;
            }
        }

        private void OnOrphanTestsItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CurrentStandard != null)
            {
                CurrentStandard = null;
                e.Handled = true;
            }
        }

        #region 阶段1-Job 加载与清空 (JobDocumentService + MainWindow UI)

        /// <summary>仅清空 TreeView / 表格 / 图表等呈现层。</summary>
        private void ClearJobUi()
        {
            displayedCategory = null;
            _session.Selection.ResetView();
            CurrentStandard = null;
            StandardDataGrid.Items.Clear();
            TestDataGrid.Items.Clear();
            ToleranceDataGrid.Items.Clear();
            SpectrumPlot.Clear();
            AbsorbancePlot.Clear();
            ClearNavigationTree();

            StandardGroupTreeItem.IsSelected = false;
            AllTestsTreeItem.IsSelected = false;
            OrphanTestsTreeItem.IsSelected = false;

            StatisticsPlot.Clear();
        }

        /// <summary>新建/重置 Job：Service 清数据 + MainWindow 清 UI（阶段1-T22）。</summary>
        private void ResetJobDocument()
        {
            ClearJobUi();
            _session.JobDocument.ResetNewJob(SystemDefaultConfig);
            InitDisplayUi();
        }

        /// <summary>关闭前丢弃当前 Job 内容（不恢复系统默认）。</summary>
        private void DiscardJobContent()
        {
            ClearJobUi();
            _session.JobDocument.ClearDatabase();
        }

        void ReloadAll()
        {
            CurrentStandard = null;
            StandardDataGrid.Items.Clear();
            TestDataGrid.Items.Clear();
            ToleranceDataGrid.Items.Clear();
            SpectrumPlot.Clear();
            AbsorbancePlot.Clear();
            ClearNavigationTree();

            SpecularComponentComboBox.SelectedIndex = 0;
            ResetIlluminantComboBox(Database.ObserverAndIlluminantSetting);

            LoadAllSpecimens();
        }

        void LoadAllSpecimens()
        {
            var viewData = _session.JobDocument.GetAllStandardsForView(SpecularComponent, Observer, Illuminant);
            _treePresenter.LoadAll(viewData);
        }

        int IndexOf(DataGrid specimenDataGrid, Specimen specimen)
        {
            for (int i = 0; i < specimenDataGrid.Items.Count; i++)
            {
                var item = specimenDataGrid.Items[i] as SpecimenWrap;
                if (item.Specimen == specimen)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 从Job中移除一个标样
        /// </summary>
        /// <param name="standard">要移除的标样</param>
        public void RemoveStandard(StandardSpecimen standard)
        {
            if (CurrentStandard == standard)
            {
                CurrentStandard = null;
            }

            // UI 移除由 SpecimenRemoved 事件驱动；此处只删库（阶段2-T07）
            _session.JobDocument.RemoveStandardFromDatabase(standard);
        }

        /// <summary>
        /// 删除所有标样
        /// </summary>
        private void ClearStandards()
        {
            StandardDataGrid.Items.Clear();
            SpectrumPlot.Clear();
            AbsorbancePlot.Clear();
            _treePresenter.Clear();
            _session.JobDocument.ClearDatabase();
        }

        /// <summary>
        /// 所有标样、试样光谱的类型
        /// </summary>
        private string SpectrumType
        {
            get
            {
                int countT = 0;
                int countR = 0;
                if (IsShowAllStandards)
                {
                    var stds = Database.StandardSpecimens;
                    countT = stds.Count(std => std.IsTransmission);
                    countR = stds.Length - countT;
                }
                else if (IsShowSingleStandard)
                {
                    var tests = TestsToShow;
                    countT = tests.Count(t => t.Specimen.IsTransmission);
                    countR = tests.Count() - countT;
                    if (SingleStandardInShowing?.IsTransmission == true)
                    {
                        countT += 1;
                    }
                    else
                    {
                        countR += 1;
                    }
                }
                else
                {
                    var tests = TestsToShow;
                    countT = tests.Count(t => t.Specimen.IsTransmission);
                    countR = tests.Count() - countT;
                }

                if (countR > 0 && countT > 0)
                {
                    return "R%/T%";
                }
                else if (countR > 0)
                {
                    return "R%";
                }
                else if (countT > 0)
                {
                    return "T%";
                }
                else
                {
                    return "R%";
                }
            }
        }

        /// <summary>
        /// 往Job中添加一个新标样，如果没有名字，为其生成一个名字。
        /// 阶段0关键路径：Instrument.ProcessSingleStandard、Import、Edit 均会调用。
        /// </summary>
        /// <param name="standard">要添加的标样</param>
        public void AddStandard(StandardSpecimen standard, bool createNewId = true, bool isRemoveSame = true)
        {
            if (isRemoveSame)
            {
                var existing = _session.JobDocument.FindStandardByName(standard.Name);
                if (existing != null)
                {
                    RemoveStandard(existing);
                }
            }

            // 数据库写入在 Service，DataGrid/TreeView 由领域事件刷新（阶段2-T07）
            _session.JobDocument.CommitStandard(
                standard, createNewId, SpecularComponent, Observer, Illuminant);
        }

        /// <summary>
        /// 添加一个试样到Database中，如果有必要，显示该试样。
        /// 阶段0关键路径：FinishMeasureOnUiAsync、Import、Edit 均会调用（跨 partial 耦合点）。
        /// </summary>
        /// <param name="test">要添加的试样</param>
        /// <param name="standard">将试样添加到该标样下，标样必须已存在标样列表中或为null</param>
        /// <param name="isSelectedNewTest">是否选中新添加的样品</param>
        public void AddTest(TestSpecimen test, StandardSpecimen standard, bool isSelectedNewTest = false)
        {
            _session.JobDocument.CommitTest(
                test, standard, SpecularComponent, Observer, Illuminant);
            if (isSelectedNewTest)
            {
                CurrentTest = test;
            }
        }

        public void RemoveTest(TestSpecimen test)
        {
            if (CurrentTest == test)
            {
                CurrentTest = null;
            }

            _session.JobDocument.RemoveTestFromDatabase(test);
        }

        private void OnObserverAndIlluminantSettingChanged()
        {
            ResetIlluminantComboBox(Database.ObserverAndIlluminantSetting);
            ObserverIlluminant = Database.ObserverAndIlluminantSetting.Primary;
        }

        private void OnCurrentStandardChanged()
        {
            if (CurrentStandard == null)
            {
                CurrentStandardTreeItem.Header = _("NoCurrentStandard");
                CurrentStandardTreeItem.DataContext = null;
                CurrentStandardTreeItem.Tag = null;
            }
            else
            {
                var item = FindStandard(CurrentStandard);
                if (item == null)
                {
                    return;
                }
                CurrentStandardTreeItem.DataContext = item;
                CurrentStandardTreeItem.Tag = item.Specimen;
                CurrentStandardTreeItem.Header = CreateSpecimenItemHeader(true);
                StandardDataGrid.SelectedItem = item;
            }
        }



        // 颜色设置更改
        void OnChromaticitySettingChanged()
        {
            ResetDisplayedFields();
            RefreshSpecimenData();
        }

        void OnDisplaySettingChanged()
        {
            ResetDisplayedFields();
        }

        private void RefreshToleranceTable()
        {
            EnsureChromaticityDisplay()?.RefreshToleranceTable();
        }

        private void ResetTestDataGrid(ChromaticitySetting setting)
        {
            EnsureChromaticityDisplay()?.ResetTestDataGrid(setting);
        }

        private void ResetStandardDataGrid(ChromaticitySetting setting)
        {
            EnsureChromaticityDisplay()?.ResetStandardDataGrid(setting);
        }

        private ChromaticityDisplayService EnsureChromaticityDisplay()
        {
            if (_chromaticityDisplay == null)
            {
                WireChromaticityDisplay();
            }

            return _chromaticityDisplay;
        }

        /// <summary>
        /// 更新标样和试样表的字段
        /// </summary>
        private void UpdateDataTableFieldNames()
        {
            if (ChromaticitySetting.ColorIndexFields.Contains(ChromaticityField.MI1) ||
                ChromaticitySetting.ColorIndexFields.Contains(ChromaticityField.MI2))
            {
                // 更新同色异谱标签
                Formula ref1, ref2;
                Database.GetReferenceFormula(Formula, out ref1, out ref2);
                foreach (var column in TestDataGrid.Columns)
                {
                    if (column is DataGridTextColumn)
                    {
                        Binding binding = (column as DataGridTextColumn).Binding as Binding;
                        if (binding.Path.Path == "Adapter.MI1")
                        {
                            column.Header = SpecimenAdapter.MakeMIName(ref1);
                        }
                        else if (binding.Path.Path == "Adapter.MI2")
                        {
                            column.Header = SpecimenAdapter.MakeMIName(ref2);
                        }
                    }
                }
            }
        }

        void ChromaticitySettingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ChromaticitySettingDialog dlg = new ChromaticitySettingDialog(WavelengthRange);
            dlg.Owner = this;
            dlg.Title = S._("Title_DisplaySetting");
            dlg.ChromaticitySetting = Database.SystemChromaticitySetting;

            if (dlg.ShowDialog().GetValueOrDefault() && dlg.IsModified)
            {
                // 更新显示的项
                Database.SystemChromaticitySetting.Copy(dlg.ChromaticitySetting);

                // 更新使用的容差项
                Database.SystemToleranceSetting.ItemKeys = Database.SystemChromaticitySetting.ColorDiffFields;

                Database.clearCatchs();
                EnsureViewCoordinator()?.OnChromaticitySettingsChanged();

                Database.IsModified = true;

                SystemDefaultConfig.DisplaySetting.Copy(dlg.ChromaticitySetting);
            }
        }

        void ToleranceSettingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ToleranceSettingDialog dlg = new ToleranceSettingDialog(Database.SystemChromaticitySetting);
            dlg.Owner = this;
            dlg.ToleranceSetting = Database.SystemToleranceSetting;

            // 如果没有参数，表示是用户发出的命令，绑定事件处理函数。
            if (e.Parameter == null)
            {
                dlg.ChangeAccepted += SystemToleranceSettingChangedAccepted;
            }

            dlg.ShowDialog();
        }

        private void SystemToleranceSettingChangedAccepted(object sender, ValueEventArg<bool> e)
        {
            if (e.Value)
            {
                Database.OnIsUseSystemToleranceChanged();
            }

            EnsureViewCoordinator()?.OnSystemToleranceChanged(e.Value);
            Database.IsModified = true;

            SystemDefaultConfig.ToleranceSetting.Copy(Database.SystemToleranceSetting);
            SyncStatisticsPlotTolerance();
        }

        internal StandardSpecimenAdapter GetStandardAdapter(StandardSpecimen standard)
        {
            return Database.GetAdapter(standard, SpecularComponent, Observer, Illuminant);
        }

        /// <summary>
        /// 当前显示的试样
        /// </summary>
        internal IEnumerable<SpecimenWrap> TestsToShow
        {
            get
            {
                SpecimenWrap[] testsToShow = null;
                if (IsShowAllTests)
                {
                    testsToShow = GetAllTestWraps();
                }
                else if (IsShowOrphanTests)
                {
                    testsToShow = GetTestWraps(null);
                }
                else if (IsShowSingleStandard && SingleStandardInShowing != null)  // 显示单一标样及其下的试样
                {
                    testsToShow = GetTestWraps(SingleStandardInShowing);
                }
                if (testsToShow == null)
                    return new SpecimenWrap[0];
                return testsToShow;
            }
        }


        /// <summary>
        /// 当前显示的标样
        /// </summary>
        internal IEnumerable<SpecimenWrap> StandardsToShow
        {
            get
            {
                if (IsShowAllStandards)
                {
                    return GetAllStandardWraps();
                }
                else if (IsShowSingleStandard && SingleStandardInShowing != null)
                {
                    var stdWrap = FindStandard(SingleStandardInShowing);
                    if (stdWrap != null)
                    {
                        return new SpecimenWrap[1] { stdWrap };
                    }
                }
                return new SpecimenWrap[0];
            }
        }


        private void OnPanTongMatchExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentSetting curSet = new CurrentSetting
            {
                SpecCom = SpecularComponent,
                IlluObs = ObserverIlluminant,
                DispSet = SystemDefaultConfig.DisplaySetting,
                ToleranceSet = SystemDefaultConfig.ToleranceSetting,
                CurrentStandard = CurrentStandard,
                MainWindowObj = this,
            };
            if (SelectedStandard == null)
            {
                MessageBox.Show(S._("NoStandard"));
                return;
            }
            PanTongMatch mat = new PanTongMatch(curSet, Colorimeter, SelectedStandard);
            mat.ShowDialog();
        }

        internal void ImportStandards(IEnumerable<StandardSpecimen> standards, bool createNewId = true, bool isRemoveSame = true)
        {
            var imported = standards.ToList();
            if (imported.Count == 0)
            {
                return;
            }

            // 批量导入期间暂停领域事件，避免与末尾 LoadAll 全量重建冲突（阶段2-T04/T07）
            BeginSpecimenBulkUpdate();
            try
            {
                foreach (StandardSpecimen std in imported)
                {
                    std.Gloss8 = CalculateEightDegreesGloss(std.SCI, std.SCE);
                    if (isRemoveSame)
                    {
                        var existing = _session.JobDocument.FindStandardByName(std.Name);
                        if (existing != null)
                        {
                            RemoveStandard(existing);
                        }
                    }

                    _session.JobDocument.ImportStandardWithTests(
                        std, createNewId, SpecularComponent, Observer, Illuminant);
                }
            }
            finally
            {
                EndSpecimenBulkUpdate();
            }

            var first = imported[0];

            if (first.SpecularComponent == SpecularComponent.SCIAndSCE || first.SpecularComponent == SpecularComponent.SCI)
            {
                SpecularComponent = SpecularComponent.SCI;
            }
            else if (first.SpecularComponent == SpecularComponent.SCE)
            {
                SpecularComponent = SpecularComponent.SCE;
            }
            else
            {
                SpecularComponent = SpecularComponent.Unknown;
            }

            ClearNavigationTree();
            LoadAllSpecimens();
            ShowAllStandards();
        }


        internal void ImportTests(TestSpecimen[] tests, StandardSpecimen standard, bool isSelected = false)
        {

            if (tests.Length == 0)
            {
                return;
            }

            foreach (TestSpecimen test in tests)
            {
                test.Gloss8 = CalculateEightDegreesGloss(test.SCI, test.SCE);
                _session.JobDocument.CommitTest(
                    test, standard, SpecularComponent, Observer, Illuminant);
                if (isSelected)
                {
                    CurrentTest = test;
                }
            }
        }

        private void InputStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            StandardInputDialog dlg = new StandardInputDialog(WavelengthRange);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                var std = new StandardSpecimen(dlg.Specimen);
                AddStandard(std);
            }
        }

        // XAML CommandBinding 入口保留；编排已迁 ImportCommandHandler（阶段4-T01）
        private void ImportFromExcelExecuted(object sender, ExecutedRoutedEventArgs e)
            => _importCommands.ExecuteImportFromExcel(sender, e);


        private void OnConvertWhiteBoardData(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog fileDlg = new OpenFileDialog();
            fileDlg.Title = _("MenuItem_ConvertWhiteBoardData");
            fileDlg.Filter = _("ExcelFileFilter");
            fileDlg.CheckFileExists = true;
            if (fileDlg.ShowDialog().GetValueOrDefault())
            {
                var dlg = new ConvertWhiteBoardDataDialog(fileDlg.FileName);
                dlg.Owner = this;
                if (dlg.ShowDialog().GetValueOrDefault())
                {
                    // 白板转换对话框当前不产出 Job 标样；策略入口预留，有标样列表时可走 WhiteBoardExcel
                    //ApplySpecimenImportResult(...);
                }
            }
        }


        private void ImportFromFileExecuted(object sender, ExecutedRoutedEventArgs e)
            => _importCommands.ExecuteImportFromFile(sender, e);

        /// <summary>
        /// 将策略解析结果写入当前 Job 并刷新导航（各来源共用，避免 Excel/文件/仪器库各写一套）。
        /// </summary>
        private void ApplySpecimenImportResult(SpecimenImportResult result)
        {
            if (result == null || result.IsEmpty)
            {
                return;
            }

            if (result.Standards.Count > 0)
            {
                ImportStandards(result.Standards, result.CreateNewId, result.RemoveSame);
            }

            if (result.Tests.Count > 0)
            {
                // Job 文件导入保留原行为：全部试样再按 Tag 尝试挂靠已有标样
                ImportTests(result.Tests.ToArray());
            }
        }

        /// <summary>仪器库 / 外部对话框已组装好的标样列表，经策略统一选项后入库。</summary>
        internal void ImportStandardsViaStrategy(
            SpecimenImportSourceKind kind,
            IEnumerable<StandardSpecimen> standards,
            bool createNewId = true,
            bool isRemoveSame = false)
        {
            var result = SpecimenImportStrategyRegistry.Resolve(kind).Load(new SpecimenImportRequest
            {
                PreParsedStandards = standards,
                CreateNewId = createNewId,
                RemoveSame = isRemoveSame
            });
            ApplySpecimenImportResult(result);
        }

        private void ImportTests(TestSpecimen[] testSpecimens)
        {
            var standardWraps = new List<SpecimenWrap>();
            foreach (var item in StandardGroupTreeItem.Items)
            {
                var standardWrap = (item as TreeViewItem).DataContext as SpecimenWrap;
                standardWraps.Add(standardWrap);
            }
            var loading = new Controls.Loading("Importing...");
            loading.BackgroundWork += (o, p, e) =>
            {
                foreach (var test in testSpecimens)
                {
                    var stdName = test.Tag as string;
                    var standardWrap = standardWraps.FirstOrDefault(x => stdName.Contains(x.Name));
                    if (standardWrap != null)
                        Dispatcher.Invoke(new Action(() => { AddTest(test, standardWrap.Specimen as StandardSpecimen); }));
                    else
                        Dispatcher.Invoke(new Action(() => { AddTest(test, null); }));
                }
            };
            loading.BackgroundWorkerCompleted += (sender, args) => { };
            loading.Start();
        }
        private void StandardToleranceSettingCanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !Database.SystemToleranceSetting.IsFromSystemDefault;
        }
        private void StandardToleranceSettingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var standard = SelectedStandard;
            if (SelectedStandard == null)
            {
                MessageBox.Show(S._("PleaseSelectedAStandard"));
                return;
            }
            ToleranceSettingDialog dlg = new ToleranceSettingDialog(Database.GetChromaticitySetting(SelectedStandard));
            dlg.Owner = this;

            // 如果标样的容差为空，就从系统容差复制一份
            if (standard.ToleranceSetting == null)
            {
                dlg.ToleranceSetting = new ToleranceSetting(Database.SystemToleranceSetting);
            }
            else  // 否则使用标样的容差设置
            {
                dlg.ToleranceSetting = SelectedStandard.ToleranceSetting;
            }
            dlg.ChangeAccepted += OnStandardToleranceSettingAccepted;
            dlg.ShowDialog();
        }

        private void PathlenthSelectExecuted(object sender, ExecutedRoutedEventArgs e)
        {

            Pathlength plview = new Pathlength();
            plview.Owner = this;

            if (e.Parameter == null)
            {
                plview.ChangeAccepted += SystemPathlenthSettingChanged;
            }

            plview.ShowDialog();
        }

        private void TestModeSettingExecuted(object sender, ExecutedRoutedEventArgs e)
        {

            TestModeSettingView plview = new TestModeSettingView();
            plview.Owner = this;


            plview.ShowDialog();
        }
        private void SystemPathlenthSettingChanged(object sender, ValueEventArg<ICUMSAParameter> e)
        {
            // 如果获取容差的途径更改了，需要更改缓存数据和显示
            Database.OnIsUseSystemToleranceChanged();
            UpdataSpecimenBySetting();
            Database.IsModified = true;

            SystemDefaultConfig.ToleranceSetting.Copy(Database.SystemToleranceSetting);
        }
        private void UpdataSpecimenBySetting()
        {
            foreach (TreeViewItem treeViewItem in StandardGroupTreeItem.Items)
            {
                var item = treeViewItem.DataContext as SpecimenWrap;
                var standard = item.Specimen as StandardSpecimen;
                var adapter = Database.UpdataSpecimen(standard, SpecularComponent, Observer, Illuminant);
                item.Adapter = adapter;
            }

        }


        private void OnStandardToleranceSettingAccepted(object sender, EventArgs e)
        {
            ToleranceSettingDialog dlg = sender as ToleranceSettingDialog;
            var standard = SelectedStandard;
            // 如果标样原来没有容差，将其容差设为dlg.ToleranceSetting，并记录ChromaticitySetting
            if (standard.ToleranceSetting == null)
            {
                standard.ToleranceSetting = dlg.ToleranceSetting;
                //standard.ChromaticitySetting = new ChromaticitySetting(ChromaticitySetting);
            }

            if (!Database.SystemToleranceSetting.IsFromSystemDefault)
            {
                Database.UpdateCachesOf(standard);
                Database.UpdateJudgements(standard);
                EnsureViewCoordinator()?.RefreshAdaptersForStandard(standard, withTests: true);
                RefreshToleranceTable();
            }

            SyncStatisticsPlotTolerance();
            Database.IsModified = true;
        }

        /// <summary>
        /// 容差变更后刷新统计图 ΔE 上限虚线，避免仍显示旧范围。
        /// </summary>
        private void SyncStatisticsPlotTolerance(StandardSpecimen standard = null)
        {
            var std = standard ?? SingleStandardInShowing ?? CurrentStandard;
            StatisticsPlot.RangeTolerance = Database.GetTolerance(
                std, new ObserverIlluminantPair(Observer, Illuminant));
        }

        private void StandardChromaticitySettingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ChromaticitySettingDialog dlg = new ChromaticitySettingDialog(WavelengthRange);
            string title = _("Title_StandardDisplaySetting");
            if (title != null)
                dlg.Title = title;
            dlg.Owner = this;

            dlg.ChromaticitySetting = Database.GetChromaticitySetting(SelectedStandard);
            if (dlg.ShowDialog() == true)
            {
                var standard = SelectedStandard;

                if (standard.ChromaticitySetting == null)
                {
                    standard.ChromaticitySetting = dlg.ChromaticitySetting;
                    standard.ToleranceSetting = new ToleranceSetting(Database.SystemToleranceSetting);
                }
                else
                {
                    standard.ChromaticitySetting.Copy(dlg.ChromaticitySetting);
                }

                if (dlg.IsModified)
                {
                    standard.ToleranceSetting.ItemKeys = ChromaticitySetting.ColorDiffFields;

                    Database.UpdateCachesOf(standard);
                    EnsureViewCoordinator()?.OnStandardDisplaySettingsChanged(standard);
                }
                Database.IsModified = true;
            }
        }

        private void SetAsCurrentStandardExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentStandard = SelectedStandard;
        }

        private void OnStaticsPlotPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            foreach (var field in ChromaticitySetting.ColorDiffFields)
            {
                MenuItem item = new MenuItem();
                item.Header = string.Format(_("Show_0"), field.Name());
                item.IsCheckable = true;
                item.IsChecked = StatisticsPlot.IsShow(field.Name());
                item.Tag = field;
                item.Click += StaticsMenuItemClick;

                menu.Items.Add(item);
            }

            StatisticsPlot.ContextMenu = menu;
        }

        private void StaticsMenuItemClick(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                ChromaticityField field = (ChromaticityField)item.Tag;

                if (StatisticsPlot.IsShow(field.Name()))
                    StatisticsPlot.Hide(field.Name());
                else
                    StatisticsPlot.Show(field.Name(), t => (double)t.GetPropertyValue(field));

            }
        }

        private DeltaPlotType[] GetValidDeltaPlotTypes(ChromaticitySetting setting)
        {
            List<DeltaPlotType> list = new List<DeltaPlotType>();
            if (setting.NeedCIEDE1976)
            {
                list.Add(DeltaPlotType.DeltaLab);
                list.Add(DeltaPlotType.DeltaLCH);
            }
            if (setting.NeedDeltaLuv)
                list.Add(DeltaPlotType.DeltaLuv);

            if (setting.NeedDeltaHunterLab)
                list.Add(DeltaPlotType.DeltaHunterLab);

            if (setting.NeedDeltaXyz)
                list.Add(DeltaPlotType.DeltaYxy);

            return list.ToArray();
        }

        void OnSearchBoxPopulating(object sender, PopulatingEventArgs e)
        {
            var searchBox = sender as AutoCompleteBox;
            if (searchBox == null)
            {
                return;
            }

            if (IsShowAllStandards)
                searchBox.ItemsSource = StandardDataGrid.Items;
            else
                searchBox.ItemsSource = TestDataGrid.Items;

            searchBox.FilterMode = AutoCompleteFilterMode.Contains;
        }

        void OnSearchBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                if (IsShowAllStandards)
                    StandardDataGrid.SelectedItem = null;
                else
                    TestDataGrid.SelectedItem = null;
            }
            else
            {
                var specimenWrap = e.AddedItems[0] as SpecimenWrap;
                if (specimenWrap != null)
                {
                    if (specimenWrap.Specimen is StandardSpecimen)
                    {
                        StandardDataGrid.SelectedItem = specimenWrap;
                        StandardDataGrid.ScrollIntoView(specimenWrap);
                    }
                    else
                    {
                        TestDataGrid.SelectedItem = specimenWrap;
                        TestDataGrid.ScrollIntoView(specimenWrap);
                    }
                }
            }
        }

        void OnSearchBtnClick(object sender, RoutedEventArgs e)
        {
            searchBox.Text = null;
            Keyboard.Focus(searchBox);
            Grid grid = VisualTreeHelper.GetChild(searchBox, 0) as Grid;
            var txtbox = grid.Children[0] as TextBox;
            if (txtbox != null)
                txtbox.Focus();
        }

        #endregion

    }
}
