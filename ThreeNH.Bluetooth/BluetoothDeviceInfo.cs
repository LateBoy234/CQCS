using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.Bluetooth
{
    public class BluetoothDeviceInfo
    {
        internal BluetoothApis.BluetoothDeviceInfo _device;

        internal BluetoothDeviceInfo(BluetoothApis.BluetoothDeviceInfo deviceInfo)
        {
            _device = deviceInfo;
        }

        public BluetoothDeviceInfo() { }

        public BluetoothAddress Address
        {
            get { return new BluetoothAddress(_device.Address); }
            set { _device.Address = value._bthaddr; }
        }
        public uint ClassofDevice
        {
            get { return _device.ulClassofDevice; }
            set { _device.ulClassofDevice = value; }
        }
        public bool Connected
        {
            get { return _device.fConnected != 0; }
            set { _device.fConnected = value ? 1 : 0; }
        }
        public bool Remembered
        {
            get { return _device.fRemembered != 0; }
            set { _device.fRemembered = value ? 1 : 0; }
        }
        public bool Authenticated
        {
            get { return _device.fAuthenticated != 0; }
            set { _device.fAuthenticated = value ? 1 : 0; }
        }
        public DateTime LastSeen
        {
            get
            {
                return new DateTime(_device.stLastSeen.Year, _device.stLastSeen.Month,
                    _device.stLastSeen.Day, _device.stLastSeen.Hour, _device.stLastSeen.Minute,
                    _device.stLastSeen.Second, _device.stLastSeen.Milliseconds);
            }
            set
            {
                _device.stLastSeen.Year = (ushort)value.Year;
                _device.stLastSeen.Month = (ushort)value.Month;
                _device.stLastSeen.Day = (ushort)value.Day;
                _device.stLastSeen.DayOfWeek = (ushort)value.DayOfWeek;
                _device.stLastSeen.Hour = (ushort)value.Hour;
                _device.stLastSeen.Minute = (ushort)value.Minute;
                _device.stLastSeen.Second = (ushort)value.Second;
                _device.stLastSeen.Milliseconds = (ushort)value.Millisecond;
            }
        }
        public DateTime LastUsed
        {
            get
            {
                return new DateTime(_device.stLastUsed.Year, _device.stLastUsed.Month,
                    _device.stLastUsed.Day, _device.stLastUsed.Hour, _device.stLastUsed.Minute,
                    _device.stLastUsed.Second, _device.stLastUsed.Milliseconds);
            }
            set
            {
                _device.stLastUsed.Year = (ushort)value.Year;
                _device.stLastUsed.Month = (ushort)value.Month;
                _device.stLastUsed.Day = (ushort)value.Day;
                _device.stLastUsed.DayOfWeek = (ushort)value.DayOfWeek;
                _device.stLastUsed.Hour = (ushort)value.Hour;
                _device.stLastUsed.Minute = (ushort)value.Minute;
                _device.stLastUsed.Second = (ushort)value.Second;
                _device.stLastUsed.Milliseconds = (ushort)value.Millisecond;
            }
        }
        public string Name
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var ch in _device.szName)
                {
                    if (ch == '\0')
                        break;
                    sb.Append(ch);
                }
                return sb.ToString();
            }
            set
            {
                for (int i = 0; i < value.Length; i++)
                    _device.szName[i] = value[i];
                _device.szName[value.Length] = '\0';
            }
        }
    }
}
