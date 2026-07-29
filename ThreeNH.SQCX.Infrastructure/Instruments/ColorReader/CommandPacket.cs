﻿﻿namespace ThreeNH.SQCX.Infrastructure.Instruments.ColorReader
{
    public class CommandPacket
    {
        public CommandPacket(Command command = 0, byte[] data = null)
        {
            Command = (byte)command;
            Data = data;
        }

        public CommandPacket(byte command=0, byte[] data=null)
        {
            Command = command;
            Data = data;
        }

        public byte Command { get; set; }

        public byte[] Data { get; set; }

        public int AckCode { get; set; }

        /// <summary>
        /// 是否已接收完所有数据
        /// </summary>
        public bool IsCompleted { get; internal set; }
    }

}
