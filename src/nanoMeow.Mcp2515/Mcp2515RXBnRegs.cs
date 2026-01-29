using Iot.Device.Mcp25xxx;
using Iot.Device.Mcp25xxx.Register;
using nanoMeow.Mcp2515.Enums;

namespace nanoMeow.Mcp2515
{
    public class Mcp2515RXBnRegs
    {
        public Address Ctrl { get; set; }
        public RxBufferAddressPointer Sidh { get; set; }
        public RxBufferAddressPointer Data { get; set; }
        public Mcp2515CanIntF RXnIF { get; set; }
    }
}
