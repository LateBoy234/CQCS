using System;
using System.IO;
using System.Net.Sockets;
using ThreeNH.Bluetooth;
using System.Threading;
using System.Diagnostics;

namespace ThreeNH.CommDevice
{
    public class BluetoothDevice : ICommDevice
    {
        public const string BluetoothDeviceName = "3nh";
        private const int _Port = 1;
        private const int _BufferLenth = 1024 * 1024;

        private BluetoothClient _bluetooth = null;
        private IAsyncResult _asyncResult = null;
        private ReceiveAsyncCallback _receiveCallback;
        private Mutex _lock = new Mutex();
        private byte[] _buffer = new byte[_BufferLenth];
        private BthEndPoint _endPoint = null;

        public string DeviceName
        {
            get
            {
                return IsOpen ? _bluetooth.RemoteMachineName : null;
            }
        }

        public bool IsOpen
        {
            get
            {
                return _bluetooth != null && (_bluetooth.Connected || _endPoint != null);
            }
        }

        public Stream Stream
        {
            get
            {
                return _bluetooth.GetStream();
            }
        }

        public string DriverDescription { get; set; }

        public bool IsBlueSerialPortDevice => false;

        public void Close()
        {
            //EndReceiveAsync();
            _bluetooth.Close();
            _bluetooth = null;
            _receiveCallback = null;
            _endPoint = null;
        }

        /// <summary>
        /// 打开蓝牙设备
        /// </summary>
        /// <param name="device">蓝牙地址，类型为BluetoothAddress</param>
        public void Open(object device = null)
        {
            _bluetooth = new BluetoothClient();
            BthEndPoint endPoint = null;

            if (device is BluetoothDeviceInfo)
            {
                var address = device as BluetoothDeviceInfo;
                endPoint = new BthEndPoint(address.Address, 1);
            }
            else if (device is string)
            {
                if (!BthEndPoint.TryParse(device as string, out endPoint))
                    endPoint = null;
            }
            else if (device is BthEndPoint)
            {
                endPoint = device as BthEndPoint;
            }
            else if (device is BluetoothAddress)
            {
                endPoint = new BthEndPoint((BluetoothAddress)device, 1);
            }

            if (endPoint == null)
            {
                BluetoothDetector detector = new BluetoothDetector(true, true, true, true, false);
                var deviceInfo = detector.SelectDevice();
                if (deviceInfo?.Address == null)
                {
                    //throw new CommunicationException(CommunicationError.DeviceOpenFailed, "Connection was canceled by user");
                    throw new System.IO.FileNotFoundException();
                }
                endPoint = new BthEndPoint(deviceInfo.Address, 1);
            }

            try
            {
                _bluetooth.Connect(endPoint);
                _endPoint = endPoint;
            }
            catch (Exception ex)
            {
                _bluetooth = null;
                _endPoint = null;
                //throw new CommunicationException(CommunicationError.DeviceOpenFailed, ex.Message);
                throw new System.IO.FileLoadException();
            }

        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

        bool ICommDevice.BeginReceiveAsync(ReceiveAsyncCallback receiveCallback)
        {
            if (receiveCallback == null)
                throw new ArgumentNullException();

            if (!_lock.WaitOne(50))
                return false;

            if (_receiveCallback != null)
            {
                _lock.ReleaseMutex();
                return false;
            }

            if (!_bluetooth.Connected && _endPoint != null)
            {
                try
                {
                    _bluetooth.Reconnect();
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[{0}]重新连接到{1}失败：{2}", this, _endPoint.Address.ToString(), ex.Message);
                    _lock.ReleaseMutex();
                    return false;
                }
            }

            _receiveCallback += receiveCallback;
            _asyncResult = _bluetooth.Client.BeginReceive(_buffer, 0, _BufferLenth,
                SocketFlags.None, OnReceiveCompleted, _bluetooth.Client);

            _lock.ReleaseMutex();
            return true;
        }

        private void OnReceiveCompleted(IAsyncResult ar)
        {
            Socket socket = ar.AsyncState as Socket;
            if (!socket.Connected)
            {
                return;
            }

            _lock.WaitOne();
            if (_receiveCallback == null)
            {
                Trace.TraceWarning("[{0}] _receiveCallback is null", this);
            }

            try
            {
                int n = socket.EndReceive(ar);
                if (n > 0)
                {
                    byte[] bytesReceived = new byte[n];
                    Array.Copy(_buffer, 0, bytesReceived, 0, n);
                    _receiveCallback?.Invoke(this, bytesReceived, null);

                    _asyncResult = socket.BeginReceive(_buffer, 0, _BufferLenth,
                        SocketFlags.None, OnReceiveCompleted, ar.AsyncState);
                }
                else
                {
                    _receiveCallback?.Invoke(this, null, null);
                }
            }
            catch (Exception ex)
            {
                if (ex is SocketException)
                {
                    var sockEx = ex as SocketException;
                    _receiveCallback?.Invoke(this, null, null);
                }
                else
                {
                    _receiveCallback?.Invoke(this, null, ex);
                }
            }

            _lock.ReleaseMutex();
        }

        public bool EndReceiveAsync()
        {
            if (_receiveCallback == null)
            {
                return false;
            }

            if (!_lock.WaitOne())
                return false;

            _receiveCallback = null;
            if (_bluetooth != null && _bluetooth.Client.Connected && _asyncResult != null)
            {
                //_bluetooth.Client.EndReceive(_asyncResult);
                _bluetooth.Client.Close();
                _asyncResult = null;
            }

            _lock.ReleaseMutex();
            return true;
        }
    }
}
