using System;

namespace nanoMeow.CanBus
{
    public class CanFrame
    {
        public uint Id { get; set; } = ushort.MaxValue;
        public byte Dlc { get; set; }
        public byte[] Data { get; set; } = new byte[8];
    }
}
