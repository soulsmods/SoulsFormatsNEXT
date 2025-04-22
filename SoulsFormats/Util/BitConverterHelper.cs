
using System;
using System.Buffers.Binary;

namespace SoulsFormats
{
    internal static class BitConverterHelper
    {
        internal static ushort ToUInt16BigEndian(byte[] bytes, int offset)
        {
            ushort value = BitConverter.ToUInt16(bytes, offset);
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }

        internal static int ToInt32BigEndian(byte[] bytes, int offset)
        {
            int value = BitConverter.ToInt32(bytes, offset);
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }
    }
}
