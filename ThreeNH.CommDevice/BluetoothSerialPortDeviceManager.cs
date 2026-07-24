using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ThreeNH.Helper;

namespace ThreeNH.CommDevice
{

    public class BluetoothSerialPortDevice
    {
        public int Id { get; set; }

        public string DeviceName { get; set; }
    }


    public class BluetoothSerialPortDeviceManager : IDisposable
    {

        private List<BluetoothSerialPortDevice> _recvList = new List<BluetoothSerialPortDevice>();
        private ManualResetEvent _recvEvent = new ManualResetEvent(false);
        private Exception _ex = null;
        private ICommDevice CommDevice { get; set; }
        private int _roll;

        public BluetoothSerialPortDeviceManager(ICommDevice device)
        {
            CommDevice = device;
            try
            {
                CommDevice.BeginReceiveAsync(ReceiveAsyncCallback);
            }
            catch (Exception ex)
            {

            }
        }
        private void ReceiveAsyncCallback(object sender, byte[] bytesreceived, Exception ex)
        {
            if (ex != null)
            {
                _ex = ex;
                _recvEvent.Set();
                return;
            }

            lock (_recvList)
            {
                if (bytesreceived.ToList().IndexOf(new byte[] { 0x55, 0xaa }) >= 0)
                    _recvEvent.Set();
                var buffer = Encoding.UTF8.GetString(bytesreceived);
                //蓝牙模块返回错误直接返回
                if (buffer.StartsWith("ER="))
                    _recvEvent.Set();
                ////返回OK
                if (buffer.Contains("OK") && buffer.Contains("+CONNS"))
                {
                    _recvEvent.Set();
                }
                if (buffer.Contains("ROLE="))
                {
                    var temp = buffer.Replace("ROLE=", "").Replace("+", "").Replace("\r\n", "");
                    if (Int32.TryParse(temp, out _roll))
                        _recvEvent.Set();
                }
                //返回设备信息
                _recvList.AddRange(GetBluetoothSerialPortDeviceFromString(buffer));
            }
        }


        public List<BluetoothSerialPortDevice> GetBluetoothSerialPortDeviceFromString(string source)
        {
            List<BluetoothSerialPortDevice> devices = new List<BluetoothSerialPortDevice>();
            var temp1 = source.Split('=');
            if (temp1 != null && temp1.Length == 2)
            {
                var temp2 = temp1[1].Split(':');
                {
                    if (temp2 != null && temp2.Length == 2)
                    {
                        var device = new BluetoothSerialPortDevice();
                        device.Id = Convert.ToInt32(temp2[0]);
                        var temp3 = temp2[1].Split(',');
                        if (temp3 != null && temp3.Length > 0)
                        {
                            device.DeviceName = temp3[0];
                        }
                        devices.Add(device);
                    }
                }
            }
            //Regex rg = new Regex(@"[\d{1}]:[a-zA-Z0-9]{12}", RegexOptions.Multiline | RegexOptions.Singleline);
            //MatchCollection matches = rg.Matches(source);
            //foreach (Match item in matches)
            //{
            //    var items = item.Value.Split(':');
            //    BluetoothSerialPortDevice device = new BluetoothSerialPortDevice()
            //    {
            //        Id = Convert.ToInt32(items[0]),
            //        DeviceName = items[1]
            //    };
            //    devices.Add(device);
            //}
            return devices;
        }


        public bool GetDeviceInfo()
        {
            _recvEvent.Reset();
            CommDevice.Write(new byte[] { 0x55, 0xaa, 0xa1, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02 }, 0, 9);
            if (!_recvEvent.WaitOne(1200))
            {
                return false;
            }
            return true;
        }

        public bool CheckMaster()
        {
            _recvEvent.Reset();
            var conn = Encoding.UTF8.GetBytes($"AT+MT{Environment.NewLine}");
            CommDevice.Write(conn, 0, conn.Length);
            if (!_recvEvent.WaitOne(2000))
            {
                return false;
            }
            //CommDevice.Write(new byte[] { 0x55, 0xaa, 0xa1, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02 }, 0, 9);
            return _roll == 1;
        }

        public bool Connect(int index)
        {
            _recvEvent.Reset();
            var conn = Encoding.UTF8.GetBytes($"AT+CONN={index}{Environment.NewLine}");
            CommDevice.Write(conn, 0, conn.Length);
            if (!_recvEvent.WaitOne(1000))
            {
                return false;
            }
            //CommDevice.Write(new byte[] { 0x55, 0xaa, 0xa1, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02 }, 0, 9);
            return true;
        }

        public List<BluetoothSerialPortDevice> Search(int timeout = 2000)
        {
            try
            {
                _recvEvent.Reset();
                var inq = Encoding.UTF8.GetBytes($"AT+SC{Environment.NewLine}");
                CommDevice.Write(inq, 0, inq.Length);
                _recvEvent.WaitOne(timeout);
                return _recvList;
            }
            finally
            {
                //必须再次发送AT+SC停止搜索
                var sinq = Encoding.UTF8.GetBytes($"AT+SC{Environment.NewLine}");
                CommDevice.Write(sinq, 0, sinq.Length);
            }

        }

        public void Dispose()
        {
            CommDevice?.EndReceiveAsync();
            //CommDevice = null;
        }

        ~BluetoothSerialPortDeviceManager()
        {
            CommDevice?.EndReceiveAsync();
            //CommDevice = null;
        }
    }
}
