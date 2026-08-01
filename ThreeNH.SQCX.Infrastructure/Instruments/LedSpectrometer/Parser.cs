using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.SQCX.Infrastructure.Instruments.LedSpectrometer
{
    class Parser : CommunicationBase.PacketParser
    {
        protected override bool HasAck(byte command)
        {
            return command != (byte)Command.SpecimenPacket;
        }

        protected override bool IsValidCommand(byte command)
        {
            // 原有有效区间 + 新增的三个命令值
            return (command >= (byte)Command.GetDeviceInfo && command <= (byte)Command.ReadWhiteBoardData) ||
                   (command >= (byte)Command.SetDateTime && command < (byte)Command.NullCommand) ||
                   // 新增：判断是否是WriteSVD/ReadSVD/ReadSpectrumEight
                   command == (byte)Command.WriteSVD ||
                   command == (byte)Command.ReadSVD ||
                   command == (byte)Command.ReadSpectrumEight;
        }
    }
}
