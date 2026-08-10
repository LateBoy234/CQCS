using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorQualityControl.Instrument;
using ThreeNH.ColorQualityControl.Refactoring.Contracts;
using ThreeNH.CommunicationBase;
using static ThreeNH.CommunicationBase.EhMc17AtInterface;
namespace ThreeNH.ColorQualityControl.Services
{
    /// <summary>
    /// 仪器连接状态与 BackgroundWorker 生命周期；DoConnectionWork 见 partial ConnectionWork。
    /// 连接布尔态已收口到 <see cref="ConnectionStateMachine"/>（阶段 3-T02）。
    /// </summary>
    internal sealed partial class InstrumentConnectionService
    {
        private BackgroundWorker _connectionWorker;
        // 后台线程 WaitOne、UI 线程 Set，与原先 MainWindow 字段行为一致
        private readonly ManualResetEvent _selectDeviceDoneEvent = new ManualResetEvent(false);
        private readonly ConnectionStateMachine _stateMachine = new ConnectionStateMachine();
        private IInstrumentConnectionHost _host;
        private ProgressChangedEventHandler _progressHandler;
        private RunWorkerCompletedEventHandler _completedHandler;

        private IInstrument _colorimeter;
        private string _deviceSN;

        /// <summary>连接状态变更时通知 MainWindow 刷新绑定（状态由 Service 持有，MainWindow 仅转发）。</summary>
        public event EventHandler ConnectionStateChanged;

        public InstrumentType InstrumentType { get; internal set; } = InstrumentType.Unknown;

        public InstrumentConnectionState ConnectionState => _stateMachine.State;

        public bool IsConnecting => _stateMachine.IsConnecting;

        public bool IsSearching => _stateMachine.IsSearching;

        public bool IsConnected => _stateMachine.IsConnected;

        public string DeviceSN => _deviceSN ?? string.Empty;

        // 蓝牙 5 dongle 扫描到的从设备列表，待用户选择；从 MainWindow 迁入以便连接域状态集中
        internal SlaveDevice[] BluetoothModuleSlaveDevices { get; set; }

        internal object SelectedDevice { get; set; }

        internal string SelectedPortName { get; set; }

        internal YL30Communication SelectedSpecialDevice { get; set; }

        internal IInstrument ColorimeterBacking
        {
            get => _colorimeter;
            set => _colorimeter = value;
        }

        public IInstrument Colorimeter => _colorimeter;

        public bool IsWorkerBusy => _connectionWorker?.IsBusy ?? false;

        public InstrumentConnectionService()
        {
            _stateMachine.StateChanged += (_, __) => ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AttachHost(
            IInstrumentConnectionHost host,
            ProgressChangedEventHandler progressHandler,
            RunWorkerCompletedEventHandler completedHandler)
        {
            _host = host;
            _progressHandler = progressHandler;
            _completedHandler = completedHandler;
        }

        public void EnsureWorker()
        {
            if (_connectionWorker != null)
            {
                return;
            }

            _connectionWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            _connectionWorker.DoWork += DoConnectionWork;
            _connectionWorker.ProgressChanged += _progressHandler;
            _connectionWorker.RunWorkerCompleted += _completedHandler;
        }

        public void StartConnection(object argument = null)
        {
            EnsureWorker();
            if (!_connectionWorker.IsBusy)
            {
                // Worker 启动即进入 Connecting，避免连接按钮在后台扫描期间仍可重复点击
                _stateMachine.Force(InstrumentConnectionState.Connecting);
                _connectionWorker.RunWorkerAsync(argument);
            }
        }

        /// <summary>菜单/工具栏「连接」入口（阶段1-T03）。</summary>
        public void ConnectFromUserCommand()
        {
            if (IsWorkerBusy)
            {
                return;
            }

            StartConnection(_host?.GetConnectionStartupArgument());
        }

        /// <summary>关闭仪器并清空 Service 内连接状态；UI 收尾由 Host 回调。</summary>
        public void Disconnect()
        {
            if (_colorimeter == null)
            {
                _stateMachine.Force(InstrumentConnectionState.Disconnected);
                return;
            }

            Trace.TraceInformation("尝试断开仪器连接");
            var instrument = _colorimeter;
            try
            {
                _host?.DetachMeasurementNotification(instrument);
                instrument.Close();
                Trace.TraceInformation("连接已断开");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("断开连接失败：{0}\n{1}", ex.Message, ex.StackTrace);
                _host?.ShowConnectionError(ex.Message);
            }
            finally
            {
                ColorimeterBacking = null;
                InstrumentType = InstrumentType.Unknown;
                SetDeviceSn(string.Empty);
                _stateMachine.Force(InstrumentConnectionState.Disconnected);
                _host?.OnInstrumentDisconnected();
            }
        }

        /// <summary>兼容旧 SetConnecting(bool)；true→Connecting，false→按是否已有仪器落到 Connected/Disconnected。</summary>
        internal void SetConnecting(bool connecting)
        {
            if (connecting)
            {
                if (!_stateMachine.IsConnecting)
                {
                    _stateMachine.Force(InstrumentConnectionState.Connecting);
                }

                return;
            }

            // Worker/自动连失败收尾：有仪器则 Connected，否则 Disconnected
            _stateMachine.Force(_colorimeter != null
                ? InstrumentConnectionState.Connected
                : InstrumentConnectionState.Disconnected);
        }

        internal void BeginSearch()
        {
            // 自动扫描从 WaitingForDeviceSelection / Connecting 进入 Searching
            if (!_stateMachine.TryTransition(InstrumentConnectionState.Searching))
            {
                _stateMachine.Force(InstrumentConnectionState.Searching);
            }
        }

        internal void EndSearch()
        {
            if (!_stateMachine.IsSearching)
            {
                return;
            }

            // 扫描结束仍可能在等 Signal；若 Worker 忙则回到 Waiting，否则断开
            if (IsWorkerBusy)
            {
                _stateMachine.TryTransition(InstrumentConnectionState.WaitingForDeviceSelection);
            }
            else
            {
                _stateMachine.Force(InstrumentConnectionState.Disconnected);
            }
        }

        internal void SetDeviceSn(string deviceSn)
        {
            deviceSn = deviceSn ?? string.Empty;
            if (string.Equals(_deviceSN, deviceSn, StringComparison.Ordinal))
            {
                return;
            }

            _deviceSN = deviceSn;
            ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>BackgroundWorker 完成（成功或失败）后统一复位：有仪器→Connected，否则 Disconnected。</summary>
        internal void OnConnectionWorkerCompleted()
        {
            _stateMachine.Force(_colorimeter != null
                ? InstrumentConnectionState.Connected
                : InstrumentConnectionState.Disconnected);
        }

        internal void ResetDeviceSelectionWait()
        {
            _selectDeviceDoneEvent.Reset();
            // 进入等选设备态，UI 可据此禁用重复连接
            if (!_stateMachine.TryTransition(InstrumentConnectionState.WaitingForDeviceSelection))
            {
                _stateMachine.Force(InstrumentConnectionState.WaitingForDeviceSelection);
            }
        }

        internal void SignalDeviceSelectionDone()
        {
            // 用户选完设备后回到 Connecting，Worker WaitOne 被唤醒继续探测
            if (_stateMachine.State == InstrumentConnectionState.WaitingForDeviceSelection
                || _stateMachine.State == InstrumentConnectionState.Searching)
            {
                _stateMachine.TryTransition(InstrumentConnectionState.Connecting);
            }

            _selectDeviceDoneEvent.Set();
        }

        internal void WaitForDeviceSelection() => _selectDeviceDoneEvent.WaitOne();

        /// <summary>蓝牙狗串口描述符，StartSearch 等 UI 流程仍需要此判断。</summary>
        internal static bool IsBluetoothDonglePort(string description)
        {
            return string.Compare(description, "USB-SERIAL CH340", StringComparison.CurrentCultureIgnoreCase) == 0;
        }
    }
}
