namespace nanoMeow.Mcp2515.Enums
{
    public enum Mcp2515TXBnCtrl : byte
    {
        ABTF = 0x40,
        MLOA = 0x20,
        TXERR = 0x10,
        TXREQ = 0x08,
        TXIE = 0x04,
        TXP = 0x03
    }
}
