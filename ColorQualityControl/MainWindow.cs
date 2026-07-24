using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorQualityControl.UI.Charts;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl
{
    /// <summary>
    /// Facade：构造、生命周期、XAML 事件/Command 路由、对话框 Owner（阶段4-T02，≤500 行）。
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        // 阶段2-T01：四个核心 Service 由 Session 聚合，便于后续 Mediator 协调
        private readonly Session.MainWindowSession _session;

        public MainWindow() : this(new Session.MainWindowSession())
        {
        }

        internal MainWindow(Session.MainWindowSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _session.AttachJobFacade(this);
            // Database 由 JobDocumentService 构造，此处不再单独 new
            WavelengthRange = new WavelengthRange(360, 780, 10);
            // CommandBinding 在 InitializeComponent 时就会触发 CanExecute，须先装配 Handler
            WireCommandHandlers();
            InitializeComponent();
            // XAML 控件就绪后立即装配 Presentation 层，避免构造函数内 OnDisplaySettingChanged 时 _chromaticityDisplay 为空
            WireChromaticityDisplay();
            WireChartCoordinator();
            WireSpecimenGridPresenter();
            WireSpecimenViewCoordinator();
            WireSpecimenSelectionState();
            WireTreePresenter();
            InitializeApplicationInformation();
            InitializeData();
            OnDisplaySettingChanged();
            ComponentDispatcher.ThreadIdle += ComponentDispatcherOnThreadIdle;
        }

        private void ComponentDispatcherOnThreadIdle(object sender, EventArgs e)
        {
            // 阶段1-T21：备份策略在 JobDocumentService，MainWindow 仅注册空闲回调
            _session.JobDocument.BackupIfModified();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            try
            {
                if (Colorimeter != null && Colorimeter.IsOpen)
                {
                    Colorimeter.Close();
                }
            }
            catch
            {
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitLanguageMenuItem();

            StatusBar.DataContext = this;
            mainMenu.DataContext = this;

            // 替换掉标样和试样表 Delete 命令的默认绑定
            StandardDataGrid.CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Delete, DeleteExecuted, CanActOnSpecimen));
            TestDataGrid.CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Delete, DeleteExecuted, CanActOnSpecimen));

            SpecularComponentComboBox.ItemsSource = new SpecularComponent[]
            {
                SpecularComponent.Unknown,
                SpecularComponent.SCI,
                SpecularComponent.SCE
            };
            OnCurrentStandardChanged();
            InitConnection();
            InitDisplay();
            InitLibrarySubmenu();
            RefreshCharts();
            TryRestoreLatestBackup();
            LoadBluetoothConfig();
        }

        private void CloseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            TryDisconnect();
            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Database.IsModified)
            {
                if (!ConfirmSave())
                {
                    e.Cancel = true;
                    return;
                }

                if (!Database.IsModified)
                {
                    // 用户选「是」并已保存：丢弃内存 Job 并清理备份
                    DiscardJobContent();
                    _session.JobDocument.RemoveBackupFiles();
                }
                // 用户选「否」不保存：保留空闲备份，下次启动可提示恢复
            }

            base.OnClosing(e);
        }

        private void OnSpecularComponentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                SpecularComponent = (SpecularComponent)e.AddedItems[0];
                EnsureViewCoordinator()?.OnViewContextChanged(refreshCharts: false);
                EnsureChartCoordinator()?.RefreshSpectrumPlot();
                EnsureChartCoordinator()?.RefreshAbsorbancePlot();
            }
        }

        private void OnIlluminantSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                ObserverIlluminant = (ObserverIlluminantPair)e.AddedItems[0];

                UpdateDataTableFieldNames();
                EnsureViewCoordinator()?.OnViewContextChanged(refreshCharts: true);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _statusText;

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                NotifyPropertyChanged("StatusText");
            }
        }

        public WavelengthRange WavelengthRange { get; private set; }

        public string _(string key) => S._(key);

        internal SystemDefaultConfig SystemDefaultConfig
            => (Application.Current as App).SystemDefaultConfig;

        public bool IsAutoNaming
        {
            get { return Database.IsAutoNaming; }
            set
            {
                if (value != Database.IsAutoNaming)
                {
                    Database.IsAutoNaming = value;
                    SystemDefaultConfig.IsAutoNaming = value;
                    Database.IsModified = true;
                    NotifyPropertyChanged("IsAutoNaming");
                }
            }
        }

        public bool IsAutoSelectStandard
        {
            get { return SystemDefaultConfig.IsAutoSelectStandard; }
            set
            {
                if (value != SystemDefaultConfig.IsAutoSelectStandard)
                {
                    SystemDefaultConfig.IsAutoSelectStandard = value;
                    NotifyPropertyChanged("IsAutoSelectStandard");
                }
            }
        }

        public bool IsHelpEnabled => Title.Contains("SQCX");
    }
}
