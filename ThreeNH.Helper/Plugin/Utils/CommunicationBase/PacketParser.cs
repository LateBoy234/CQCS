using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.StructHelper;

namespace ThreeNH.Helper.Plugin.Utils.CommunicationBase
{
    public abstract class PacketParser
    {
        Position ParsingPosition = Position.Header;

        //byte[] _header = new byte[2];
        List<byte> _header = new List<byte>();
        byte _command;
        byte? _ack;
        List<byte> _lengthBytes = new List<byte>();
        int _dataLength = 0;
        List<byte> _data = new List<byte>();
        List<byte> _checksumBytes = new List<byte>();

        public PacketParser()
        {
            Reset();
        }

        public void Reset()
        {
            ParsingPosition = Position.Header;

            //_header[0] = _header[1] = 0;
            _header.Clear();
            _command = 0;
            _ack = null;
            _lengthBytes.Clear();
            _checksumBytes.Clear();
            _data.Clear();
        }

        /// <summary>
        /// 如果是初始化状态返回真
        /// </summary>
        public bool IsEmpty
        {
            get { return _header.Count == 0; }
        }

        /// <summary>
        /// 如果解析出一个完整的包返回真
        /// </summary>
        public bool IsCompleted
        {
            get { return ParsingPosition == Position.Completed; }
        }

        /// <summary>
        /// 返回解析的结果；如果没有解析完，返回null
        /// </summary>
        public DataPacket Result
        {
            get
            {
                if (IsCompleted)
                {
                    DataPacket packet = new DataPacket();
                    packet.Command = _command;
                    packet.Ack = _ack;
                    if (_data.Count > 0)
                    {
                        packet.PacketContent = new PacketContent(_data);
                    }

                    return packet;
                }
                return null;
            }
        }

        public void Parse(Stream stream)
        {
            if (IsCompleted)
            {
                Reset();
            }

            while (!IsCompleted)
            {
                int need = ByteCountNeedRead();
                byte[] buffer = new byte[need];

                int bytesRead = stream.Read(buffer, 0, need);
                if (bytesRead == 0)
                    break;

                int n = Parse(buffer, 0, bytesRead);
            }
        }

        private int ByteCountNeedRead()
        {
            int need = 0;
            switch (ParsingPosition)
            {
                case Position.Header:
                    need = 2 - _header.Count;
                    break;
                case Position.Command:
                    need = 1;
                    break;
                case Position.Ack:
                    need = 1;
                    break;
                case Position.Length:
                    need = 4 - _lengthBytes.Count;
                    break;

                case Position.Data:
                    need = _dataLength - _data.Count + 2;  // 加上校验和的两个字节
                    break;

                case Position.CheckSum:
                    need = 2 - _checksumBytes.Count;
                    break;
                default:
                    need = 0;
                    break;
            }

            return need;
        }

        /// <summary>
        /// 对数据进行解析
        /// </summary>
        /// <param name="data">要解析的数据</param>
        /// <param name="offset">位置</param>
        /// <param name="count">要解析的字节数</param>
        /// <returns>返回解析的字节数</returns>
        public int Parse(byte[] data, int offset, int count)
        {
            if (IsCompleted)
            {
                Reset();
            }

            int start = offset;
            int end = offset + count;
            for (; offset < end && ParsingPosition != Position.Completed; ++offset)
            {
                switch (ParsingPosition)
                {
                    case Position.Header:
                        if (_header.Count == 0)
                        {
                            if (data[offset] == DataPacket.PacketHeaderFlag1)
                            {
                                _header.Add(data[offset]);
                            }
                        }
                        else if (_header.Count == 1)
                        {
                            if (data[offset] == DataPacket.PacketHeaderFlag2)
                            {
                                _header.Add(data[offset]);
                                ParsingPosition = Position.Command;
                            }
                        }
                        break;

                    case Position.Command:
                        if (IsValidCommand(data[offset]))
                        {
                            _command = data[offset];
                            if (HasAck(_command))
                                ParsingPosition = Position.Ack;
                            else
                                ParsingPosition = Position.Length;
                        }
                        else
                        {
                            throw new CommunicationException(CommunicationError.ParsedBadCommand);
                        }
                        break;

                    case Position.Ack:
                        _ack = data[offset];
                        ParsingPosition = Position.Length;
                        break;

                    case Position.Length:
                        _lengthBytes.Add(data[offset]);
                        if (_lengthBytes.Count == 4)
                        {
                            _dataLength = EndianConverter.NetworkOrderBytesToInt32(_lengthBytes.ToArray()) - 2; // -2减去两个字节的校验和
#if TRACE
                            Trace.TraceInformation("[Parse]Data Length is: " + _dataLength);
#endif
                            // 如果数据长度大于零，下一步解析数据，否则直接跳到校验和解析
                            if (_dataLength > 0)
                                ParsingPosition = Position.Data;
                            else
                                ParsingPosition = Position.CheckSum;
                        }
                        break;

                    case Position.Data:
                        if (_data.Count < _dataLength)
                        {
                            _data.Add(data[offset]);
                        }
                        if (_data.Count == _dataLength)
                        {
                            ParsingPosition = Position.CheckSum;
                        }
                        break;

                    case Position.CheckSum:
                        _checksumBytes.Add(data[offset]);
                        if (_checksumBytes.Count == 2)
                        {
                            ParsingPosition = Position.Completed;
                            UInt16 checksum = EndianConverter.NetworkOrderBytesToUInt16(_checksumBytes.ToArray());
                            if (checksum != GetCheckSum())
                            {
                                //throw new ArgumentException("Packet Parse Error: Check sum error");
                                Trace.TraceError("Packet Parse Error: Check sum error");
                            }
                        }
                        break;

                    default:
                        Trace.TraceInformation("[{0}]丢掉数据", this);
                        break;
                }
            }

            return offset - start;
        }

        /// <summary>
        /// 判断是否是有效命令
        /// </summary>
        /// <param name="command">命令代号</param>
        /// <returns>如果是有效命令返回真</returns>
        protected virtual bool IsValidCommand(byte command)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 判命令是否带有一个Ack
        /// </summary>
        /// <param name="command">命令代号</param>
        /// <returns>如果命令带有一个Ack返回真</returns>
        protected virtual bool HasAck(byte command)
        {
            throw new NotImplementedException();
        }

        UInt16 GetCheckSum()
        {
            UInt16 checksum = 0;
            foreach (byte item in _lengthBytes)
            {
                checksum += item;
            }
            foreach (byte item in _data)
            {
                checksum += item;
            }
            return checksum;
        }

        enum Position
        {
            Header,
            Command,
            Ack,
            Length,
            Data,
            CheckSum,
            Completed
        }
    }
}
