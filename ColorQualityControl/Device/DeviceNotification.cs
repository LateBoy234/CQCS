using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Text;

namespace ThreeNH.ColorQualityControl.Device
{
    public enum DeviceNotificationDeviceType : Int32
    {
        OEM = 0x0000,               // OEM- or IHV-defined device type. This structure is a DEV_BROADCAST_OEM structure.
        Volume = 0x0002,            // Logical volume. This structure is a DEV_BROADCAST_VOLUME structure.
        Port = 0x0003,              // Port device (serial or parallel). This structure is a DEV_BROADCAST_PORT structure.
        DeviceInterface = 0x0005,   // Class of devices. This struct is a DEV_BROADCASE_DEVICEINTERFACE struct
        Handle = 0x0006,            // File system handle. This struct is a DEV_BROADCAST_HANDLE structure.
    }

    public static class DeviceNotification
    {
        public delegate void DeviceArrivalDelegate(DeviceNotificationDeviceType devicetype, string deviceName);
        public delegate void DeviceRemovedDelegate(DeviceNotificationDeviceType devicetype, string deviceName);
        public static event DeviceArrivalDelegate DeviceArrival;    // 设备连接
        public static event DeviceRemovedDelegate DeviceRemoved;    // 设备拔掉

        private const int
            DBT_CONFIGCHANGECANCELED = 0X0019,
            DBT_CONFIGCHANGED = 0X0018,
            DBT_CUSTOMEVENT = 0X8006,
            DBT_DEVICEARRIVAL = 0X8000,
            DBT_DEVICEQUERYREMOVE = 0X8001,
            DBT_DEVICEQUERYREMOVEFAILED = 0X8002,
            DBT_DEVICEREMOVEPENDING = 0X8003,
            DBT_DEVICEREMOVECOMPLETE = 0X8004,
            DBT_DEVICETYPESPECIFIC = 0X8005,
            DBT_DEVNODES_CHANGED = 0X0007,
            DBT_USERDEFINED = 0XFFFF;
        private static readonly Guid GuidDeviceInterfaceUSBDevice =
            new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB Devices
        private const int WmDeviceChange = 0x0219;   // device change event


        /// <summary>
        /// 注册USB事件
        /// </summary>
        /// <param name="window">主窗口</param>
        /// <returns>返回一个HDEVNOTIFY句柄，取消注册时用。</returns>
        public static IntPtr Register(Window window)
        {
            //// 注册USB事件
            //DevBroadcastDeviceInterface dbi = new DevBroadcastDeviceInterface
            //{
            //    DeviceType = DeviceNotificationDeviceType.DeviceInterface,
            //    Reserved = 0,
            //    classguid = GuidDeviceInterfaceUSBDevice,
            //    Name = ""
            //};
            //dbi.Size = Marshal.SizeOf(dbi);
            //IntPtr buffer = Marshal.AllocHGlobal(dbi.Size);
            //Marshal.StructureToPtr(dbi, buffer, true);

            WindowInteropHelper helper = new WindowInteropHelper(window);
            HwndSource hwndSource = HwndSource.FromHwnd(helper.Handle);
            hwndSource.AddHook(HwndHook);
            //return RegisterDeviceNotification(hwndSource.Handle, buffer, 0);
            return IntPtr.Zero;
        }

        /// <summary>
        /// 取消注册的事件
        /// </summary>
        /// <param name="handle">注册成功时返回的句柄</param>
        /// <returns></returns>
        public static bool Unregister(IntPtr handle)
        {
            return UnregisterDeviceNotification(handle);
        }

        static DeviceNotificationDeviceType GetDeviceType(IntPtr data)
        {
            DevBroadcastHdr devInfo = (DevBroadcastHdr)
                    Marshal.PtrToStructure(data, typeof(DevBroadcastHdr));
            return devInfo.DeviceType;
        }

        static string GetDeviceName(IntPtr data)
        {
            DeviceNotificationDeviceType devicetype = GetDeviceType(data);

            int offset = 0;
            if (devicetype == DeviceNotificationDeviceType.DeviceInterface)
            {
                offset = (int)Marshal.OffsetOf(typeof(DevBroadcastDeviceInterface), "Name");
            }
            else if (devicetype == DeviceNotificationDeviceType.Port)
            {
                offset = (int)Marshal.OffsetOf(typeof(DevBroadcastPort), "Name");
            }
            else
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            for (;;offset += Marshal.SystemDefaultCharSize)
            {
                char ch = (Char)Marshal.ReadInt16(data, offset);
                if (ch == '\0')
                    break;
                builder.Append(ch);
            }
            return builder.ToString();
        }

        static IntPtr HwndHook(IntPtr hwnd, int msg, 
            IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmDeviceChange)
            {
                switch ((int)wParam)
                {
                    case DBT_DEVICEARRIVAL:
                        if (DeviceArrival != null)
                        {
                            DeviceArrival(GetDeviceType(lParam), GetDeviceName(lParam));
                        }
                        break;
                    case DBT_DEVICEREMOVECOMPLETE:
                        if (DeviceRemoved != null)
                        {
                            DeviceRemoved(GetDeviceType(lParam), GetDeviceName(lParam));
                        }
                        break;
                }
            }
            handled = false;
            return IntPtr.Zero;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(
            IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(
            IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct DevBroadcastHdr
        {
            internal Int32 Size;
            internal DeviceNotificationDeviceType DeviceType;
            internal Int32 Reserved;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DevBroadcastDeviceInterface
        {
            internal Int32 Size;
            internal DeviceNotificationDeviceType DeviceType;
            internal Int32 Reserved;
            internal Guid classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
            internal String Name;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DevBroadcastPort
        {
            internal Int32 Size;
            internal DeviceNotificationDeviceType DeviceType;
            internal Int32 Reserved;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
            internal String Name;
        }
    }
}
