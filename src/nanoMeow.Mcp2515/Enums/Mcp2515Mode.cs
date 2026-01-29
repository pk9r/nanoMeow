namespace nanoMeow.Mcp2515.Enums
{
    public enum Mcp2515Mode : byte
    {
        Normal = 0x00,
        Sleep = 0x20, // This suggestion by AI, not tested
        Loopback = 0x40,
        Configuration = 0x80
    }
}
