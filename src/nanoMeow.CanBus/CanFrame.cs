using System;

namespace nanoMeow.CanBus
{
    public class CanFrame
    {
        public uint CanId { get; set; }
        public byte CanDlc { get; set; }
        public byte[] Data { get; set; }
    }
}
