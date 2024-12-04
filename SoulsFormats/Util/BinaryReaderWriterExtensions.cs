#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace SoulsFormats.Util
{
    public static class BinaryReaderWriterExtensions
    {
        public static long GetNextPaddedOffsetAfterCurrentField(this BinaryReaderEx br, int currentFieldLength, int align)
        {
            long pos = br.Position;
            pos += currentFieldLength;
            if (align <= 0)
                return pos;
            if (pos % align > 0)
                pos += align - (pos % align);
            return pos;
        }
    }
}
