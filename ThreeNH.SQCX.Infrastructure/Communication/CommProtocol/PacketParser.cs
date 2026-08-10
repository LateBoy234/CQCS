using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ThreeNH.StructHelper;

namespace ThreeNH.CommProtocol
{

    class PacketParser
    {
        enum Position
        {
            Header1,
            Header2,
            PktType,
            Other,
            Data,
            Completed,
        }

        private const byte Header1 = 0xaa;
        private const byte Header2 = 0x55;
        private const int Timeout = 1000;

        private DateTime _timestamp = DateTime.Now;
        private Position _pos = Position.Header1;
        private int _pktSize = 0;

        private readonly List<byte> _parsedBytes = new List<byte>();
        private readonly List<byte> _invalidBytes = new List<byte>();

        /// <summary>
        /// 获取包的长度
        /// </summary>
        /// <param name="pktType"></param>
        /// <returns>如果是有效包，返回包长度，否则返回0</returns>
        private static int GetPacketSize(byte pktType)
        {
            switch (pktType)
            {
                case (byte)PacketType.ShakeHandsAck:
                    return Marshal.SizeOf(typeof(ShakeHandsAckPacket));
                case (byte)PacketType.KeepActivity:
                case (byte)PacketType.KeepActivityAck:
                case (byte)PacketType.Close:
                case (byte)PacketType.CloseAck:
                    return Marshal.SizeOf(typeof(MessagePacket));
                case (byte)PacketType.Data:
                    return Marshal.SizeOf(typeof(DataPacket));
                case (byte)PacketType.DataAck:
                    return Marshal.SizeOf(typeof(DataAckPacket));
                default: // InvalidPacket
                    return 0;
            }
        }

        /// <summary>
        /// 返回解析到的包的类型
        /// </summary>
        public PacketType PacketType => _parsedBytes.Count > 0 ? (PacketType)_parsedBytes[2] : PacketType.InvalidPacket;

        /// <summary>
        /// 解析包，当解析完所有数据或是解析到一个完整的包时返回
        /// </summary>
        /// <param name="data">要解析的数据</param>
        /// <returns>返回解析的字节数</returns>
        public int Parse(IEnumerable<byte> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if ((DateTime.Now - _timestamp).TotalMilliseconds > Timeout || IsComplete)
            {
                if (_parsedBytes.Count > 0)
                {
                    if (!IsComplete)
                    {
                        _invalidBytes.AddRange(_parsedBytes);
                    }
                    _parsedBytes.Clear();
                }

                _pos = Position.Header1;
            }

            int parsedCount = 0;
            _timestamp = DateTime.Now;
            foreach (var b in data)
            {
                switch (_pos)
                {
                    case Position.Header1:
                        if (b == Header1)
                        {
                            _pos = Position.Header2;
                        }
                        else
                        {
                            _invalidBytes.Add(b);
                            _pos = Position.Header1;
                        }
                        break;
                    case Position.Header2:
                        if (b == Header2)
                        {
                            _pos = Position.PktType;
                        }
                        else
                        {
                            _invalidBytes.Add(Header1);
                            _invalidBytes.Add(b);
                            _pos = Position.Header1;
                        }
                        break;
                    case Position.PktType:
                        _pktSize = GetPacketSize(b);
                        if (_pktSize > 0)
                        {
                            _parsedBytes.Add(Header1);
                            _parsedBytes.Add(Header2);
                            _parsedBytes.Add(b);
                            _pos = Position.Other;
                        }
                        else
                        {
                            Trace.TraceWarning($"收到无效数据包类型：{b}");
                            _invalidBytes.Add(Header1);
                            _invalidBytes.Add(Header2);
                            _invalidBytes.Add(b);
                            _pos = Position.Header1;
                        }
                        break;
                    case Position.Other:
                        _parsedBytes.Add(b);
                        if (_parsedBytes.Count == _pktSize)
                        {
                            // 如果是数据包，包的长度需要加上数据的长度
                            // 否则解析完成
                            if (PacketType == PacketType.Data)
                            {
                                DataPacket dataPacket = StructConverter.BytesToStruct<DataPacket>(_parsedBytes.ToArray());
                                if (dataPacket.len != 0)
                                {
                                    _pktSize += dataPacket.len;
                                    _pos = Position.Data;
                                }
                                else
                                {
                                    _pos = Position.Completed;
                                }
                            }
                            else
                            {
                                _pos = Position.Completed;
                            }
                        }
                        break;
                    case Position.Data:
                        _parsedBytes.Add(b);
                        if (_parsedBytes.Count == _pktSize)
                        {
                            _pos = Position.Completed;
                        }
                        break;
                }

                parsedCount++;

                if (IsComplete)
                {
                    break;
                }
            }

            return parsedCount;
        }

        public bool IsComplete => _pos == Position.Completed;

        /// <summary>
        /// 获取解析到的无效数据，该操作会自动清空已解析的无效数据
        /// </summary>
        public byte[] InvalidData
        {
            get
            {
                var tmp = _invalidBytes.ToArray();
                _invalidBytes.Clear();
                return tmp;
            }
        }

        public byte[] ParsedData => _parsedBytes.ToArray();

        public void Clear()
        {
            _parsedBytes.Clear();
            _invalidBytes.Clear();
        }
    }
}