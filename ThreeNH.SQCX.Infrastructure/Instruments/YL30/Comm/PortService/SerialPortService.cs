using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30
{
    public static class SerialPortService
    {
        private static Dictionary<string, SerialPortInformation> _serialPorts;
        private static ManagementEventWatcher arrival;
        private static ManagementEventWatcher removal;
        public static event EventHandler<PortsChangedArgs> PortsChanged;
        public static readonly string[] PortDescriptions =
        {
           "Prolific USB-to-Serial Comm Port"
        };

        static SerialPortService()
        {
            _serialPorts = GetAvailableSerialPort();
            MonitorDeviceChanges();
        }

        public static SerialPortInformation[] SerialPorts
        {
            get { return _serialPorts.Values.ToArray(); }
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
            arrival.Stop();
            removal.Stop();
        }

        // Start listening for events
        public static void Start()
        {
            arrival.Start();
            removal.Start();
        }

        private static void MonitorDeviceChanges()
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
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        private static void RaisePortsChangedIfNecessary(EventType eventType)
        {
            lock (_serialPorts)
            {
                var availableSerialPorts = GetAvailableSerialPort();
                if (eventType == EventType.Insertion)
                {
                    var addedKeys = availableSerialPorts.Keys.Except(_serialPorts.Keys).ToArray();
                    if (addedKeys.Length > 0)
                    {
                        List<SerialPortInformation> added = new List<SerialPortInformation>();
                        foreach (var key in addedKeys)
                        {
                            added.Add(availableSerialPorts[key]);
                        }
                        _serialPorts = availableSerialPorts;
                        PortsChanged.Raise(null, new PortsChangedArgs(eventType, added.ToArray()));
                    }
                }
                else if (eventType == EventType.Removal)
                {
                    var removedKeys = _serialPorts.Keys.Except(availableSerialPorts.Keys).ToArray();
                    if (removedKeys.Length > 0)
                    {
                        List<SerialPortInformation> removed = new List<SerialPortInformation>();
                        foreach (var key in removedKeys)
                        {
                            removed.Add(_serialPorts[key]);
                        }
                        _serialPorts = availableSerialPorts;
                        PortsChanged.Raise(null, new PortsChangedArgs(eventType, removed.ToArray()));
                    }
                }
            }
        }
        public static SerialPortInformation[] SuitableSerialPorts
        {
            get
            {
                try
                {
                    return GetAvailableSerialPort()?.Select(p => p.Value)?.Where(
                         o => string.Compare(o.Description, PortDescriptions[0]) == 0
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
            Dictionary<string, SerialPortInformation> serialPorts =
                new Dictionary<string, SerialPortInformation>();

            //ManagementObjectSearcher searcher = new ManagementObjectSearcher(
            //    "root\\CIMV2", "SELECT * FROM WIN32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\cimv2",
                "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");
            foreach (ManagementObject queryObj in searcher.Get())
            {

                string portName = queryObj["Caption"].ToString();
                if (portName == null)
                {
                    continue;
                }
                //                string portName = queryObj["DeviceID"].ToString();
                if (portName.Contains("(COM"))
                {
                    // Get the index number where "(COM" starts in the string
                    int indexOfCom = portName.IndexOf("(COM");

                    // Set PortName to COMx based on the expression in the "selectedItem" string
                    // It automatically gets the correct length of the COMx expression to make sure 
                    // that also a COM10, COM11 and so on is working properly.
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
