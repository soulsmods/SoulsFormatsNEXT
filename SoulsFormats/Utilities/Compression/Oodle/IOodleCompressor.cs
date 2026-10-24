using System;

namespace SoulsFormats
{
        public interface IOodleCompressor : IDisposable
        {
               byte[] Compress(byte[] source, Oodle.OodleLZ_Compressor compressor, Oodle.OodleLZ_CompressionLevel level);
               byte[] Decompress(byte[] source, long uncompressedSize);
        }
}
