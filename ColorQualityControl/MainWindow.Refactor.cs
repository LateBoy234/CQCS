using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.CommandHandlers;
using ThreeNH.ColorQualityControl.Instrument;
using ThreeNH.ColorQualityControl.Refactoring;
using ThreeNH.ColorQualityControl.Presentation;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.ColorQualityControl.Specimens.Import;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorQualityControl.Utils;
using ThreeNH.ColorSpecimen;
using ThreeNH.Controls;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.Helper.Plugin.Services;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.SQCX.Infrastructure.DependencyInjection;

namespace ThreeNH.ColorQualityControl
{
    /// <summary>
    /// 阶段 1：服务实例与重构契约转发。含 UI 的方法仍由 MainWindow partial 编排。
    /// </summary>
    public partial class MainWindow
        : IJobDocumentService
        , IInstrumentConnectionService
        , IInstrumentConnectionHost
        , IMeasurementHost
        , IMeasurementCaptureGateway
        , IMeasurementService
        , ISpecimenSelectionState
        , IInstrumentCalibrationHost
        , IInstrumentLibraryHost
        , ISpecimenTreeHost
        , IChromaticityDisplayHost
        , IChartHost
        , ISpecimenGridHost
        , ISpecimenViewHost
        , IConnectCommandHost
        , IMeasureCommandHost
        , IImportCommandHost
    {
        private readonly InstrumentCalibrationService _calibration = new InstrumentCalibrationService();
        private readonly InstrumentLibraryService _library = new InstrumentLibraryService();
        private SpecimenTreePresenter _treePresenter;
        private SpecimenGridPresenter _gridPresenter;
        private ChromaticityDisplayService _chromaticityDisplay;
        private ChartCoordinator _chartCoordinator;
        private SpecimenViewCoordinator _viewCoordinator;
        // 阶段4-T01：Command 编排迁出 partial，XAML 仍绑定 MainWindow 薄转发方法
        private ConnectCommandHandler _connectCommands;
        private MeasureCommandHandler _measureCommands;
        private ImportCommandHandler _importCommands;

        internal Session.MainWindowSession Session => _session;

        private void WireCommandHandlers()
        {
            _connectCommands = new ConnectCommandHandler(this, _session.Instrument);
            var measurementGateway =
                new Adapter.LegacyMeasurementGateway(this, _session.Measurement);
            _measureCommands = new MeasureCommandHandler(
                ApplicationServices.CreateMeasurement(measurementGateway));
            _importCommands = new ImportCommandHandler(this);
        }

        bool IConnectCommandHost.IsWindowLoaded => IsLoaded;

        void IConnectCommandHost.UpdateConnectionStatus() => UpdateConnectionStatus();

        bool IMeasureCommandHost.RunCalibration() => DoCalibration();

        void IMeasureCommandHost.ShowError(string message) => MessageBox.Show(message);

        Window IImportCommandHost.DialogOwner => this;

        void IImportCommandHost.ApplySpecimenImportResult(SpecimenImportResult result)
            => ApplySpecimenImportResult(result);

        private void WireTreePresenter()
        {
            _treePresenter = new SpecimenTreePresenter(this, _session.JobDocument);
            // 标试样入库/删除改由领域事件驱动 TreeView，MainWindow 只保留 DataGrid/图表（阶段2-T04）
            _treePresenter.Subscribe();
        }

        private void WireChromaticityDisplay()
        {
            // DataGrid 列构建与容差表迁出 MainWindow.Data，便于后续事件驱动统一刷新（阶段2-T05）
            _chromaticityDisplay = new ChromaticityDisplayService(this);
        }

        private void WireChartCoordinator()
        {
            // 色品图/光谱图刷新迁出 MainWindow，便于与 Specimen 事件统一协调（阶段2-T06）
            _chartCoordinator = new ChartCoordinator(this);
        }

        private void WireSpecimenGridPresenter()
        {
            // DataGrid/光谱图增删改由 SpecimenAdded/Removed 驱动，替代手动 Present/Unpresent（阶段2-T07）
            _gridPresenter = new SpecimenGridPresenter(this, _session.JobDocument);
            _gridPresenter.Subscribe();
        }

        private void WireSpecimenViewCoordinator()
        {
            _viewCoordinator = new SpecimenViewCoordinator(
                this, EnsureChromaticityDisplay(), EnsureChartCoordinator());
        }

        private SpecimenViewCoordinator EnsureViewCoordinator()
        {
            if (_viewCoordinator == null)
            {
                WireSpecimenViewCoordinator();
            }

            return _viewCoordinator;
        }

        private void BeginSpecimenBulkUpdate()
        {
            _gridPresenter?.BeginBulkUpdate();
            _treePresenter?.BeginBulkUpdate();
        }

        private void EndSpecimenBulkUpdate()
        {
            _gridPresenter?.EndBulkUpdate();
            _treePresenter?.EndBulkUpdate();
        }

        private ChartCoordinator EnsureChartCoordinator()
        {
            if (_chartCoordinator == null)
            {
                WireChartCoordinator();
            }

            return _chartCoordinator;
        }

        ContentControl IChartHost.ToleranceChartView => ToleranceChartView;

        ContentControl IChartHost.TriaxialChartView => TriaxialChartView;

        SpectrumPlotControl IChartHost.SpectrumPlot => SpectrumPlot;

        AbsorbancePlotControl IChartHost.AbsorbancePlot => AbsorbancePlot;

        SpecimenDatabase IChartHost.Database => Database;

        StandardObserver IChartHost.Observer => Observer;

        StandardIlluminant IChartHost.Illuminant => Illuminant;

        SpecularComponent IChartHost.SpecularComponent => SpecularComponent;

        StandardSpecimen IChartHost.SingleStandardInShowing => SingleStandardInShowing;

        ChromaticitySetting IChartHost.ChromaticitySetting => ChromaticitySetting;

        IColorData IChartHost.StandardColorData => StandardColorData;

        IList<IColorData> IChartHost.SampleColorDatas => SampleColorDatas;

        IEnumerable<SpecimenWrap> IChartHost.TestsToShow => TestsToShow;

        IEnumerable<SpecimenWrap> IChartHost.StandardsToShow => StandardsToShow;

        private void RefreshCharts(Specimen test = null)
        {
            EnsureChartCoordinator()?.RefreshCharts(test);
        }

        private void RefreshCharts(IList<Specimen> tests)
        {
            EnsureChartCoordinator()?.RefreshCharts(tests);
        }

        private void SetToleranceChartView(Specimen test = null)
        {
            EnsureChartCoordinator()?.SetToleranceChartView(test);
        }

        private void RefreshSpectrumPlot()
        {
            EnsureChartCoordinator()?.RefreshSpectrumPlot();
        }

        private void RefreshAbsorbancePlot()
        {
            EnsureChartCoordinator()?.RefreshAbsorbancePlot();
        }

        DataGrid ISpecimenGridHost.StandardDataGrid => StandardDataGrid;

        DataGrid ISpecimenGridHost.TestDataGrid => TestDataGrid;

        SpectrumPlotControl ISpecimenGridHost.SpectrumPlot => SpectrumPlot;

        AbsorbancePlotControl ISpecimenGridHost.AbsorbancePlot => AbsorbancePlot;

        StatisticsPlotControl ISpecimenGridHost.StatisticsPlot => StatisticsPlot;

        bool ISpecimenGridHost.IsShowAllStandards => IsShowAllStandards;

        bool ISpecimenGridHost.IsShowSingleStandard => IsShowSingleStandard;

        bool ISpecimenGridHost.IsShowAllTests => IsShowAllTests;

        bool ISpecimenGridHost.IsShowOrphanTests => IsShowOrphanTests;

        StandardSpecimen ISpecimenGridHost.SingleStandardInShowing => SingleStandardInShowing;

        TestSpecimen ISpecimenGridHost.CurrentTest => CurrentTest;

        SpecularComponent ISpecimenGridHost.SpecularComponent
        {
            get => SpecularComponent;
            set => SpecularComponent = value;
        }

        bool ISpecimenGridHost.IsShowStandard(StandardSpecimen standard) => IsShowStandard(standard);

        bool ISpecimenGridHost.IsShowTest(TestSpecimen test) => IsShowTest(test);

        string ISpecimenGridHost.SpectrumType => SpectrumType;

        SpecimenWrap ISpecimenGridHost.CreateStandardWrap(StandardSpecimen standard)
            => new SpecimenWrap(_session.JobDocument.GetStandardAdapter(
                standard, SpecularComponent, Observer, Illuminant));

        SpecimenWrap ISpecimenGridHost.CreateTestWrap(TestSpecimen test)
            => new SpecimenWrap(_session.JobDocument.GetTestAdapter(
                test, SpecularComponent, Observer, Illuminant));

        void ISpecimenGridHost.NotifySingleStandardDiagramChanged()
            => EnsureViewCoordinator()?.RefreshSingleStandardDiagram();

        TreeViewItem ISpecimenViewHost.StandardGroupRoot => StandardGroupTreeItem;

        TreeViewItem ISpecimenViewHost.AllTestsRoot => AllTestsTreeItem;

        TreeViewItem ISpecimenViewHost.OrphanTestsRoot => OrphanTestsTreeItem;

        SpecimenDatabase ISpecimenViewHost.Database => Database;

        Formula ISpecimenViewHost.Formula => Formula;

        StandardObserver ISpecimenViewHost.Observer => Observer;

        StandardIlluminant ISpecimenViewHost.Illuminant => Illuminant;

        SpecularComponent ISpecimenViewHost.SpecularComponent => SpecularComponent;

        StandardSpecimen ISpecimenViewHost.SingleStandardInShowing => SingleStandardInShowing;

        StandardSpecimen ISpecimenViewHost.CurrentStandard => CurrentStandard;

        TestSpecimen ISpecimenViewHost.CurrentTest => CurrentTest;

        ChromaticitySetting ISpecimenViewHost.ChromaticitySetting => ChromaticitySetting;

        bool ISpecimenViewHost.IsShowSingleStandard => IsShowSingleStandard;

        StatisticsPlotControl ISpecimenViewHost.StatisticsPlot => StatisticsPlot;

        ColorDifferenceControl ISpecimenViewHost.ColorDiffTable => colorDiffTable;

        DataGrid ISpecimenViewHost.TestDataGrid => TestDataGrid;

        SpectrumPlotControl ISpecimenViewHost.SpectrumPlot => SpectrumPlot;

        AbsorbancePlotControl ISpecimenViewHost.AbsorbancePlot => AbsorbancePlot;

        private void RefreshSpecimenData()
        {
            EnsureViewCoordinator()?.RefreshAdapters();
        }

        private void ResetDisplayedFields()
        {
            EnsureViewCoordinator()?.RefreshDisplayLayout();
        }

        DataGrid IChromaticityDisplayHost.TestDataGrid => TestDataGrid;

        DataGrid IChromaticityDisplayHost.StandardDataGrid => StandardDataGrid;

        DataGrid IChromaticityDisplayHost.ToleranceDataGrid => ToleranceDataGrid;

        SpecimenDatabase IChromaticityDisplayHost.Database => Database;

        Formula IChromaticityDisplayHost.Formula => Formula;

        StandardObserver IChromaticityDisplayHost.Observer => Observer;

        StandardIlluminant IChromaticityDisplayHost.Illuminant => Illuminant;

        ObserverIlluminantPair IChromaticityDisplayHost.ObserverIlluminant => ObserverIlluminant;

        StandardSpecimen IChromaticityDisplayHost.CurrentStandard => CurrentStandard;

        TestSpecimen IChromaticityDisplayHost.CurrentTest => CurrentTest;

        StandardSpecimen IChromaticityDisplayHost.SingleStandardInShowing => SingleStandardInShowing;

        TreeViewItem ISpecimenTreeHost.StandardGroupRoot => StandardGroupTreeItem;

        TreeViewItem ISpecimenTreeHost.AllTestsRoot => AllTestsTreeItem;

        TreeViewItem ISpecimenTreeHost.OrphanTestsRoot => OrphanTestsTreeItem;

        SpecimenWrap ISpecimenTreeHost.CreateStandardWrap(StandardSpecimen standard)
            => new SpecimenWrap(_session.JobDocument.GetStandardAdapter(
                standard, SpecularComponent, Observer, Illuminant));

        SpecimenWrap ISpecimenTreeHost.CreateTestWrap(TestSpecimen test)
            => new SpecimenWrap(_session.JobDocument.GetTestAdapter(
                test, SpecularComponent, Observer, Illuminant));

        void ISpecimenTreeHost.ActivateStandard(StandardSpecimen standard)
        {
            if (standard != CurrentStandard)
            {
                CurrentStandard = standard;
            }
        }

        /// <summary>选中状态变更时刷新 TreeView / DataGrid（阶段1-T11）。</summary>
        private void WireSpecimenSelectionState()
        {
            _session.Selection.CurrentStandardChanged += (_, __) => OnCurrentStandardChanged();
            _session.Selection.CurrentTestChanged += (_, __) =>
            {
                EnsureChromaticityDisplay()?.ResetTestDataGrid(ChromaticitySetting);
                EnsureViewCoordinator()?.OnCurrentTestChanged();
            };
        }

        public StandardSpecimen CurrentStandard
        {
            get => _session.Selection.CurrentStandard;
            set => _session.Selection.CurrentStandard = value;
        }

        public TestSpecimen CurrentTest
        {
            get => _session.Selection.CurrentTest;
            set => _session.Selection.CurrentTest = value;
        }

        public StandardSpecimen SingleStandardInShowing => _session.Selection.SingleStandardInShowing;

        private bool IsShowAllStandards => _session.Selection.IsShowAllStandards;

        private bool IsShowAllTests => _session.Selection.IsShowAllTests;

        private bool IsShowOrphanTests => _session.Selection.IsShowOrphanTests;

        private bool IsShowSingleStandard => _session.Selection.IsShowSingleStandard;

        private bool IsShowCurrentStandard => _session.Selection.IsShowCurrentStandard;

        /// <summary>Service 持有连接状态，MainWindow 订阅后转发 INotifyPropertyChanged 给 XAML。</summary>
        private void WireInstrumentConnectionState()
        {
            _session.Instrument.ConnectionStateChanged += (_, __) =>
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => NotifyInstrumentConnectionPropertiesChanged());
                    return;
                }

                NotifyInstrumentConnectionPropertiesChanged();
            };
        }

        private void NotifyInstrumentConnectionPropertiesChanged()
        {
            NotifyPropertyChanged(nameof(IsConnecting));
            NotifyPropertyChanged(nameof(DeviceSN));
        }

        SpecimenDatabase IJobDocumentService.Database => _session.JobDocument.Database;

        void IJobDocumentService.AddStandard(StandardSpecimen standard, bool createNewId, bool isRemoveSame)
            => AddStandard(standard, createNewId, isRemoveSame);

        void IJobDocumentService.AddTest(TestSpecimen test, StandardSpecimen standard, bool isSelectedNewTest)
            => AddTest(test, standard, isSelectedNewTest);

        void IJobDocumentService.RemoveStandard(StandardSpecimen standard) => RemoveStandard(standard);

        void IJobDocumentService.RemoveTest(TestSpecimen test) => RemoveTest(test);

        void IJobDocumentService.LoadAllSpecimens() => LoadAllSpecimens();

        void IJobDocumentService.Clear() => ResetJobDocument();

        IInstrument IInstrumentConnectionService.Colorimeter => Colorimeter;

        InstrumentType IInstrumentConnectionService.InstrumentType => InstrumentType;

        bool IInstrumentConnectionService.IsConnecting => _session.Instrument.IsConnecting;

        InstrumentConnectionState IInstrumentConnectionService.ConnectionState => _session.Instrument.ConnectionState;

        string IInstrumentConnectionService.DeviceSN => _session.Instrument.DeviceSN;

        ConnectionMethod IInstrumentConnectionHost.ConnectionMethod => SystemDefaultConfig.ConnectionMethod;

        void IInstrumentConnectionHost.SetConnectWay(string way) => ConnectWay = way;

        void IInstrumentConnectionHost.ShowDriverDownloadDialog() => ShowDriverDownloadDialog();

        // 后台连接线程写入 SN，经 Service 统一持有后再通知 UI 绑定
        void IInstrumentConnectionHost.SetDeviceSn(string deviceSn)
            => Dispatcher.Invoke(() => _session.Instrument.SetDeviceSn(deviceSn));

        object IInstrumentConnectionHost.GetConnectionStartupArgument()
        {
            if (SystemDefaultConfig.ConnectionMethod == ConnectionMethod.BySpecificalDevice &&
                !string.IsNullOrWhiteSpace(SystemDefaultConfig.LatestConnection))
            {
                return SystemDefaultConfig.LatestConnection;
            }

            return null;
        }

        void IInstrumentConnectionHost.DetachMeasurementNotification(IInstrument instrument)
        {
            if (instrument is IMeasurementNotification measurementNotification)
            {
                measurementNotification.MeasurementReceived -= OnMeasureResultReceived;
            }
        }

        void IInstrumentConnectionHost.OnInstrumentDisconnected()
        {
            NotifyInstrumentConfigChanged();
            UpdateConnectionStatus();
            ReleaseWhiteboardSpectrum();
            CommandManager.InvalidateRequerySuggested();
        }

        void IInstrumentConnectionHost.ShowConnectionError(string message) => MessageBox.Show(message);

        StandardSpecimen IMeasurementService.GetBestStandard(Specimen test) => _session.Measurement.GetBestStandard(test);

        Task IMeasurementService.FinishMeasureOnUiAsync(Specimen sample) => _session.Measurement.FinishMeasureOnUiAsync(sample);

        Task IMeasurementService.ExecuteTrialMeasurementsAsync() => _session.Measurement.ExecuteTrialMeasurementsAsync();

        Task IMeasurementService.ExecuteStandardMeasurementsAsync() => _session.Measurement.ExecuteStandardMeasurementsAsync();

        Task IMeasurementService.ExecuteHazeTrialMeasurementAsync() => _session.Measurement.ExecuteHazeTrialMeasurementAsync();

        Task IMeasurementService.ExecuteHazeStandardMeasurementAsync() => _session.Measurement.ExecuteHazeStandardMeasurementAsync();

        StandardSpecimen IMeasurementHost.CurrentStandard => CurrentStandard;

        StandardSpecimen IMeasurementHost.SingleStandardInShowing => SingleStandardInShowing;

        bool IMeasurementHost.IsAutoSelectStandard => IsAutoSelectStandard;

        bool IMeasurementHost.IsAutoNaming => IsAutoNaming;

        SpecimenDatabase IMeasurementHost.Database => Database;

        SpecularComponent IMeasurementHost.SpecularComponent => SpecularComponent;

        StandardObserver IMeasurementHost.Observer => Observer;

        StandardIlluminant IMeasurementHost.Illuminant => Illuminant;

        Formula IMeasurementHost.ColorFormula => Formula;

        DataModel.Tolerance IMeasurementHost.AutoSelectStandardLimits => SystemDefaultConfig.AutoSelectStandardLimits;

        string IMeasurementCaptureGateway.GetInstrumentModel() => colorimeter?.Model;

        bool IMeasurementHost.TryPromptTestName(TestSpecimen test, StandardSpecimen standard)
        {
            var dlg = new GetSpecimenNameDialog
            {
                Owner = this,
                SpecimenName = Database.TestNameGenerator.GenerateName(test, Database.GetNewTestIndex(standard)),
                Material = standard?.Material ?? Database.DefaultMaterial
            };

            if (dlg.ShowDialog() != true)
            {
                // 取消命名不结束 Measuring：整批仍持锁，避免连续测样中途菜单被误恢复（3-T06）
                return false;
            }

            test.Name = dlg.SpecimenName;
            test.Material = dlg.Material;
            return true;
        }

        void IMeasurementHost.OnTestMeasurementCompleted(TestSpecimen test, StandardSpecimen standard)
        {
            // 仪器回调与 Loading 完成可能在非 UI 线程，统一 Invoke 刷新 TreeView / 图表
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ApplyMeasuredTestResult(test, standard));
                return;
            }

            ApplyMeasuredTestResult(test, standard);
        }

        MeasurementSchedule IMeasurementHost.GetTrialMeasurementSchedule()
        {
            var parameter = SystemDefaultConfig.TestModelParameter;
            bool single = parameter.SampMeasurementMode == MeasurementMode.SingleMeasure;
            return new MeasurementSchedule
            {
                Times = single ? 1 : parameter.SampRepeatTimes,
                DelaySeconds = parameter.SampDelayTimeMs,
                ModeLabel = single ? S._("rd_SingleMeasure") : S._("rd_ContinuousMeasure")
            };
        }

        MeasurementSchedule IMeasurementHost.GetStandardMeasurementSchedule()
        {
            var parameter = SystemDefaultConfig.TestModelParameter;
            bool single = parameter.StdMeasurementMode == MeasurementMode.SingleMeasure;
            return new MeasurementSchedule
            {
                Times = single ? 1 : parameter.StdRepeatTimes,
                DelaySeconds = parameter.StdDelayTimeMs,
                ModeLabel = single ? S._("rd_SingleMeasure") : S._("rd_ContinuousMeasure")
            };
        }

        string IMeasurementHost.FormatMeasureProgressMessage(string modeLabel, int current, int total)
            => string.Format($"{modeLabel}：{S._("Measuring_m_n")}", current, total);

        Task<Specimen> IMeasurementCaptureGateway.CaptureTrialSampleAsync(string loadingMessage, double delaySeconds)
            => CaptureInstrumentSampleAsync(loadingMessage, delaySeconds, () => colorimeter.MeasureSample());

        Task<Specimen> IMeasurementCaptureGateway.CaptureStandardSampleAsync(string loadingMessage, double delaySeconds)
            => CaptureInstrumentSampleAsync(loadingMessage, delaySeconds, () => colorimeter.MeasureStandard());

        /// <summary>Loading 采集仪器的公共壳，测标/测样仅后台调用不同。</summary>
        private async Task<Specimen> CaptureInstrumentSampleAsync(
            string loadingMessage,
            double delaySeconds,
            Func<Specimen> measure)
        {
            var tcs = new TaskCompletionSource<Specimen>();
            var loading = new Loading(loadingMessage)
            {
                BackgroundWork = (report, setResult, args) =>
                {
                    try
                    {
                        Thread.Sleep((int)(delaySeconds * 1000));
                        setResult(measure());
                    }
                    catch (Exception ex)
                    {
                        setResult(ex);
                    }
                }
            };

            loading.BackgroundWorkerCompleted += (_, args) =>
            {
                try
                {
                    if (args.IsCancelled)
                    {
                        tcs.TrySetResult(null);
                        return;
                    }

                    if (args.Result is Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        tcs.TrySetResult(null);
                        return;
                    }

                    var specimen = args.Result as Specimen;
                    if (specimen == null)
                    {
                        MessageBox.Show("未获取到有效数据");
                        tcs.TrySetResult(null);
                        return;
                    }

                    tcs.TrySetResult(specimen);
                }
                catch (Exception uiEx)
                {
                    MessageBox.Show(uiEx.Message);
                    tcs.TrySetResult(null);
                }
            };

            loading.Start();
            return await tcs.Task.ConfigureAwait(true);
        }

        int? IMeasurementCaptureGateway.CalculateEightDegreesGloss(ColorData sci, ColorData sce)
            => CalculateEightDegreesGloss(sci, sce);

        bool IMeasurementCaptureGateway.IsInstrumentClarityValid()
            => colorimeter != null && colorimeter.IsClarityValid(); 

        double IMeasurementHost.PromptAndMeasureClarity() => MeasureClarity();

        bool IMeasurementCaptureGateway.ShouldMarkSampleAsTransmission()
            => colorimeter?.Model?.Contains("ts40") == true;

        bool IMeasurementHost.TryPromptStandardName(StandardSpecimen standard)
        {
            var dlg = new GetSpecimenNameDialog
            {
                Owner = this,
                SpecimenName = Database.StandardNamgeGenerator.GenerateName(
                    standard, Database.GetNewStandardIndex()),
                Material = Database.DefaultMaterial
            };

            if (dlg.ShowDialog() != true)
            {
                // 同测样：取消命名不 Force Idle，由 ReleaseMeasureLock 统一收尾
                return false;
            }

            standard.Name = dlg.SpecimenName;
            standard.Material = dlg.Material;
            return true;
        }

        void IMeasurementHost.OnStandardMeasurementCompleted(StandardSpecimen standard)
        {
            // Loading 完成回调在 UI 线程，与原先 ProcessSingleStandard 内 Dispatcher.Invoke 等效
            // 不再在此处 UpdateMeasureUi(false)：连续测标时单次完成仍属 Measuring（3-T06）
            _session.AddStandard(standard);
            CurrentStandard = standard;
            ShowSingleStandard(standard);

            StandardGroupTreeItem.IsExpanded = true;
            _treePresenter.ExpandStandard(standard);

            RefreshCharts();

            if (SystemDefaultConfig.AutoSaveStandardToLibrary)
            {
                // MS-01：自动存库编排下沉 InstrumentLibraryService（阶段1-T24）
                _library.AutoSaveStandardToColorLibrary(
                    true,
                    ColorLibraryInfo.LastUsedLibrary,
                    standard);
            }
        }

        void IMeasurementHost.ShowMeasureError(string message) => MessageBox.Show(message);

        bool IMeasurementHost.EnsureTransmissiveModeForHaze()
        {
            var setting = Colorimeter as ISystemSetting;
            if (setting == null)
            {
                return false;
            }

            if (setting.SpectrumMeasurementMode == SpectrumMeasurementMode.Transmissive)
            {
                return true;
            }

            try
            {
                setting.SpectrumMeasurementMode = SpectrumMeasurementMode.Transmissive;
                return true;
            }
            catch (NotSupportedException)
            {
                MessageBox.Show(S._("Please switch to transmissive mode"));
                return false;
            }
        }

        // 雾度透射校准编排已下沉 InstrumentCalibrationService（阶段1-T23）
        bool IMeasurementHost.EnsureTransmissionCalibratedForHaze()
            => _calibration.EnsureTransmissionCalibrated();

        Specimen IMeasurementHost.CaptureHazeSpecimen()
        {
            var dialog = new HazeMeasurementDialog(Colorimeter, HazeReferenceTuple)
            {
                Owner = this
            };
            if (!dialog.ShowDialog().GetValueOrDefault())
            {
                return null;
            }

            HazeReferenceTuple = dialog.HazeRef;
            var sample = dialog.Result;
            if (sample == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sample.Instrument))
            {
                sample.Instrument = colorimeter.Model;
            }

            return sample;
        }

        bool IMeasurementHost.TryHandleMeasurementAbnormal(Exception ex) => _calibration.TryHandleMeasurementAbnormal(ex);

        IInstrument IInstrumentCalibrationHost.Colorimeter => Colorimeter;

        bool IInstrumentCalibrationHost.ConfirmBlackCalibration(bool isTransmission)
        {
            var prompt = isTransmission ? _("TransmissionBlackCalibrationPrompt") : _("BlackCalibrationPrompt");
            return MessageBox.Show(prompt, _("Title_BlackCalibration"), MessageBoxButton.OKCancel, MessageBoxImage.Information)
                   != MessageBoxResult.Cancel;
        }

        bool IInstrumentCalibrationHost.ConfirmWhiteCalibration(bool isTransmission)
        {
            var prompt = isTransmission ? _("TransmissionWhiteCalibrationPrompt") : _("WhiteCalibrationPrompt");
            return MessageBox.Show(prompt, _("Title_WhiteCalibration"), MessageBoxButton.OKCancel, MessageBoxImage.Information)
                   != MessageBoxResult.Cancel;
        }

        void IInstrumentCalibrationHost.ShowCalibrationCompleted() => MessageBox.Show(_("CalibrationCompleted"));

        void IInstrumentCalibrationHost.ShowBlackCalibrationFailed(string exMessage)
            => MessageBox.Show(string.Format(_("BlackCalibrationFailed") + ": " + exMessage), _("Title_BlackCalibration"),
                MessageBoxButton.OK, MessageBoxImage.Exclamation);

        void IInstrumentCalibrationHost.ShowWhiteCalibrationFailed(string exMessage)
            => MessageBox.Show(string.Format(_("WhiteCalibrationFailed") + ": " + exMessage), _("Title_WhiteCalibration"),
                MessageBoxButton.OK, MessageBoxImage.Exclamation);

        void IInstrumentCalibrationHost.ShowRefreshInstrumentInfoFailed(string message)
            => MessageBox.Show(string.Format(_("RefereshInstrumentInfoFailed_0"), message));

        void IInstrumentCalibrationHost.ShowCommunicationError(string errorCodeKey)
            => MessageBox.Show(S._(errorCodeKey), S._("SystemException"), MessageBoxButton.OK, MessageBoxImage.Error);

        bool IInstrumentCalibrationHost.ShowInstrumentCalibrationDialog(IInstrument colorimeter)
        {
            var calibration = new CalibrationWindow(colorimeter);
            return calibration.ShowDialog().GetValueOrDefault();
        }

        bool IInstrumentCalibrationHost.ConfirmCalibrateAfterMeasurementAbnormal()
            => MessageBox.Show(_("MeasurementAbnormalPleaseCalibrate"), _("Title_MeasurementAbnormal"),
                MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.OK;

        void IInstrumentLibraryHost.InvokeOnUi(Action action)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(action);
                return;
            }

            action();
        }

        IInstrument IInstrumentLibraryHost.Colorimeter => Colorimeter;

        StandardSpecimen[] IInstrumentLibraryHost.GetJobStandardSpecimens() => Database.StandardSpecimens;

        void IInstrumentLibraryHost.ShowColorLibraryAddSuccess()
            => ToastInfo.ShowToastSuccess(S._("AddedToLibrarySuccessfully"));

        void IInstrumentLibraryHost.ShowColorLibraryAddSuccessToast()
            => ToastInfo.ShowToastSuccess(S._("AddedToLibrarySuccessfully"));

        void IInstrumentLibraryHost.ShowColorLibraryDuplicateError()
            => ToastInfo.ShowToastWorning(S._("WarningColorNumberDuplicated"));

        void IInstrumentLibraryHost.ShowColorLibraryDuplicateErrorToast(string detail)
            => ToastInfo.ShowToastError($"{S._("WarningColorNumberDuplicated")}:{detail}");

        void IInstrumentLibraryHost.ShowInstrumentLibraryNotSupported()
            => ToastInfo.ShowToastError(_("TheOperationIsNotSupported"));

        void IInstrumentLibraryHost.ShowPleaseSelectAStandard()
            => ToastInfo.ShowToastInfo(_("PleaseSelectedAStandard"));

        void IInstrumentLibraryHost.SetInstrumentLibraryBusy(bool busy, string statusResourceKey)
        {
            mainMenu.IsEnabled = !busy;
            _toolBarTray.IsEnabled = !busy;
            StatusText = _(statusResourceKey);
        }

        void IInstrumentLibraryHost.ShowInstrumentLibraryWriteCompleted()
            => MessageBox.Show(_("WritingToInstrumentLibraryCompleted"));

        void IInstrumentLibraryHost.ShowInstrumentLibraryOperationCompleted()
            => MessageBox.Show(S._("OptionCompleted"));

        void IInstrumentLibraryHost.ShowInstrumentLibraryError(string message)
            => MessageBox.Show(_(message), Title, MessageBoxButton.OK, MessageBoxImage.Error);

        bool IInstrumentLibraryHost.ConfirmClearInstrumentLibrary()
            => MessageBox.Show(_("ConfirmClearInstrumentLibrary"), Title,
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.Cancel;

        bool IInstrumentLibraryHost.ConfirmUpdateInstrumentLibrary()
            => MessageBox.Show(_("ConfirmUpdateInstrumentLibrary"), Title,
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.Cancel;

        void IInstrumentLibraryHost.ImportInstrumentLibraryTargets(Specimen[] targets)
            => ImportStandards(from target in targets select new StandardSpecimen(target));

        // 由 MeasureStateMachine.StateChanged 推送，调用方勿再直接 UpdateMeasureUi
        void IMeasurementHost.SetMeasureUiActive(bool isMeasuring) => UpdateMeasureUi(isMeasuring);

        StandardSpecimen ISpecimenSelectionState.CurrentStandard
        {
            get => _session.Selection.CurrentStandard;
            set => _session.Selection.CurrentStandard = value;
        }

        TestSpecimen ISpecimenSelectionState.CurrentTest
        {
            get => _session.Selection.CurrentTest;
            set => _session.Selection.CurrentTest = value;
        }

        StandardSpecimen ISpecimenSelectionState.SingleStandardInShowing => _session.Selection.SingleStandardInShowing;
    }
}
