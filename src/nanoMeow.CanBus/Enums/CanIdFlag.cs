using System;

namespace nanoMeow.CanBus.Enums
{
    [Flags]
    public enum CanIdFlag : ulong
    {
        EFF = 0x80000000UL,
        RTR = 0x40000000UL,
        ERR = 0x20000000UL
    }
}
