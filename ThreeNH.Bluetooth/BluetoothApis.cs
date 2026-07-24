using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;

namespace ThreeNH.Bluetooth
{
    

    internal static class BluetoothApis
    {
        public const int BLUETOOTH_MAX_NAME_SIZE = 248;
        public const int BLUETOOTH_MAX_PASSKEY_SIZE = 16;
        public const int BLUETOOTH_MAX_PASSKEY_BUFFER_SIZE = BLUETOOTH_MAX_PASSKEY_SIZE + 1;
        public const int BLUETOOTH_MAX_SERVICE_NAME_SIZE = 256;
        public const int BLUETOOTH_DEVICE_NAME_SIZE = 256;
        public const int FALSE = 0;
        public const int TRUE = 1;
        public const ushort AF_BTH = 32;
        public const int BTHPROTO_RFCOMM = 0x0003;
        // 错误码
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_NO_MORE_ITEMS = 259;

        [StructLayout(LayoutKind.Sequential)]
        internal struct BluetoothDeviceSearchParams
        {
            public uint dwSize;
            public int fReturnAuthenticated;
            public int fReturnRemembered;
            public int fReturnUnknown;
            public int fReturnConnected;
            public int fIssueInquiry;
            public byte cTimeoutMultiplier;
            public IntPtr hRadio;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct SystemTime 
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Milliseconds;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BluetoothAddress 
        {
            public long addr;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct BluetoothDeviceInfo
        {
              public uint dwSize;
              public BluetoothAddress Address;
              public uint ulClassofDevice;
              public int fConnected;
              public int fRemembered;
              public int fAuthenticated;
              public SystemTime stLastSeen;
              public SystemTime stLastUsed;
              [MarshalAs(UnmanagedType.ByValArray, SizeConst=BLUETOOTH_MAX_NAME_SIZE)]
              public Char[] szName;
        }

        internal delegate int PFN_DEVICE_CALLBACK(IntPtr pvParam, ref BluetoothDeviceInfo pDevice);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct BluetoothSelectDeviceParams
        {
            public uint dwSize;
            public uint cNumOfClasses;
            public IntPtr prgClassOfDevices;
            public string pszInfo;
            public IntPtr hwndParent;
            public int fForceAuthentication;
            public int fShowAuthenticated;
            public int fShowRemembered;
            public int fShowUnknown;
            public int fAddNewDeviceWizard;
            public int fSkipServicesPage;
            public PFN_DEVICE_CALLBACK pfnDeviceCallback;
            public IntPtr pvParam;  // 传递给回调函数pfnDeviceCallback的参数
            public uint cNumDevices;
            public IntPtr pDevices; // 指向BluetoothDeviceInfo
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct SockAddrBth
        {
            public ushort addressFamily;
            public BluetoothAddress btAddr;
            public Guid serviceClassId;
            public uint port;
        }

        [DllImport("Bthprops.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr BluetoothFindFirstDevice(
            ref BluetoothDeviceSearchParams pbtsp,
            ref BluetoothDeviceInfo pbtdi);

        [DllImport("Bthprops.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int BluetoothFindNextDevice(
            IntPtr hFind,
            ref BluetoothDeviceInfo pbtdi);

        [DllImport("Bthprops.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int BluetoothFindDeviceClose(
            IntPtr hFind);

        [DllImport("Bthprops.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int BluetoothSelectDevices(
            ref BluetoothSelectDeviceParams pbtsp);

        [DllImport("Bthprops.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int BluetoothSelectDevicesFree(
            ref BluetoothSelectDeviceParams pbtsp);

        [DllImport("Bthprops.cpl", CharSet = CharSet.Unicode)]
        internal static extern int BluetoothGetDeviceInfo(
            IntPtr hRadio,
            ref BluetoothDeviceInfo pbtdi);

        public static BluetoothDeviceInfo BluetoothGetDeviceInfo(BluetoothAddress address)
        {
            BluetoothDeviceInfo btdi = new BluetoothDeviceInfo();
            btdi.dwSize = (uint)Marshal.SizeOf(btdi);
            btdi.Address = address;
            int ret = BluetoothGetDeviceInfo(IntPtr.Zero, ref btdi);
            if (ret == 0)
                return btdi;
            throw new SystemException(Win32Apis.GetLastErrorMsg(ret));
        }

        [DllImport("Ws2_32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int WSAAddressToString(
            ref SockAddrBth lpsaAddress,
            uint dwAddressLength,
            IntPtr lpProtocolInfo,
            StringBuilder lpszAddressString,
            ref uint lpdwAddressStringLength);

        [DllImport("Ws2_32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int WSAStringToAddress(
            string addressString,
            int addressFamily,
            IntPtr lpProtocolInfo,
            ref SockAddrBth lpsaAddress,
            ref int lpAddressLength);

        [DllImport("Ws2_32.dll")]
        public static extern int WSAGetLastError();

        public const int WSADESCRIPTION_LEN = 256;
        public const int WSASYS_STATUS_LEN = 128;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct WSAData
        {
            ushort wVersion;
            ushort wHighVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = WSADESCRIPTION_LEN + 1)]
            Char[] szDescription;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = WSASYS_STATUS_LEN + 1)]
            Char[] szSystemStatus;
            ushort iMaxSockets;
            ushort iMaxUdpDg;
            IntPtr lpVendorInfo;
        };

        public static ushort MakeWord(byte low, byte high)
        {
            return (ushort)(low | (high << 8));
        }

        [DllImport("Ws2_32.dll")]
        public static extern int WSAStartup(
            ushort wVersionRequested,
            ref WSAData lpWSAData);

        public static void WSAStartup(ref WSAData data)
        {
            int ret = WSAStartup(MakeWord(2, 2), ref data);
            if (ret != 0)
            {
                throw new SystemException(Win32Apis.GetLastErrorMsg(WSAGetLastError()));
            }
        }

        public static void WSAStartup()
        {
            WSAData data = new WSAData();
            WSAStartup(ref data);
        }

        [DllImport("Ws2_32.dll")]
        public static extern int WSACleanup();


        internal static string AddressToString(SockAddrBth address)
        {
            uint bufsize = BLUETOOTH_MAX_SERVICE_NAME_SIZE;
            StringBuilder buffer = new StringBuilder((int)bufsize);
            int addrSize = Marshal.SizeOf(address);
            int ret = WSAAddressToString(ref address, (uint)addrSize,
                IntPtr.Zero, buffer, ref bufsize);

            if (ret == 0)
            {
                return buffer.ToString();
            }
            throw new SystemException(Win32Apis.GetLastErrorMsg(WSAGetLastError()));
        }

        internal static SockAddrBth StringToAddress(string stringAddress)
        {
            SockAddrBth address = new SockAddrBth();
            int addressLength = Marshal.SizeOf(address);

            int ret = WSAStringToAddress(stringAddress, AF_BTH, IntPtr.Zero,
                ref address, ref addressLength);

            // WSAStringToAddress不会返回Port
            if (address.serviceClassId == Guid.Empty && address.port == 0)
            {
                int n = stringAddress.LastIndexOf(':');
                if (!uint.TryParse(stringAddress.Substring(n + 1), out address.port))
                {
                    address.port = 1;
                }
            }

            if (ret == 0)
                return address;

            throw new SystemException(Win32Apis.GetLastErrorMsg(WSAGetLastError()));
        }
    }
}
