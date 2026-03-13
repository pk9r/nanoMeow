using System;

namespace nanoMeow.CanBus
{
    public class CanFrame
    {
        public uint Id { get; set; }
        public byte Dlc { get; set; }
        public byte[] Data { get; set; }
    }
}
