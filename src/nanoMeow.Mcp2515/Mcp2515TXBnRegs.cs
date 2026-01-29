using Iot.Device.Mcp25xxx.Register;

namespace nanoMeow.Mcp2515
{
    public class Mcp2515TXBnRegs
    {
        public Address Ctrl { get; set; }
        public Address Sidh { get; set; }
        public Address Data { get; set; }
    }
}
