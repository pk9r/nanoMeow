/* bit7   bit6   bit5   bit4   bit3   bit2   bit1    bit0
 * RXM1   RXM0   -      RXRTR  BUKT   BUKT1  FILHIT0 FILHIT1
 * 
 */

namespace nanoMeow.Mcp2515.Enums
{
    public enum Mcp2515RXBnCtrl : byte
    {
        RXM_STD    = 0b00100000,
        RXM_EXT    = 0b01000000,
        RXM_STDEXT = 0b00000000,
        RXM_MASK   = 0b01100000,
        RTR        = 0b00001000,

        RxB0_BUKT = 1 << 3,
        RxB0_FILHIT_MASK = 0b11,
        RxB0_FILHIT = 0b0,

        RxB1_FILHIT_MASK = 0b111,
        RxB1_FILHIT = 0b0,
    }
}
