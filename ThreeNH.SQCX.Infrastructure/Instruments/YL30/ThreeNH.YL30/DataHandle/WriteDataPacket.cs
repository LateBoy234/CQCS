﻿﻿﻿﻿using NModbus.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  param($mt) $map[$mt.Value] ;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.DataHandle
{
    public class WriteDataPacket
    {
        public ushort SlaveID { get; set; }
        public byte Funcode { get; set; }
        public ushort StartAddress { get; set; }
        public byte[] PacketFrame { get; set; }
        public byte[] DataContent { get; set; }

        public byte ByteCount => (byte)DataContent?.Length;
        public ushort RegCount { get; set; }
        public RequestResult<List<byte>> RequestResult { get; set; }

        public int TotalLength => PacketFrame.Length;
        private WriteDataPacket()
        {

        }
        public WriteDataPacket(ushort slaveID, byte funcode, ushort startAddress, byte[] data, bool IsMultiRegisters = false)
        {
            SlaveID = slaveID;
            Funcode = funcode;
            StartAddress = startAddress;
            DataContent = data;
            PacketFrame = PackageDataFrame(IsMultiRegisters);
        }
        private byte[] PackageDataFrame(bool isMultiRegisters = false)
        {
            PacketContent content = new PacketContent();
            byte id = (byte)SlaveID;
            byte funcCode = (byte)Funcode;
            content.Enqueue(id);
            content.Enqueue(funcCode);
            content.Enqueue(StartAddress);
            if (isMultiRegisters)
            {
                RegCount = Convert.ToUInt16(DataContent?.Length / 2);
                content.Enqueue(RegCount);
                content.Enqueue(ByteCount);
            }
            content.Enqueue(DataContent);
            content.Enqueue(ModbusUtility.CalculateCrc(content.Bytes));
            return content.Bytes;
        }
        public WriteDataPacket Clone(IList<byte> bytes)
        {
            return new WriteDataPacket()
            {
                SlaveID = this.SlaveID,
                Funcode = this.Funcode,
                StartAddress = this.StartAddress,
                DataContent = this.DataContent,
                PacketFrame = bytes.ToArray(),
            };
        }
    }
}
