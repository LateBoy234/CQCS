using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ThreeNH.Bluetooth
{
    public class BthEndPoint : EndPoint
    {
        private const int AddressFamilyOffset = 0;
        private const int BthAddressOffset = 2;
        private const int ServiceClassIdOffset = 10;
        private const int PortOffset = 26;

        private SocketAddress _address = null;


        
        public BthEndPoint()
        {
            int size = Marshal.SizeOf(typeof(BluetoothApis.SockAddrBth));
            _address = new SocketAddress((AddressFamily)BluetoothApis.AF_BTH, size);

            int offset = AddressFamilyOffset;
            foreach (var b in BitConverter.GetBytes(BluetoothApis.AF_BTH))
            {
                _address[offset++] = b;
            }
        }

        private BthEndPoint(SocketAddress address)
        {
            _address = address;
        }

        internal BthEndPoint(BluetoothApis.SockAddrBth addr)
            : this()
        {
            SockAddrBth = addr;
        }

        public BthEndPoint(BluetoothAddress address, uint port)
            : this()
        {
            Address = address;
            Port = port;
        }

        public override AddressFamily AddressFamily
        {
            get { return (AddressFamily)BluetoothApis.AF_BTH; }
        }

        internal BluetoothApis.SockAddrBth SockAddrBth
        {
            get
            {
                List<byte> bytes = new List<byte>(ToByteArray());
                var sockaddr = new BluetoothApis.SockAddrBth();
                sockaddr.addressFamily = (ushort)AddressFamily;
                sockaddr.btAddr = Address._bthaddr;
                sockaddr.port = Port;
                sockaddr.serviceClassId = ServiceClassId;
                return sockaddr;
            }
            set
            {
                Address = new BluetoothAddress(value.btAddr);
                Port = value.port;
                ServiceClassId = value.serviceClassId;
            }
        }

        byte[] ToByteArray()
        {
            SocketAddress sockaddr = Serialize();
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < sockaddr.Size; i++)
            {
                bytes.Add(sockaddr[i]);
            }
            return bytes.ToArray();
        }
        public BluetoothAddress Address
        {
            get
            {
                return new BluetoothAddress(BitConverter.ToInt64(ToByteArray(), BthAddressOffset));
            }
            set
            {
                int offset = BthAddressOffset;
                foreach(var val in BitConverter.GetBytes(value.ToLong()))
                {
                    _address[offset++] = val;
                }
            }
        }
        public uint Port
        {
            get
            {
                return BitConverter.ToUInt32(ToByteArray(), PortOffset);
            }
            set
            {
                int offset = PortOffset;
                foreach(var val in BitConverter.GetBytes(value))
                {
                    _address[offset++] = val;
                }
            }
        }
        public Guid ServiceClassId
        {
            get
            {
                return new Guid(new List<byte>(ToByteArray()).GetRange(ServiceClassIdOffset, 16).ToArray());
            }
            set
            {
                int offset = ServiceClassIdOffset;
                foreach(var val in value.ToByteArray())
                {
                    _address[offset++] = val;
                }
            }
        }


        // 摘要:
        //     通过 System.Net.SocketAddress 实例创建 System.Net.EndPoint 实例。
        //
        // 参数:
        //   socketAddress:
        //     用作连接终结点的套接字地址。
        //
        // 返回结果:
        //     从指定的 System.Net.SocketAddress 实例初始化的新 System.Net.EndPoint 实例。
        //
        // 异常:
        //   System.NotImplementedException:
        //     当未在子类中重写该方法时，试图对该方法进行访问。
        public override EndPoint Create(SocketAddress socketAddress)
        {
            return new BthEndPoint(socketAddress);
        }
        //
        // 摘要:
        //     将终结点信息序列化为 System.Net.SocketAddress 实例。
        //
        // 返回结果:
        //     包含终结点信息的 System.Net.SocketAddress 实例。
        //
        // 异常:
        //   System.NotImplementedException:
        //     当未在子类中重写该方法时，试图对该方法进行访问。
        public override SocketAddress Serialize()
        {
            return _address;
        }
        public override string ToString()
        {
            return BluetoothApis.AddressToString(SockAddrBth);
        }

        public static bool TryParse(string str, out BthEndPoint endPoint)
        {
            try
            {
                endPoint = new BthEndPoint(BluetoothApis.StringToAddress(str));
                return true;
            }
            catch
            {
                endPoint = null;
                return false;
            }
        }

        public static BthEndPoint Parse(string str)
        {
            return new BthEndPoint(BluetoothApis.StringToAddress(str));
        }
    }
}
