using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ThreeNH.Bluetooth
{
    public class BluetoothDetector
    {
        private BluetoothApis.BluetoothDeviceSearchParams _btsp;

        public BluetoothDetector(
            bool returnAuthenticated = true,
            bool returnRemembered = true,
            bool returnUnknown = true,
            bool returnConnected = false,
            bool issueInquiry = true,
            byte timeoutMultiplier = 5)
        {
            _btsp = new BluetoothApis.BluetoothDeviceSearchParams();
            _btsp.dwSize = (uint)Marshal.SizeOf(_btsp);
            _btsp.fReturnAuthenticated = returnAuthenticated ? 1 : 0;
            _btsp.fReturnRemembered = returnRemembered ? 1 : 0;
            _btsp.fReturnUnknown = returnUnknown ? 1 : 0;
            _btsp.fReturnConnected = returnConnected ? 1 : 0;
            _btsp.fIssueInquiry = issueInquiry ? 1 : 0;
            _btsp.cTimeoutMultiplier = timeoutMultiplier;
            _btsp.hRadio = IntPtr.Zero;
        }

        public BluetoothDeviceInfo[] DiscoverDevices()
        {
            List<BluetoothDeviceInfo> list =
                new List<BluetoothDeviceInfo>();

            BluetoothApis.BluetoothDeviceInfo btdi =
                new BluetoothApis.BluetoothDeviceInfo();
            btdi.dwSize = (uint)Marshal.SizeOf(btdi);

            int lastError = 0;
            IntPtr hFind = BluetoothApis.BluetoothFindFirstDevice(ref _btsp, ref btdi);
            if (hFind != IntPtr.Zero)
            {
                do
                {
                    list.Add(new BluetoothDeviceInfo(btdi));
                } while (BluetoothApis.BluetoothFindNextDevice(hFind, ref btdi) != 0);

                lastError = Marshal.GetLastWin32Error();
                if (lastError == BluetoothApis.ERROR_SUCCESS 
                    || lastError == BluetoothApis.ERROR_NO_MORE_ITEMS)
                {
                    return list.ToArray();
                }
            } else {
                lastError = Marshal.GetLastWin32Error();
            }

            throw new SystemException(Win32Apis.GetLastErrorMsg(lastError));
        }

        public BluetoothDeviceInfo SelectDevice(bool skipServicesPage = true)
        {
            BluetoothApis.BluetoothSelectDeviceParams btsdp = 
                new BluetoothApis.BluetoothSelectDeviceParams();
            btsdp.dwSize = (uint)Marshal.SizeOf(btsdp);
            btsdp.fShowAuthenticated = _btsp.fReturnAuthenticated;
            btsdp.fShowRemembered = _btsp.fReturnRemembered;
            btsdp.fShowUnknown = _btsp.fReturnUnknown;
            btsdp.fAddNewDeviceWizard = _btsp.fIssueInquiry;
            btsdp.fSkipServicesPage = skipServicesPage ? 1 : 0;

            BluetoothDeviceInfo info = null;
            if (BluetoothApis.BluetoothSelectDevices(ref btsdp) != 0)
            {
                info = new BluetoothDeviceInfo((BluetoothApis.BluetoothDeviceInfo)
                    Marshal.PtrToStructure(btsdp.pDevices, typeof(BluetoothApis.BluetoothDeviceInfo)));
            }
            BluetoothApis.BluetoothSelectDevicesFree(ref btsdp);

            return info;
        }

    }
}
