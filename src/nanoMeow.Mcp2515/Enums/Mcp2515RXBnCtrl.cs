namespace nanoMeow.Mcp2515.Enums
{
    public enum Mcp2515RXBnCtrl : byte
    {
        RXM_STD = 0x20,
        RXM_EXT = 0x40,
        RXM_STDEXT = 0x00,
        RXM_MASK = 0x60,
        RTR = 0x08,
    }
}
