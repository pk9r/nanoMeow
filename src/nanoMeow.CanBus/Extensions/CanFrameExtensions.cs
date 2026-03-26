// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoMeow.CanBus.Extensions
{
    public static class CanFrameExtensions
    {
        public static bool IsBitSet(
            this CanFrame frame,
            byte byteIndex, byte bitIndex
        )
        {
            int maskedValue =
                frame.Data[byteIndex] &
                (1 << bitIndex);

            return maskedValue != 0;
        }

        public static bool IsBitSet(
            this CanFrame frame,
            byte mergedIndex
        )
        {
            byte byteIndex = (byte)(mergedIndex >> 4);
            byte bitIndex = (byte)(mergedIndex & 0x0F);

            int maskedValue =
                frame.Data[byteIndex] &
                (1 << bitIndex);

            return maskedValue != 0;
        }

        public static byte GetUInt2(
            this CanFrame frame,
            byte mergedIndex)
        {
            byte byteIndex = (byte)(mergedIndex >> 4);
            byte bitIndex = (byte)(mergedIndex & 0x0F);

            int maskedValue = frame.Data[byteIndex] >> bitIndex & 0b11;

            return (byte)maskedValue;
        }

        public static ushort GetUInt16(
            this CanFrame frame,
            byte byteIndex
        )
        {
            return (ushort)(
                (frame.Data[byteIndex] << 8) |
                frame.Data[byteIndex + 1]
            );
        }
    }
}
