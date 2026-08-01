namespace ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer
{
    class Parser : CommunicationBase.PacketParser
    {
        protected override bool HasAck(byte command)
        {
            return command != (byte)Command.SpecimenPacket;
        }

        protected override bool IsValidCommand(byte command)
        {
            return command >= (byte)Command.GetDeviceInfo &&
                command < (byte)Command.NullCommand;
        }
    }
}
