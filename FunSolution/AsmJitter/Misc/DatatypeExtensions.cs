using System;
using System.Collections.Generic;
using System.Text;

namespace AsmJitter.Misc
{
    internal static class DatatypeExtensions
    {

        internal static byte ConvertToByte(this sbyte value)
        {
            return (byte)value;
        }

        internal static byte ConvertToSingleByte(this int value)
        {
            return Convert.ToSByte(value).ConvertToByte();
        }

        internal static byte[] ConvertToByteArray(this int value)
        {
            return BitConverter.GetBytes(value);
        }

        internal static byte[] ConvertToByteArray(this float value)
        {
            return BitConverter.GetBytes(value);
        }

        internal static bool Is8Bit(this int value)
        {
            return value <= 0x7F && value >= -0x80;
        }

    }
}
