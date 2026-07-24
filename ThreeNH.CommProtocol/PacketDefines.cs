using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ThreeNH.CommProtocol
{
    enum PacketType
    {
        InvalidPacket = 0,
        ShakeHands = 0xA1,
        ShakeHandsAck,
        ShakeHandsAck2,
        Close,
        CloseAck,
        Data,
        DataAck,
        KeepActivity,
        KeepActivityAck,
        PassThrough,
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    class MessagePacket
    {
        public byte header1;
        public byte header2;
        public byte pkt_type;    // 包类型
        public byte session;     // 会话标志
    }


    // 握手应答包
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    class ShakeHandsAckPacket : MessagePacket
    {
        public byte endian; // 大小端类型
        public ushort buf_size; // 仪器端的接收缓冲区大小，即上位机允许的每个包的最大大小
        private uint version; // 仪器代号、版本信息等
        public ushort crc; // CRC校验码

        public byte Version => (byte)(version & 0xFF);

        public int ProductType => (int)(version >> 8) & 0xFFFFFF;
    }

    // 数据包头
    // 数据包的格式：DataPacket + (长度为len - CRC_SIZE的数据) + CRC
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    class DataPacket : MessagePacket
    {
        public int seq; // 0-29位表示流水号，30-31 如果为00表示这是最后一个包，01表示还有数据，10用于DataAckPacket，表示请求重发; 如果为0xFFFFFFFF表示中止传输
        public ushort len; // 数据长度 + 2字节的CRC校验长度
    }

    // 数据命令包的应答
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    class DataAckPacket : MessagePacket
    {
        public int seq; // DataPacket.seq + 1，如果是请求重发则为 DataPacket.seq | 0x80000000
    }
}