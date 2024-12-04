#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace SoulsFormats
{
        public interface IOodleCompressor
        {
               byte[] Compress(byte[] source, Oodle.OodleLZ_Compressor compressor, Oodle.OodleLZ_CompressionLevel level);
               byte[] Decompress(byte[] source, long uncompressedSize);
        }
}
