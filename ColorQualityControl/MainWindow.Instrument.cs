using PanTongColorMatchSearch;
using PTS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ThreeNH.BleProtocol;
using ThreeNH.Bluetooth;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Device;
using ThreeNH.ColorQualityControl.Reports;
using ThreeNH.ColorQualityControl.Services;
using ThreeNH.ColorQualityControl.UI;
using ThreeNH.ColorQualityControl.Utils;
using ThreeNH.ColorQualityControl.ViewModels;
using ThreeNH.ColorQualityControl.ViewModels.Charts;
using ThreeNH.ColorQualityControl.YRColorimeter;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommDevice;
using ThreeNH.CommunicationBase;
using ThreeNH.Controls;
using ThreeNH.CR400;
using ThreeNH.DataModel;
using ThreeNH.Optical;
using ThreeNH.PortService;
using ThreeNH.Spectrophotometer;
using ThreeNH.YL30;
using BleSpectrophotometer = BTS.BleSpectrophotometer;
using Formula = ThreeNH.ColorAlgorithm.Formula;
using RelayCommand = PanTongColorMatchSearch.RelayCommand;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        //IColorimeter colorimeter = null;
        private IInstrument colorimeter
        {
            get => _session.Instrument.ColorimeterBacking;
            set => _session.Instrument.ColorimeterBacking = value;
        }

        private BthAutoConnectConfig _bthConfig;

        public BthAutoConnectConfig BthConnectConfig
        {
            get
            {
                if (_bthConfig == null)
                {
                    return new BthAutoConnectConfig();
                }
                return _bthConfig;
            }
            set { _bthConfig = value; }
        }

        private void LoadBluetoothConfig()
        {
            try
            {
                var result = Utils.JsonHelper.Load<BthAutoConnectConfig>(PathHelper.BthAutoConnectFilePath);
                if (result == null)
                {
                    BthConnectConfig = new BthAutoConnectConfig();
                    Utils.JsonHelper.Save(BthConnectConfig, PathHelper.BthAutoConnectFilePath);
                    Thread.Sleep(300);
                    BthConnectConfig = Utils.JsonHelper.Load<BthAutoConnectConfig>(PathHelper.BthAutoConnectFilePath);
                }
                else
                {
                    BthConnectConfig = result;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private InstrumentType instrumentType
        {
            get => _session.Instrument.InstrumentType;
            set => _session.Instrument.InstrumentType = value;
        }

        // 连接状态由 Service 持有；MainWindow 保留 set 以兼容 WPF TextBox 默认 TwoWay 绑定
        public bool IsConnecting
        {
            get => _session.Instrument.IsConnecting;
            set => _session.Instrument.SetConnecting(value);
        }

        public string DeviceSN
        {
            get => _session.Instrument.DeviceSN;
            set => _session.Instrument.SetDeviceSn(value);
        }

        private bool _isHazeEnabled;
        private bool _canHaze = false;

        public bool IsCanUseHaze
        {
            get => _canHaze;
            set
            {
                _canHaze = value;
                NotifyPropertyChanged(nameof(IsCanUseHaze));
            }
        }

        public IInstrument Colorimeter
        {
            get { return colorimeter; }
        }

        public InstrumentType InstrumentType => instrumentType;

        /// <summary>
        /// 雾度计算的参考T1和T3的值，分别对应Item1和Item2
        /// </summary>
        public Tuple<ColorData, ColorData> HazeReferenceTuple { get; set; }

        public IColorData StandardColorData
        {
            get
            {
                if (CurrentStandard == null) return null;
                return new ChartColorData(Illuminant, Observer, SpecularComponent, CurrentStandard);
            }
        }

        public List<IColorData> SampleColorDatas
        {
            get
            {
                List<IColorData> samples = new List<IColorData>();
                if (CurrentStandard != null && CurrentStandard.TestSpecimens != null)
                {
                    //防止客户试样太多加载太慢，最多只显示20个点
                    IEnumerable<Specimen> specimens = CurrentStandard.TestSpecimens.Count > 20 ? CurrentStandard.TestSpecimens.Take(20) : CurrentStandard.TestSpecimens;
                    foreach (var test in specimens)
                    {
                        var data = new ChartColorData(Illuminant, Observer, SpecularComponent, test);
                        samples.Add(data);
                    }
                }
                return samples;
            }
        }

        #region 仪器连接相关函数

        // DoConnectionWork 已迁入 InstrumentConnectionService.ConnectionWork（阶段 1）

        private string _connectWay;

        public string ConnectWay
        {
            get { return _connectWay; }
            set
            {
                _connectWay = value;
                NotifyPropertyChanged(nameof(ConnectWay));
            }
        }

        #region 展讯平台驱动检测逻辑

        /// <summary>
        /// 显示驱动下载提示对话框（自动识别32/64位系统）
        /// </summary>
        private void ShowDriverDownloadDialog()
        {
            // 检测操作系统位数
            bool is64Bit = Environment.Is64BitOperatingSystem;
            string architecture = is64Bit ? "64位(x64)" : "32位(x86)";

            // 官网下载页面（假设官网有区分32/64位的下载链接）
            string downloadUrl = is64Bit
                ? "https://tnh.3n88.com/app/SQCX/driver/SciU2Sx64.zip"  // 64位下载页
                : "https://tnh.3n88.com/app/SQCX/driver/SciU2Sx86.zip"; // 32位下载页


            var message = new StringBuilder();
            message.AppendLine(S._("SpreadtrumDriverNotFound"));
            message.AppendLine();
            message.AppendLine(S._("DownLoadSpreadtrumDriver"));


            var result = MessageBox.Show(
                     message.ToString(),
                     S._("DriverNotFoundTitle"),
                     MessageBoxButton.OKCancel,
                     MessageBoxImage.Warning,
                     MessageBoxResult.OK);

            if (result == MessageBoxResult.OK)
            {
                OpenUrl(downloadUrl);
            }
        }
        /// <summary>
        /// 使用默认浏览器打开URL
        /// </summary>
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true  // .NET Core/.NET 5+ 必需
                });
            }
            catch
            {
                // 降级方案：复制到剪贴板
                Clipboard.SetText(url);
                MessageBox.Show(
                    $"无法自动打开浏览器，链接已复制到剪贴板：\n{url}",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        #endregion

        private bool CanOpenBluetoothWindow = false;
        private void ConnectionProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string text)
            {
                StatusText = text;
            }
            else if (e.UserState is ConnectionProgressRequest request)
            {
                if (request == ConnectionProgressRequest.YS30BluetoothSelectDevice)
                {
                    try
                    {
                        BluetoothDetector detector = new BluetoothDetector(true, true, true, true, false);
                        _session.Instrument.SelectedDevice = detector.SelectDevice();
                    }
                    finally
                    {
                        // Worker 在 WaitForDeviceSelection 上阻塞，选完（含取消）必须 Set，否则会一直无响应
                        _session.Instrument.SignalDeviceSelectionDone();
                    }
                }
                else if (request == ConnectionProgressRequest.DongleSelectSlaveDevice)
                {
                    if (BthConnectConfig.IsAutoConnect && !CanOpenBluetoothWindow)
                    {
                        SearchBluetoothDeviceAutomatically();
                    }
                    else
                    {
                        var dlg = new ConnectByBluetooth5ManualDlg(BthConnectConfig);
                        var result = dlg.ShowDialog();
                        if (result.GetValueOrDefault())
                        {
                            _session.Instrument.SelectedDevice = dlg.CurrentBlueToothDevice;
                            _session.Instrument.SelectedPortName = dlg.PortName;
                            BthConnectConfig.IsAutoConnect = dlg.IsAutoConnectBth;
                            BthConnectConfig.IsAutoConnectOtherInstrument = dlg.IsAutoConnectOtherInstrument;
                            BthConnectConfig.SlaveName = dlg.CurrentBlueToothDevice.Name;
                            BthConnectConfig.SlaveMac = dlg.CurrentBlueToothDevice.Address;
                        }
                        else
                        {
                            if (dlg.CurrentBlueToothDevice != null)
                            {
                                BthConnectConfig.SlaveName = dlg.CurrentBlueToothDevice.Name;
                                BthConnectConfig.SlaveMac = dlg.CurrentBlueToothDevice.Address;
                            }
                            BthConnectConfig.IsAutoConnect = dlg.IsAutoConnectBth;
                            BthConnectConfig.IsAutoConnectOtherInstrument = dlg.IsAutoConnectOtherInstrument;
                        }
                        Utils.JsonHelper.Save(BthConnectConfig, PathHelper.BthAutoConnectFilePath);
                        CanOpenBluetoothWindow = false;
                        _session.Instrument.SignalDeviceSelectionDone();
                    }

                }
                else if (request == ConnectionProgressRequest.YL30SpecialDevice)
                {
                    var dlg = new YL30ConnectDlg();
                    var result = dlg.ShowDialog();
                    if (result.GetValueOrDefault())
                    {
                        _session.Instrument.SelectedPortName = dlg.SerialComPort;
                        _session.Instrument.SelectedSpecialDevice = dlg.CurrentYL30Device;
                    }
                    _session.Instrument.SignalDeviceSelectionDone();
                }
            }
        }

        private void SearchBluetoothDeviceAutomatically()
        {
            Dispatcher.Invoke(() =>
            {
                _session.Instrument.SetDeviceSn($"SN:{BthConnectConfig.SlaveName}");
            });
            this.StartSearch();
        }

        private SerialPort _serialPort;
        private EhMc17AtInterface _dongle;
        private bool _isBtConnectedAutoly = false;
        public void StartSearch()
        {
            try
            {
                ClosePortAndDongle();
                devices.Clear();
                var ports = SerialPortService.SerialPorts.Where(x => InstrumentConnectionService.IsBluetoothDonglePort(x.Description)).Select(o => o.PortName).ToList();
                if (ports.Count == 0) return;//无可用蓝牙适配器端口，开启蓝牙适配器等待功能，在端口接入中断事件处设置
                var port = ports.FirstOrDefault();
                _serialPort = new SerialPort(port, 115200);
                _serialPort.Open();
                _dongle = new EhMc17AtInterface(_serialPort);
                _dongle.DeviceDiscovered += DeviceDiscovered;
                Task.Factory.StartNew(() =>
                {
                    if (_dongle.InitDongle())
                    {
                        _dongle.BeginScan();
                        // 扫描态收口到 ConnectionStateMachine，替代原 _isSearching
                        _session.Instrument.BeginSearch();
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void ClosePortAndDongle()
        {
            try
            {
                if (_dongle != null)
                {
                    _dongle.DeviceDiscovered -= DeviceDiscovered;
                    _dongle.Dispose();
                }
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
            }
            catch
            {

            }
        }
        public void StopSearch()
        {
            try
            {
                if (_dongle != null)
                {
                    _dongle.StopScan();
                    _session.Instrument.EndSearch();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void DeviceDiscovered(EhMc17AtInterface.SlaveDevice device)
        {
            devices.Add(device);
            if (device.Name == BthConnectConfig.SlaveName && device.Address == BthConnectConfig.SlaveMac)
            {
                _session.Instrument.SetConnecting(true);
                this.StopSearch();
                _session.Instrument.SelectedDevice = device;
                _session.Instrument.SelectedPortName = _serialPort.PortName;
                await Task.Delay(3000);
                if (!_dongle.Connect(device.Address, out var msg))
                {
                    await Task.Delay(1000);
                    if (!_dongle.Connect(device.Address, out var msgs))
                    {
                        MessageBox.Show(string.Format(S._("ConnectFailPleaseConnectbymanual$0$"), msg));
                        _session.Instrument.SelectedDevice = null;
                        _session.Instrument.SetConnecting(false);
                        // 连接失败也必须唤醒 Worker，否则 WaitForDeviceSelection 会永久阻塞
                        _session.Instrument.SignalDeviceSelectionDone();
                        return;
                    }
                }
                this.ClosePortAndDongle();
                _session.Instrument.SignalDeviceSelectionDone();
            }
        }
        List<EhMc17AtInterface.SlaveDevice> devices = new List<EhMc17AtInterface.SlaveDevice>();

        private void ReStartSearchBluetooth()
        {
            if (SystemDefaultConfig.ConnectionMethod == ConnectionMethod.ByBluettooth_5 && BthConnectConfig.IsAutoConnect)
            {
                if (!_session.Instrument.IsWorkerBusy)
                {
                    _session.Instrument.StartConnection();
                }
            }
        }
        private void ConnectionCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (e.Result is Tuple<IInstrument, InstrumentType> tuple)
                {
                    colorimeter = tuple.Item1;
                    instrumentType = tuple.Item2;
                    InitializeWhiteBoardData();
                    if (instrumentType == InstrumentType.BTS || instrumentType == InstrumentType.JSON)
                    {
                        if (colorimeter is ISystemSetting systemSetting)
                        {
                            systemSetting.RefreshCaches();
                            if (systemSetting.SpectrumMeasurementMode == SpectrumMeasurementMode.Transmissive)
                            {
                                //IsHazeEnabled = true;
                                IsCanUseHaze = true;
                            }
                        }
                        else if (instrumentType == InstrumentType.BTS || instrumentType == InstrumentType.JSON)
                        {
                            IsCanUseHaze = true;
                        }
                    }
                }
                else
                {
                    colorimeter = null;
                    instrumentType = InstrumentType.Unknown;
                    IsHazeEnabled = false;
                    IsCanUseHaze = false;
                    ReleaseWhiteboardSpectrum();
                }
                _session.Instrument.OnConnectionWorkerCompleted();
                StatusText = S._("Ready");
                UpdateInstrumentUi(instrumentType);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message},{ex.StackTrace},{ex.InnerException?.Message}");
                _session.Instrument.OnConnectionWorkerCompleted();
            }
        }

        /// <summary>
        /// 初始化白板参数(8度光泽度需要白板反射率)
        /// </summary>
        private void InitializeWhiteBoardData()
        {
            if (colorimeter == null || !colorimeter.IsOpen)
                return;

            if (colorimeter is BluetoothDongleSpectrometer bts)
            {
                var productInfo = bts.GetProductInfo();
                WSCI = new ColorData(new Spectrum(bts.WavelengthRange, Array.ConvertAll(productInfo.Sci, f => (double)f)));
                WSCE = new ColorData(new Spectrum(bts.WavelengthRange, Array.ConvertAll(productInfo.Sce, f => (double)f)));
            }
            if (colorimeter is BTS.Spectrometer btsspectrometer)
            {
                var wb = btsspectrometer.GetWhiteBoardParameter();
                WSCI = new ColorData(new Spectrum(btsspectrometer.WavelengthRange, Array.ConvertAll(wb.Sci, f => f * 0.01)));
                WSCE = new ColorData(new Spectrum(btsspectrometer.WavelengthRange, Array.ConvertAll(wb.Sce, f => f * 0.01)));
            }
            else if (colorimeter is PTS.Spectrometer ptsspectrometer)
            {
                var productInfo = ptsspectrometer.GetProductInfo();
                WSCI = new ColorData(new Spectrum(ptsspectrometer.WavelengthRange,
                    Array.ConvertAll(productInfo.WhitePlate.Sci, f => (double)f)));
                WSCE = new ColorData(new Spectrum(ptsspectrometer.WavelengthRange,
                    Array.ConvertAll(productInfo.WhitePlate.Sce, f => (double)f)));
            }
            else if (colorimeter is GratingSpectrometer.Spectrometer gratingspectrometer)
            {
                var whiteBoardParameter = gratingspectrometer.GetWhiteBoardParameter();
                if (whiteBoardParameter.SCI != null)
                    WSCI = new ColorData(whiteBoardParameter.SCI);
                if (whiteBoardParameter.SCE != null)
                    WSCE = new ColorData(whiteBoardParameter.SCE);
            }
            else if (colorimeter is ThreeNH.Spectrophotometer.TS8450 ts8450)
            {
                ts8450.GetWhiteboard(out var sn, out var number, out var sci, out var sce);
                if (sci != null)
                {
                    WSCI = new ColorData(new Spectrum(ts8450.WavelengthRange,
                    Array.ConvertAll(sci.Data, f => (double)f)));
                }
                if (sce != null)
                {
                    WSCE = new ColorData(new Spectrum(ts8450.WavelengthRange,
                   Array.ConvertAll(sce.Data, f => (double)f)));
                }

            }
        }

        #endregion 仪器连接相关函数
        private void InitConnection()
        {
            WireInstrumentConnectionState();
            _session.Measurement.AttachHost(this);
            _calibration.AttachHost(this);
            _library.AttachHost(this);
            _session.Instrument.AttachHost(this, ConnectionProgressChanged, ConnectionCompleted);
            _session.Instrument.EnsureWorker();
            _session.Instrument.StartConnection();
            DiscoverSerialPortDevice();
            DiscoverSocektDevice();

        }

      
        /// <summary>
        /// 串口连接事件监听
        /// </summary>
        private void DiscoverSerialPortDevice()
        {
            SerialPortService.PortsChanged += (obj, arg) => Dispatcher.Invoke(
                                new System.EventHandler<PortService.PortsChangedArgs>(OnSerialPortsChanged),
                                obj, arg);
        }

        /// <summary>
        /// socket连接事件监听
        /// </summary>
        private void DiscoverSocektDevice()
        {
          
            NetworkConnectivityMonitor.OnDisconnected += (obj, arg) => Dispatcher.Invoke(
                                new EventHandler<NetworkConnectivityChangedEventArgs>(OnSocektChanged),
                                obj, arg);
        }

        private void OnSocektChanged(object sender, NetworkConnectivityChangedEventArgs e)
        {
            try
            {
                if (!e.IsConnected)
                {
                    if (colorimeter != null)
                    {
                        if (colorimeter is SpectrometerForYL30 yL30)
                        {
                            if (string.Compare(yL30.communication.IP, e.Host, true) == 0)
                            {
                                TryDisconnect();
                            }
                        }
                        else if (colorimeter is TS8450 tS8450)
                        {
                            if (string.Compare(tS8450.DeviceName, e.Host, true) == 0)
                            {
                                TryDisconnect();
                            }
                        }
                        IsCanUseHaze = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.Message);
            }

        }

        /// <summary>
        /// 更新仪器相关UI
        /// </summary>
        /// <param name="type"></param>
        private void UpdateInstrumentUi(InstrumentType type)
        {
            if (type != InstrumentType.Unknown)
            {
                (colorimeter as ISystemSetting)?.RefreshCaches();
                if (colorimeter is IMeasurementNotification)
                {
                    ((IMeasurementNotification)colorimeter).MeasurementReceived += OnMeasureResultReceived;
                }

                Trace.TraceInformation("仪器成功连接到：" + colorimeter.DeviceName);

                SystemDefaultConfig.LatestConnection = colorimeter.DeviceName;

                var sc = (Colorimeter as ISystemSetting)?.SpecularComponent;
                if (sc != null)
                {

                    if (SpecularComponent != sc && sc != SpecularComponent.SCIAndSCE)
                    {
                        Dispatcher.Invoke(new Action(() => SpecularComponent = (SpecularComponent)sc));
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(() => SpecularComponent = SpecularComponent.SCI));
                    }
                }
            }

            Dispatcher.Invoke(new Action(NotifyInstrumentConfigChanged));
            Dispatcher.Invoke(new Action(UpdateConnectionStatus));
            Dispatcher.Invoke(new Action<InstrumentType>(UpdateToolsMenu), type);

            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateToolsMenu(InstrumentType instrumentType)
        {
#if INNER
            int anchorIndex = mainMenu.Items.IndexOf(ToolsAnchorMenuItem);
            var toolsMenuItem = (mainMenu.Items[anchorIndex + 1] as MenuItem);
            if (toolsMenuItem.Tag as string == "Tools")
            {
                mainMenu.Items.Remove(toolsMenuItem);
                toolsMenuItem = null;
            }
            var toolsMenuItemKey = instrumentType.ToString() + "ToolsMenuItem";
            toolsMenuItem = FindResource(toolsMenuItemKey) as MenuItem;
            if (toolsMenuItem != null)
            {
                mainMenu.Items.Insert(anchorIndex + 1, toolsMenuItem);
            }
#endif
        }

        private void OnSerialPortsChanged(object sender, PortsChangedArgs e)
        {
            if (SystemDefaultConfig.ConnectionMethod == ConnectionMethod.ByBluettooth_4)
            {
                return;
            }
            if (e.EventType == EventType.Insertion)
            {
                Trace.TraceInformation("串口插入：{0}", e.SerialPorts[0].PortName);
                if (colorimeter == null || !IsByBluetooth && !colorimeter.IsOpen)
                {
                    if (SerialPortDevice.PortDescriptions.Contains((e.SerialPorts[0].Description)))
                    {
                        if (!_session.Instrument.IsWorkerBusy)
                        {
                            _session.Instrument.StartConnection(e.SerialPorts[0].PortName);
                        }
                    }
                    else if (e.SerialPorts[0].PortName == AppSettings.Connection_PortName)
                    {
                        try
                        {
                            ConnectToCR400(e.SerialPorts[0].PortName, AppSettings.Connection_BaudRate
                              , (System.IO.Ports.Parity)AppSettings.Connection_Parity, (StopBits)AppSettings.Connection_StopBits);
                        }
                        catch
                        {
                            colorimeter = null;
                        }
                    }
                }
            }
            else if (e.EventType == EventType.Removal)
            {
                Trace.TraceInformation("串口拔出：{0}", e.SerialPorts[0].PortName);
                if (colorimeter != null)
                {
                    foreach (var port in e.SerialPorts)
                    {
                        if (string.Compare(port.PortName, colorimeter.DeviceName, true) == 0)
                        {
                            TryDisconnect();
                        }
                    }

                    IsCanUseHaze = false;
                }
            }
        }

        private bool IsByBluetooth
        {
            //get { return colorimeter.CommunicationDevice is BluetoothDevice;  }
            get
            {
                return (colorimeter is BleSpectrophotometer || (colorimeter as GratingSpectrometer.Spectrometer)?.CommunicationDevice is BluetoothDevice);
            }
        }

        private void TryDisconnect()
        {
            _session.Instrument.Disconnect();
        }

        private void ReleaseWhiteboardSpectrum()
        {
            WSCI = null;
            WSCE = null;
        }

        // XAML CommandBinding 入口保留；编排已迁 ConnectCommandHandler（阶段4-T01）
        private void ConnectExecuted(object sender, ExecutedRoutedEventArgs e)
            => _connectCommands.ExecuteConnect(sender, e);

        private void ConnectCanExected(object sender, CanExecuteRoutedEventArgs e)
            => _connectCommands.CanExecuteConnect(sender, e);

        private void DisconnectExecuted(object sender, ExecutedRoutedEventArgs e)
            => _connectCommands.ExecuteDisconnect(sender, e);

        private void InstrumentOperationCanExecuted(object sender, CanExecuteRoutedEventArgs e)
            => _connectCommands.CanExecuteWhenConnected(sender, e);

        // 测量菜单/状态栏由 MeasureStateMachine 驱动，经 IMeasurementHost.SetMeasureUiActive 推送（3-T06）
        private void UpdateMeasureUi(bool isBeginMeasure)
        {
            mainMenu.IsEnabled = !isBeginMeasure;
            _toolBarTray.IsEnabled = !isBeginMeasure;
            StatusText = isBeginMeasure ? S._("Measuring") : S._("Ready");
        }

        private void MeasureStandardExecuted(object sender, ExecutedRoutedEventArgs e)
            => _measureCommands.ExecuteMeasureStandard(sender, e);

        #region 标样调用的方法
        private int? CalculateEightDegreesGloss(ColorData sCI, ColorData sCE)
        {
            if (WSCI == null || WSCE == null || sCI == null || sCE == null)
                return null;
            if (WSCI.Spectrum == null || WSCE.Spectrum == null || sCI.Spectrum == null || sCE.Spectrum == null)
                return null;
            return ColorAlgorithm.Formula.CalculateEightDegreesGloss(sCI.Spectrum, sCE.Spectrum, WSCI.Spectrum, WSCE.Spectrum);
            // return AlgorithmExtension.clralgo_8_gloss_data(sCI.Spectrum, sCE.Spectrum, WSCI.Spectrum, WSCE.Spectrum);
        }

        // 测量异常后是否重校准由 InstrumentCalibrationService 编排（阶段1-T23）
        internal bool IsMeasurementAbnormalException(Exception ex) => _calibration.TryHandleMeasurementAbnormal(ex);

        // 在标样中查找最接近的标样 → 已迁入 MeasurementService.FindNearestStandard

        /// <summary>
        /// 试样测量入库并刷新导航树与图表（FinishMeasureOnUiAsync / 仪器通知共用）。
        /// 平均测量对话框也走此路径：仅设 CurrentTest 时若未切到该标样视图，TestDataGrid 无此项，选中无效。
        /// </summary>
        internal void ApplyMeasuredTestResult(TestSpecimen test, StandardSpecimen std)
        {
            // 仪器测量入库经 Session 协调，避免 Instrument partial 直调 Data.AddTest（阶段2-T02）
            _session.AddTest(test, std);
            NavigateTreeAfterTestMeasured(std);
            CurrentTest = test;
            RefreshCharts();
        }

        /// <summary>
        /// 标样已 Commit 后切视图并选中；AddStandard 无选中参数，与 ApplyMeasuredTestResult 对称供对话框调用。
        /// </summary>
        internal void ApplyMeasuredStandardResult(StandardSpecimen standard)
        {
            if (standard == null)
            {
                return;
            }

            CurrentStandard = standard;
            ShowSingleStandard(standard);
            StandardGroupTreeItem.IsExpanded = true;
            _treePresenter?.ExpandStandard(standard);
            RefreshCharts();
        }

        private void NavigateTreeAfterTestMeasured(StandardSpecimen std)
        {
            if (SingleStandardInShowing != std || (std == null && !IsShowOrphanTests))
            {
                if (std != null)
                {
                    ShowSingleStandard(std);
                    StandardGroupTreeItem.IsExpanded = true;
                    var treeItem = FindTreeViewItem(StandardGroupTreeItem.Items, std);
                    treeItem.IsExpanded = true;
                }
                else
                {
                    ShowOrphanTests();
                    OrphanTestsTreeItem.IsExpanded = true;
                }
            }
        }

        private double MeasureClarity()
        {
            if (MessageBox.Show(S._("MeasureClarityPrompt"), S._("ClarityMeasurement"), MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                try
                {
                    return colorimeter.MeasureClarity();
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"{S._("MeasureClarityFailed")}{exception.Message}");
                }
            }
            return double.NaN;
        }
        #endregion


        private void MeasureTrialExecuted(object sender, ExecutedRoutedEventArgs e)
            => _measureCommands.ExecuteMeasureTrial(sender, e);

        private void InstrumentStatusExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (colorimeter?.IsOpen == true)
            {
                var dlg = new DeviceInfoDlg(colorimeter);
                dlg.Owner = this;
                dlg.ShowDialog();
            }
        }

        private void InstrumentDataManagementExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (Colorimeter is IRecordManagement)
            {
                InstrumentDataManagementDialog dlg = new InstrumentDataManagementDialog()
                {
                    Colorimeter = (IRecordManagement)colorimeter,
                    Owner = this
                };
                dlg.ShowDialog();
            }
            else
            {
                MessageBox.Show(_("TheOperationIsNotSupported"));
            }
        }

        private void InstrumentConfigurationExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Window dlg = null;
                if (colorimeter is GratingSpectrometer.Spectrometer)
                {
                    dlg = new GratingSpectrometer.WhiteBoardParameterSettingDialog(colorimeter as GratingSpectrometer.Spectrometer);
                }
                else if (colorimeter is LedSpectrometer.Spectrometer)
                {
                    dlg = new LedSpectrometer.WhiteBoardParameterDialog(colorimeter as LedSpectrometer.Spectrometer);
                }
                else if (colorimeter is ThreeNH.YRColorimeter.Colorimeter)
                {
                    dlg = new YRWhiteBoardSettingDlg(colorimeter as ThreeNH.YRColorimeter.Colorimeter);
                }
                else if (colorimeter is BTS.Spectrometer)
                {
                    dlg = new YS60.WhiteBoardParameterSettingDialog(colorimeter as BTS.Spectrometer);
                }
                else if (colorimeter is PTS.Spectrometer ptsSpectrometer)
                {
                    //TS7700系列和TS7010系列中的7030和7036可以设置白板参数
                    var productInfo = ptsSpectrometer.GetProductInfo();
                    var internalModel = productInfo.InternalModel;
                    if (internalModel == ProductType.TS7700)
                        dlg = new PTS70.WhiteBoardSettingDialog(ptsSpectrometer);
                    else if (internalModel == ProductType.TS7010)
                    {
                        var deviceType = (InternalModelTypeOf7010)productInfo.DeviceType;
                        if (deviceType == InternalModelTypeOf7010.TS7030 || deviceType == InternalModelTypeOf7010.TS7036)
                            dlg = new PTS70.WhiteBoardSettingDialog(ptsSpectrometer);
                    }
                }
                else if (colorimeter is TS8450)
                {
                    dlg = new YS60.WhiteBoardParameterSettingDialog(colorimeter as TS8450);
                }

                if (dlg == null)
                {
                    MessageBox.Show(_("TheOperationIsNotSupported"));
                    return;
                }
                dlg.Owner = this;
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        
        private void DoCalibrationExecuted(object sender, ExecutedRoutedEventArgs e) => DoCalibration();

        private bool DoCalibration() => _calibration.RunCalibration();

      
        private void WriteStandardToInstrumentExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(colorimeter is IRecordManagement))
            {
                MessageBox.Show(_("TheOperationIsNotSupported"));
                return;
            }

            var standards = GetSelectedStandards(e.Source);
            if (standards == null || standards.Length == 0)
            {
                MessageBox.Show(_("PleaseSelectedAStandard"));
                return;
            }

            StatusText = _("WritingStandards");
            mainMenu.IsEnabled = false;
            _toolBarTray.IsEnabled = false;

            //            WriteStandardsAsync(standards);

            Task.Factory.StartNew(() =>
            {
                Exception exception = null;
                try
                {
                    (colorimeter as IRecordManagement)?.DownloadStandards(standards);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Dispatcher.BeginInvoke(new Action((() =>
                {
                    mainMenu.IsEnabled = true;
                    _toolBarTray.IsEnabled = true;
                    StatusText = _("Ready");
                    if (exception != null)
                    {
                        MessageBox.Show(exception.Message);
                    }
                    else
                    {
                        MessageBox.Show(_("AllStandardsWritten"));
                    }
                })));
            });
        }

        private void AdventureMeasureExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MeasurementDialog dlg = new UI.MeasurementDialog(this);
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void StandardAdventureMeasureExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MeasurementDialog dlg = new UI.MeasurementDialog(this);
            dlg.Owner = this;
            dlg.IsStandardMeasurement = true;
            dlg.ShowDialog();
        }

        private void TestAdventureMeasureExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MeasurementDialog dlg = new UI.MeasurementDialog(this);
            dlg.Owner = this;
            dlg.IsStandardMeasurement = false;
            dlg.ShowDialog();
        }

        public static bool IsAnyModalOpened()
        {
            return Application.Current.Windows.OfType<Window>().Any(IsModal);
        }

        public static bool IsModal(Window window)
        {
            var fieldInfo = typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic);
            return fieldInfo != null && (bool)fieldInfo.GetValue(window);
        }

        private async void DealMeasureResultReceivedEvent(object sender, MeasurementNotificationArgs e)
        {
            //收到仪器关闭命令
            if (e == null)
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateConnectionStatus();
                });
                await Task.Delay(1000);
                ReStartSearchBluetooth();
                return;
            }
            if (e != null && e.Result.Name == "JsonClosed")
            {
                Dispatcher.Invoke(() =>
                {
                    CloseInstrument();
                });
                await Task.Delay(1000);
                ReStartSearchBluetooth();
                return;
            }
            if (IsAnyModalOpened())
                return;
            //雾度测量不接收仪器手动测量数据
            if (IsHazeEnabled) return;

            if (e.Result is StandardSpecimen standard)
            {
                standard.Gloss8 = CalculateEightDegreesGloss(standard.SCI, standard.SCE);
                _session.AddStandard(standard);
                CurrentStandard = standard;
                ShowSingleStandard(CurrentStandard);
            }
            else
            {
                TestSpecimen test = new TestSpecimen(e.Result);
                test.Gloss8 = CalculateEightDegreesGloss(test.SCI, test.SCE);
                StandardSpecimen std = _session.Measurement.GetBestStandard(test);
                ApplyMeasuredTestResult(test, std);
            }
        }

        private void OnMeasureResultReceived(object sender, MeasurementNotificationArgs e)
        {
            Dispatcher.Invoke(new EventHandler<MeasurementNotificationArgs>(DealMeasureResultReceivedEvent),
                sender, e);
        }

        private void OnInstrumentTypeMenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                foreach (MenuItem item in menuItem.Items)
                {
                    switch ((string)item.Tag)
                    {
                        case "0":
                            item.IsChecked = colorimeter is LedSpectrometer.Spectrometer;
                            break;

                        case "1":
                            item.IsChecked = colorimeter is GratingSpectrometer.Spectrometer;
                            break;
                    }
                }
            }
        }

        private void OnConnectionMethodMenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                foreach (MenuItem item in menuItem.Items)
                {
                    switch ((string)item.Tag)
                    {
                        case "byUsb":
                            item.IsChecked = SystemDefaultConfig.ConnectionMethod == ConnectionMethod.ByUSB;
                            break;

                        case "byBluetooth_4":
                            item.IsChecked = SystemDefaultConfig.ConnectionMethod == ConnectionMethod.ByBluettooth_4;
                            break;

                        case "byBluetooth_5":
                            item.IsChecked = SystemDefaultConfig.ConnectionMethod == ConnectionMethod.ByBluettooth_5;
                            break;
                        case "byYL30":
                            item.IsChecked = SystemDefaultConfig.ConnectionMethod == ConnectionMethod.ByYL30Device;
                            break;

                        case "serialPorts":
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// 连接到串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnectToSerialPortOpened(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem))
                return;

            Mouse.OverrideCursor = Cursors.Wait;
            menuItem.Items.Clear();
            foreach (var info in SerialPortService.SerialPorts)
            {
                MenuItem serialPortItem = new MenuItem();
                serialPortItem.IsCheckable = true;

                if (string.IsNullOrWhiteSpace(info.Description))
                    serialPortItem.Header = info.PortName;
                else
                    serialPortItem.Header = string.Format("{0} ({1})", info.Description, info.PortName);

                serialPortItem.Tag = info.PortName;
                serialPortItem.IsChecked = SystemDefaultConfig.ConnectionMethod == ConnectionMethod.BySpecificalDevice
                    && string.Compare(info.PortName, SystemDefaultConfig.LatestConnection, true) == 0;
                serialPortItem.Click += SerialPortItemOnClick;
                menuItem.Items.Add(serialPortItem);
            }

            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void SerialPortItemOnClick(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string portName = item.Tag as string;

            if (colorimeter != null && colorimeter.IsOpen)
            {
                MessageBox.Show(_("PleaseDisconnect"));
                return;
            }
            var info = new SerialPortInformation(portName);
            if (info != null && InstrumentConnectionService.IsBluetoothDonglePort(info.Description))
                SystemDefaultConfig.ConnectionMethod = ConnectionMethod.ByBluettooth_5;
            else
                SystemDefaultConfig.ConnectionMethod = ConnectionMethod.ByUSB;
            if (!_session.Instrument.IsWorkerBusy)
            {
                _session.Instrument.StartConnection(portName);
            }
        }

        private void OnByUsbChecked(object sender, RoutedEventArgs e)
        {
            if (colorimeter != null && colorimeter.IsOpen)
            {
                MessageBox.Show(_("PleaseDisconnect"));
                return;
            }

            SystemDefaultConfig.ConnectionMethod = ConnectionMethod.ByUSB;
            if (!_session.Instrument.IsWorkerBusy)
            {
                _session.Instrument.StartConnection();
            }
        }

        private void OnByBluetoothChecked(object sender, RoutedEventArgs e)
        {
            if (colorimeter != null && colorimeter.IsOpen)
            {
                MessageBox.Show(_("PleaseDisconnect"));
                return;
            }

            SystemDefaultConfig.ConnectionMethod = ConnectionMethod.ByBluettooth_4;
            if (!_session.Instrument.IsWorkerBusy)
            {
                _session.Instrument.StartConnection();
            }
        }

        private async void OnByBluetooth5Checked(object sender, RoutedEventArgs e)
        {
            if (_session.Instrument.IsWorkerBusy)
            {
                if (IsConnecting)
                {
                    MessageBox.Show(_("It's Connecting"));
                    return;
                }
                if (_session.Instrument.IsSearching)
                {
                    this.StopSearch();
                    ClosePortAndDongle();
                }
                _session.Instrument.SignalDeviceSelectionDone();
                await Task.Delay(500);
            }
            if (colorimeter != null && colorimeter.IsOpen)
            {
                MessageBox.Show(_("PleaseDisconnect"));
                return;
            }
            CanOpenBluetoothWindow = true;
            SystemDefaultConfig.ConnectionMethod = ConnectionMethod.ByBluettooth_5;
            if (!_session.Instrument.IsWorkerBusy)
            {
                _session.Instrument.StartConnection();
            }
        }

        private async void OnByYL30Checked(object sender, RoutedEventArgs e)
        {
            if (_session.Instrument.IsWorkerBusy)
            {
                if (IsConnecting)
                {
                    MessageBox.Show(_("It's Connecting"));
                    return;
                }

                _session.Instrument.SignalDeviceSelectionDone();
                await Task.Delay(500);
            }
            if (colorimeter != null && colorimeter.IsOpen)
            {
                MessageBox.Show(_("PleaseDisconnect"));
                return;
            }
            SystemDefaultConfig.ConnectionMethod = ConnectionMethod.ByYL30Device;
            if (!_session.Instrument.IsWorkerBusy)
            {
                _session.Instrument.StartConnection();
            }
        }
        private void NotifyInstrumentConfigChanged()
        {
            NotifyPropertyChanged("IsInstrumentSpecularComponentEditable");
            NotifyPropertyChanged("IsInstrumentUVEditable");
            NotifyPropertyChanged("InstrumentMeasurementCaliber");
            NotifyPropertyChanged("InstrumentUVFlag");
            NotifyPropertyChanged("InstrumentSpecularComponent");
        }

    

        public SpecularComponent InstrumentSpecularComponent
        {
            get { return Colorimeter is ISystemSetting ? ((ISystemSetting)Colorimeter).SpecularComponent : SpecularComponent.SCIAndSCE; }
        }

        public MeasurementCaliber InstrumentMeasurementCaliber
        {
            get { return Colorimeter is ISystemSetting ? ((ISystemSetting)Colorimeter).Aperture : MeasurementCaliber.Unknown; }
        }

        public UVFlag InstrumentUVFlag
        {
            get { return Colorimeter is ISystemSetting ? ((ISystemSetting)Colorimeter).UvMode : UVFlag.None; }
        }

        private void UpdateConnectionStatus()
        {
            if (Colorimeter != null && Colorimeter.IsOpen)
            {
#if true
                (Colorimeter as ISystemSetting)?.RefreshCaches();
                ConnectionStatusLabel.Content =
                    $"{_("ConnectedTo")}{Colorimeter.Model}<{Colorimeter.DeviceName}>";

#else
                ConnectionStatusLabel.Content = //Text("Connected") + ": " + Colorimeter.PortName;
                    string.Format("{0} {1}", _("ConnectedTo"),
                        "   Device");
#endif
                ConnectionStatusLabel.Foreground = Brushes.Green;
            }
            else
            {
                ConnectionStatusLabel.Content = _("Disconnected");
                ConnectionStatusLabel.Foreground = Brushes.Red;
            }
        }
        private void CloseInstrument()
        {

            colorimeter = null;
            NotifyPropertyChanged(nameof(Colorimeter));
            UpdateConnectionStatus();


        }
        private void AutoSelectStandardSettingExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AutoSelectStandardSettingDlg dlg = new AutoSelectStandardSettingDlg();
            dlg.Owner = this;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
            }
        }

        public bool IsHazeEnabled
        {
            get { return _isHazeEnabled; }
            set
            {
                if (value != _isHazeEnabled)
                {
                    _isHazeEnabled = value;
                    NotifyPropertyChanged(nameof(IsHazeEnabled));
                }
            }
        }

        private void OnInstrumentMeasurementConfigs(object sender, ExecutedRoutedEventArgs e)
        {
            if (Colorimeter is ISystemSetting setting)
            {
                setting.RefreshCaches();//added by Liguanghai,每次设置参数前先刷新一下参数
                MeasurementConfigsDialog dialog = new MeasurementConfigsDialog(Colorimeter as ISystemSetting);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
            else
            {
                MessageBox.Show(_("TheOperationIsNotSupported"));
            }
        }

        private void OnWriteToInstrumentLibrary(object sender, ExecutedRoutedEventArgs e)
            => _library.WriteToInstrumentLibraryAsync(GetSelectedStandards(e.Source));

        private void OnClearInstrumentLibrary(object sender, ExecutedRoutedEventArgs e)
            => _library.ClearInstrumentLibrary();

        private void OnUpdateInstrumentLibrary(object sender, ExecutedRoutedEventArgs e)
            => _library.UpdateInstrumentLibrary();

        private void OnReadInstrumentLibrary(object sender, ExecutedRoutedEventArgs e)
            => _library.ReadFromInstrumentLibrary();

        private void InstrumentLibraryOperationCanExecuted(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = colorimeter?.IsOpen == true && _library.SupportsInstrumentTargetLibrary();

        public void ConnectToCR400(string portName, int baudRate, System.IO.Ports.Parity parity, StopBits stopBits)
        {
            try
            {
                CR400.Instrument cr400 = new CR400.Instrument();
                cr400.Open(new SerialPort(portName, baudRate, parity, 8, stopBits));
                colorimeter = cr400;
                UpdateConnectionStatus();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool ClarityMeasurementValid => (colorimeter is ThreeNH.YHComm.USBComm usbcomm && usbcomm.IsClarityValid) || (colorimeter is ThreeNH.YHComm.BleComm blecomm && blecomm.IsClarityValid);

        public RelayCommand ClarityCalibrationCommand => new RelayCommand(x =>
        {
            var loading = new Loading("Calibrating...");
            loading.BackgroundWork += (message, progress, args) =>
            {
                try
                {
                    // 清晰度仪器校准调用下沉 Service，Loading 仍留在 MainWindow（阶段1-T23）
                    _calibration.CalibrateClarity(colorimeter);
                }
                catch (Exception ex)
                {
                    progress(ex);
                }
            };
            loading.BackgroundWorkerCompleted += (sender, args) =>
            {
                if (args.Result is Exception ex)
                {
                    MessageBox.Show($"Calibrated failed:{ex.Message}");
                }
            };
            loading.Start();
        }, y => ClarityMeasurementValid);

        private void StandardSampleReportCanExexuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CurrentStandard != null;
        }

        private void OnStandardSampleReportExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PrintSampleReportToWord(CurrentStandard);
        }

        private void TestSampleReportCanExexuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CurrentTest != null;
        }

        private void OnTestSampleReportExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PrintSampleReportToWord(CurrentTest);
        }

        private void PrintSampleReportToWord(Specimen specimen)
        {
            WordReportBase report = new WordSampleReport(specimen);
            report.IsExportChromaticityChart = Database.ReportSetting.IsExportChromaticityChart;

            report.ReportSetting = Database.ReportSetting;
            report.Standard = SingleStandardInShowing;
            report.ChromaticitySetting = ChromaticitySetting;
            report.SpecularComponent = SpecularComponent;
            report.Formula = Formula;
            Formula ref1, ref2;
            Database.GetReferenceFormula(Formula, out ref1, out ref2);
            report.Tolerance = Database.GetTolerance(report.Standard, new ObserverIlluminantPair(Observer, Illuminant));
            report.InstrumentModel = (Colorimeter as IDeviceInfo)?.Model ?? "";//190923lilin 打印文件最后的仪器型号
            report.VisibleDeltaPlots = DefaultVisibleDeltaPlots;
            report.ToleranceSetting = Database.GetToleranceSetting(SingleStandardInShowing);
            report.ObserverAndIlluminantSetting = Database.ObserverAndIlluminantSetting;
            report.ReferenceFormula1 = ref1;
            report.ReferenceFormula2 = ref2;

            report.GenerateReport();
        }
    }
}