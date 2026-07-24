using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ThreeNH.Bluetooth
{
    public class BluetoothClient
    {
        private Socket _socket = null;
        private BthEndPoint _endPoint = null;

        public BluetoothClient()
        {
            _socket = new Socket((AddressFamily)BluetoothApis.AF_BTH,
                SocketType.Stream, (ProtocolType)BluetoothApis.BTHPROTO_RFCOMM);
        }

        // 重新连接原来的地址
        public void Reconnect()
        {
            if (_endPoint == null)
            {
                throw new InvalidOperationException("_endPoint is null");
            }
            if (_socket.Connected)
            {
                _socket.Close();
            }
            _socket = new Socket((AddressFamily)BluetoothApis.AF_BTH,
                SocketType.Stream, (ProtocolType)BluetoothApis.BTHPROTO_RFCOMM);
            _socket.Connect(_endPoint);
        }

        public void Connect(BthEndPoint endPoint)
        {
            _endPoint = endPoint;
            _socket.Connect(_endPoint);
        }
        public void Connect(BluetoothAddress address, uint port)
        {
            _endPoint = new BthEndPoint(address, port);
            _socket.Connect(_endPoint);
        }
        public void Connect(string strAddress)
        {
            _endPoint = BthEndPoint.Parse(strAddress);
            _socket.Connect(_endPoint);
        }

        public Socket Client { get { return _socket; } }

        public bool Connected
        {
            get { return Client.Connected; }
        }

        public void Close()
        {
            Client.Close();
        }

        public NetworkStream GetStream()
        {
            return new NetworkStream(Client);
        }

        public string RemoteMachineName
        {
            get
            {
                return Client.RemoteEndPoint.ToString();
            }
        }

        public BluetoothDeviceInfo[] DiscoverDevices(
            bool returnAuthenticated = true,
            bool returnRemembered = true,
            bool returnUnknown = true,
            bool returnConnected = false,
            bool issueInquiry = true)
        {
            BluetoothDetector detector = new BluetoothDetector(
                returnAuthenticated, returnRemembered,
                returnUnknown, returnConnected, issueInquiry);

            return detector.DiscoverDevices();
        }

        public static void WSAStartup()
        {
            BluetoothApis.WSAStartup();
        }

        public static void WSACleanup()
        {
            BluetoothApis.WSACleanup();
        }
    }
}
