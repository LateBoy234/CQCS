using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Management;
using System.Threading;

namespace ThreeNH.PortService
{
    public static class SerialPortService
    {
        private static Dictionary<string, SerialPortInformation> _serialPorts;
        private static ManagementEventWatcher arrival;
        private static ManagementEventWatcher removal;

        private static readonly object _lockObj = new object();
        private static readonly object _portsCacheLock = new object();
        private static Timer _portsChangeDebounceTimer;
        private static Timer _pollTimer;
        private static EventType _pendingPortsEventType;
        private static bool _isRunning = false;
        // net8 的 System.Management 依赖 Framework 目录下 wminet_utils.dll，客户机 .NET FX 过旧时会抛 PlatformNotSupportedException
        private static bool _wmiAvailable = true;
        private const int PollIntervalMs = 2000;

        public static event EventHandler<PortsChangedArgs> PortsChanged;
        public static bool IsWmiAvailable => _wmiAvailable;
        public static readonly string[] PortDescriptions =
        {
                "SCI USB2Serial",
                "STMicroelectronics Virtual COM Port",
                "USB-SERIAL CH340",
        };

        static SerialPortService()
        {
            _serialPorts = QueryAvailableSerialPorts();
            if (_wmiAvailable)
                TryMonitorDeviceChanges();
            else
                StartPollingMonitor();
        }

        public static SerialPortInformation[] SerialPorts
        {
            get
            {
                lock (_portsCacheLock)
                {
                    return _serialPorts.Values.ToArray();
                }
            }
        }

        public static List<string> SerialPortNames
        {
            get { return _serialPorts.Keys.ToList(); }
        }

        /// <summary>
        /// 强制用当前系统串口列表刷新缓存。设备短暂掉线/重枚举后，仅靠 PnP 事件更新的缓存可能尚未包含端口，
        /// 重连前应调用此方法，避免 <see cref="SerialPorts"/> 仍为旧快照导致连接失败。
        /// </summary>
        public static void RefreshSerialPortsCache()
        {
            var current = QueryAvailableSerialPorts();
            lock (_portsCacheLock)
            {
                _serialPorts = current;
            }
        }

        /// <summary>
        /// If this method isn't called, an InvalidComObjectException will be thrown (like below):
        /// System.Runtime.InteropServices.InvalidComObjectException was unhandled
        /// Message=COM object that has been separated from its underlying RCW cannot be used.
        /// Source=mscorlib
        /// StackTrace:
        ///     at System.StubHelpers.StubHelpers.StubRegisterRCW(Object pThis, IntPtr pThread)
        ///     at System.Management.IWbemServices.CancelAsyncCall_(IWbemObjectSink pSink)
        ///     at System.Management.SinkForEventQuery.Cancel()
        ///     at System.Management.ManagementEventWatcher.Stop()
        ///     at System.Management.ManagementEventWatcher.Finalize()
        /// InnerException:
        /// </summary>
        public static void CleanUp()
        {
            lock (_lockObj) // 加锁保证线程安全
            {
                if (!_isRunning) return;

                // 1. 空值检查 + Stop
                arrival?.Stop();
                removal?.Stop();

                // 2. 解除事件绑定（避免内存泄漏）
                if (arrival != null)
                {
                    arrival.EventArrived -= (sender, args) => RaisePortsChangedIfNecessary(EventType.Insertion);
                }
                if (removal != null)
                {
                    removal.EventArrived -= (sender, args) => RaisePortsChangedIfNecessary(EventType.Removal);
                }

                // 3. 释放COM对象（核心：解决InvalidComObjectException）
                arrival?.Dispose();
                removal?.Dispose();

                // 4. 重置状态
                arrival = null;
                removal = null;
                _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _pollTimer?.Dispose();
                _pollTimer = null;
                _isRunning = false;
            }
        }

        // Start listening for events
        public static void Start()
        {
            lock (_lockObj)
            {
                if (_isRunning) return; // 避免重复启动

                if (_wmiAvailable)
                {
                    arrival?.Start();
                    removal?.Start();
                }
                else
                {
                    _pollTimer?.Change(PollIntervalMs, PollIntervalMs);
                }
                _isRunning = true;
            }
        }

        private static void TryMonitorDeviceChanges()
        {
            try
            {
                var deviceArrivalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType=2");
                var deviceRemovalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType=3");

                arrival = new ManagementEventWatcher(deviceArrivalQuery);
                removal = new ManagementEventWatcher(deviceRemovalQuery);

                arrival.EventArrived += (sender, args) => RaisePortsChangedIfNecessary(EventType.Insertion);
                removal.EventArrived += (sender, args) => RaisePortsChangedIfNecessary(EventType.Removal);
            }
            catch
            {
                _wmiAvailable = false;
                arrival?.Dispose();
                removal?.Dispose();
                arrival = null;
                removal = null;
                StartPollingMonitor();
            }
        }

        private static void StartPollingMonitor()
        {
            if (_pollTimer != null)
                return;
            _pollTimer = new Timer(_ => PollPortChanges(), null, Timeout.Infinite, Timeout.Infinite);
        }

        private static void PollPortChanges()
        {
            var current = GetAvailableSerialPortViaPortNames();
            Dictionary<string, SerialPortInformation> old;
            lock (_portsCacheLock)
            {
                old = _serialPorts;
            }

            var added = current.Keys.Except(old.Keys).ToList();
            var removed = old.Keys.Except(current.Keys).ToList();
            if (!added.Any() && !removed.Any())
                return;

            lock (_portsCacheLock)
            {
                _serialPorts = current;
            }

            if (added.Any())
                PortsChanged.Raise(null, new PortsChangedArgs(EventType.Insertion, added.Select(k => current[k]).ToArray()));
            if (removed.Any())
                PortsChanged.Raise(null, new PortsChangedArgs(EventType.Removal, removed.Select(k => old[k]).ToArray()));
        }

        private static void RaisePortsChangedIfNecessary(EventType eventType)
        {
            lock (_portsCacheLock)
            {
                _pendingPortsEventType = eventType;
                if (_portsChangeDebounceTimer == null)
                {
                    _portsChangeDebounceTimer = new Timer(_ => FlushPortsChanged(), null,
                        Timeout.Infinite, Timeout.Infinite);
                }
                _portsChangeDebounceTimer.Change(500, Timeout.Infinite);
            }
        }

        private static void FlushPortsChanged()
        {
            EventType eventType;
            Dictionary<string, SerialPortInformation> old;
            lock (_portsCacheLock)
            {
                eventType = _pendingPortsEventType;
                old = _serialPorts;
            }

            var current = QueryAvailableSerialPorts();
            var diffKeys = eventType == EventType.Insertion
                ? current.Keys.Except(old.Keys)
                : old.Keys.Except(current.Keys);

            if (!diffKeys.Any())
            {
                lock (_portsCacheLock)
                {
                    _serialPorts = current;
                }
                return;
            }

            var diffInfos = diffKeys.Select(k => eventType == EventType.Insertion ? current[k] : old[k]).ToArray();
            lock (_portsCacheLock)
            {
                _serialPorts = current;
            }
            PortsChanged.Raise(null, new PortsChangedArgs(eventType, diffInfos));
        }
        public static SerialPortInformation[] SuitableSerialPorts
        {
            get
            {
                try
                {
                    return QueryAvailableSerialPorts()?.Select(p => p.Value)?.Where(
                         o => string.Compare(o.Description, PortDescriptions[0]) == 0
                            || string.Compare(o.Description, PortDescriptions[1]) == 0
                            || string.Compare(o.Description, PortDescriptions[2]) == 0

                    )?.ToArray();
                }
                catch
                {
                    return null;
                }
            }
        }
        public static Dictionary<string, SerialPortInformation> GetAvailableSerialPort()
        {
            return QueryAvailableSerialPorts();
        }

        private static Dictionary<string, SerialPortInformation> QueryAvailableSerialPorts()
        {
            if (!_wmiAvailable)
                return GetAvailableSerialPortViaPortNames();

            try
            {
                return GetAvailableSerialPortViaWmi();
            }
            catch
            {
                _wmiAvailable = false;
                return GetAvailableSerialPortViaPortNames();
            }
        }

        private static Dictionary<string, SerialPortInformation> GetAvailableSerialPortViaPortNames()
        {
            var serialPorts = new Dictionary<string, SerialPortInformation>();
            foreach (var name in SerialPort.GetPortNames())
            {
                if (string.IsNullOrEmpty(name))
                    continue;

                serialPorts[name] = new SerialPortInformation
                {
                    PortName = name,
                    Description = string.Empty,
                    Caption = name,
                    DeviceID = string.Empty
                };
            }
            return serialPorts;
        }

        private static Dictionary<string, SerialPortInformation> GetAvailableSerialPortViaWmi()
        {
            Dictionary<string, SerialPortInformation> serialPorts =
                new Dictionary<string, SerialPortInformation>();

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\cimv2",
                "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");
            foreach (ManagementObject queryObj in searcher.Get())
            {

                string portName = queryObj["Caption"].ToString();
                if (portName == null)
                {
                    continue;
                }
                if (portName.Contains("(COM"))
                {
                    int indexOfCom = portName.IndexOf("(COM");
                    portName = portName.Substring(indexOfCom + 1, portName.Length - indexOfCom - 2);
                }

                SerialPortInformation info = new SerialPortInformation()
                {
                    PortName = portName,
                    Description = queryObj["Description"].ToString(),
                    Caption = queryObj["Caption"].ToString(),
                    DeviceID = queryObj["DeviceID"].ToString(),
                };


                serialPorts[info.PortName] = info;
            }
            return serialPorts;
        }

        private static void Raise<T>(this EventHandler<T> handler, object sender, T e)
            where T : EventArgs
        {
            if (handler != null)
                handler(sender, e);
        }
    }

    public enum EventType
    {
        Insertion,
        Removal
    }

    public class PortsChangedArgs : EventArgs
    {
        private readonly EventType _eventType;
        private readonly SerialPortInformation[] _serialPorts;

        public PortsChangedArgs(EventType eventType, SerialPortInformation[] serialPorts)
        {
            _eventType = eventType;
            _serialPorts = serialPorts;
        }

        /// <summary>
        /// 如果EventType是Insertion，则为插入的新串口，
        /// 如果是Removal，则是移除的串口
        /// </summary>
        public SerialPortInformation[] SerialPorts
        {
            get { return _serialPorts; }
        }

        public EventType EventType
        {
            get { return _eventType; }
        }
    }
}
