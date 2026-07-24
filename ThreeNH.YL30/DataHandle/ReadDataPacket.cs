using NModbus.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.YL30.Model;
using YL30Comm.Result;

namespace ThreeNH.YL30.DataHandle
{
    public class ReadDataPacket
    {
        public ushort SlaveID { get; set; }
        public byte Funcode { get; set; }
        public ushort StartAddress { get; set; }
        public ushort RegisterCount { get; set; }
        public byte[] PacketFrame { get; set; }
        public RequestResult<List<byte>> RequestResult { get; set; }
        public CommandType CommandType { get; set; }
        public int HeaderLength => CommandType == CommandType.ReadingCommand ? 3 : 5;
        public const int RearCRCLength = 2;

        public int ValidLength => CommandType == CommandType.ReadingCommand ? RegisterCount * 2 : 6;

        public int TotalLength => HeaderLength + RearCRCLength + ValidLength;

        public ReadDataPacket(CommandType type, ushort slaveID, byte funcode, ushort startAddress, ushort numberOfPoints)
        {
            SlaveID = slaveID;
            Funcode = funcode;
            StartAddress = startAddress;
            RegisterCount = numberOfPoints;
            CommandType = type;
            PacketFrame = PackageDataFrame(slaveID, funcode, startAddress, numberOfPoints);
        }
        private byte[] PackageDataFrame(ushort slaveID, byte funcode, ushort startAddress, ushort numberOfPoints)
        {
            byte id = (byte)slaveID;
            byte funcCode = funcode;
            PacketContent content = new PacketContent();
            content.Enqueue(id);
            content.Enqueue(funcCode);
            content.Enqueue(startAddress);
            content.Enqueue(numberOfPoints);
            content.Enqueue(ModbusUtility.CalculateCrc(content.Bytes));
            return content.Bytes;
        }

        public ReadDataPacket Clone(List<byte> dataPacket)
        {
            var validContent = dataPacket.Skip(this.HeaderLength).Take(this.ValidLength).ToList();
            return new ReadDataPacket(this.CommandType, this.SlaveID, this.Funcode, this.StartAddress, this.RegisterCount)
            {
                CommandType = this.CommandType,
                SlaveID = this.SlaveID,
                Funcode = this.Funcode,
                StartAddress = this.StartAddress,
                RegisterCount = this.RegisterCount,
                PacketFrame = dataPacket.ToArray(),
                RequestResult = new RequestResult<List<byte>>(ResultType.OperationSuccess, "信息读取成功", validContent)
            };
        }
        public ReadDataPacket Clone()
        {

            return new ReadDataPacket(this.CommandType, this.SlaveID, this.Funcode, this.StartAddress, this.RegisterCount)
            {
                CommandType = this.CommandType,
                SlaveID = this.SlaveID,
                Funcode = this.Funcode,
                StartAddress = this.StartAddress,
                RegisterCount = this.RegisterCount,
                PacketFrame = this.PacketFrame,
                RequestResult = this.RequestResult
            };
        }
    }
}
