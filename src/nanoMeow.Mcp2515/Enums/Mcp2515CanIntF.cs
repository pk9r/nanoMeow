namespace nanoMeow.Mcp2515.Enums
{
    [System.Flags]
    public enum Mcp2515CanIntF : byte
    {
        RX0IF = 1,
        RX1IF = 1 << 1,
        TX0IF = 1 << 2,
        TX1IF = 1 << 3,
        TX2IF = 1 << 4,
        ERRIF = 1 << 5,
        WAKIF = 1 << 6,
        MERRF = 1 << 7
    }
}
