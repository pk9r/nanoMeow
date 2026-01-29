namespace nanoMeow.Mcp2515.Enums
{
    [System.Flags]
    public enum Mcp2515CanIntF : byte
    {
        RX0IF = 0x01,
        RX1IF = 0x02,
        TX0IF = 0x04,
        TX1IF = 0x08,
        TX2IF = 0x10,
        ERRIF = 0x20,
        WAKIF = 0x40,
        MERRF = 0x80
    }
}
