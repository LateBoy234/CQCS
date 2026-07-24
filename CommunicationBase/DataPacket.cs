using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO.Ports;
using System.IO;
using ThreeNH.StructHelper;

namespace ThreeNH.CommunicationBase
{
    /// <summary>
    /// 通迅的数据包
    /// </summary>
    public class DataPacket
    {
        public const byte PacketHeaderFlag1 = 0x55;
        public const byte PacketHeaderFlag2 = 0xaa;
        public const byte AckOk = 0x00;

        /// <summary>
        /// 命令
        /// </summary>
        public byte Command { get; set; }
        /// <summary>
        /// 应答；如果为空表示无应答
        /// </summary>
        public byte? Ack { get; set; }
        /// <summary>
        /// 传输的数据
        /// </summary>
        public PacketContent PacketContent {get;set;}
        /// <summary>
        /// 返回可发送的字节数组
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(PacketHeaderFlag1);
            bytes.Add(PacketHeaderFlag2);
            bytes.Add((byte)Command);
            if (Ack != null)
            {
                bytes.Add((byte)Ack);
            }

            UInt32 length = 2;
            UInt16 checksum = 0;
            if (PacketContent != null)
            {
                length += PacketContent.Length;

                var lengthBytes = length.ToNetworkOrderBytes();
                bytes.AddRange(lengthBytes);
                bytes.AddRange(PacketContent.Bytes);

                checksum = CalculateCheckSum(lengthBytes);
                checksum += PacketContent.CheckSum;
            }
            else
            {
                var lengthBytes = length.ToNetworkOrderBytes();
                bytes.AddRange(length.ToNetworkOrderBytes());
                checksum = CalculateCheckSum(lengthBytes);
            }
            bytes.AddRange(checksum.ToNetworkOrderBytes());

            return bytes.ToArray();
        }

        public UInt16 CalculateCheckSum(IEnumerable<byte> bytes)
        {
            UInt16 checksum = 0;
            foreach (byte item in bytes)
            {
                checksum += item;
            }
            return checksum;
        }

        //public void Send(IComunicationDevice device)
        //{
        //    try
        //    {
        //        var buffer = ToBytes();
        //        device.Write(buffer, 0, buffer.Length);
        //    }
        //    catch (TimeoutException)
        //    {
        //        throw new CommunicationException(CommunicationError.WritingTimeout);
        //    }
        //}

        public bool IsAckOk
        {
            get
            {
                return Ack == AckOk;
            }
        }
    }
}
