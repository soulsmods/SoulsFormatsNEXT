using System;
using System.Runtime.InteropServices;
using static SoulsFormats.Oodle;
using InteropNativeLibrary = System.Runtime.InteropServices.NativeLibrary;

namespace SoulsFormats
{
    internal sealed unsafe class OodleCompressor : IOodleCompressor
    {
        private IntPtr handle;
        private readonly bool isOodle26;

        private readonly delegate* unmanaged<OodleLZ_Compressor, byte*, long, byte*, OodleLZ_CompressionLevel, IntPtr, IntPtr, IntPtr, IntPtr, long, long> compress;
        private readonly delegate* unmanaged<OodleLZ_Compressor, OodleLZ_CompressionLevel, IntPtr> compressOptionsGetDefault;
        private readonly delegate* unmanaged<byte*, long, byte*, long, OodleLZ_FuzzSafe, OodleLZ_CheckCRC, OodleLZ_Verbosity, IntPtr, long, IntPtr, IntPtr, IntPtr, long, OodleLZ_Decode_ThreadPhase, long> decompress;

        private readonly IntPtr getCompressedBufferSizeNeeded;
        private readonly IntPtr getDecodeBufferSize;

        public OodleCompressor(IntPtr handle, uint version)
        {
            this.handle = handle;
            isOodle26 = Oodle2Version(version) == 6;
            compress = (delegate* unmanaged<OodleLZ_Compressor, byte*, long, byte*, OodleLZ_CompressionLevel, IntPtr, IntPtr, IntPtr, IntPtr, long, long>)
                InteropNativeLibrary.GetExport(handle, "OodleLZ_Compress");
            compressOptionsGetDefault = (delegate* unmanaged<OodleLZ_Compressor, OodleLZ_CompressionLevel, IntPtr>)
                InteropNativeLibrary.GetExport(handle, "OodleLZ_CompressOptions_GetDefault");
            decompress = (delegate* unmanaged<byte*, long, byte*, long, OodleLZ_FuzzSafe, OodleLZ_CheckCRC, OodleLZ_Verbosity, IntPtr, long, IntPtr, IntPtr, IntPtr, long, OodleLZ_Decode_ThreadPhase, long>)
                InteropNativeLibrary.GetExport(handle, "OodleLZ_Decompress");
            getCompressedBufferSizeNeeded = InteropNativeLibrary.GetExport(handle, "OodleLZ_GetCompressedBufferSizeNeeded");
            getDecodeBufferSize = InteropNativeLibrary.GetExport(handle, "OodleLZ_GetDecodeBufferSize");
        }

        /// <summary>
        /// Loads the Oodle library at <paramref name="path"/>.
        /// </summary>
        public static OodleCompressor Load(string path)
        {
            IntPtr handle = InteropNativeLibrary.Load(path, typeof(OodleCompressor).Assembly, null);

            try
            {
                return new OodleCompressor(handle, QueryVersion(handle));
            }
            catch
            {
                InteropNativeLibrary.Free(handle);
                throw;
            }
        }

        public static uint QueryVersion(IntPtr handle)
        {
            IntPtr checkVersion = InteropNativeLibrary.GetExport(handle, "Oodle_CheckVersion");
            IntPtr setPrintf = InteropNativeLibrary.GetExport(handle, "OodleCore_Plugins_SetPrintf");

            // Oodle prints a warning here, so we disable the logging
            IntPtr printf = ((delegate* unmanaged<IntPtr, IntPtr>)setPrintf)(IntPtr.Zero);
            uint libVersion = 0;
            ((delegate* unmanaged<uint, uint*, int>)checkVersion)(0, &libVersion);
            ((delegate* unmanaged<IntPtr, IntPtr>)setPrintf)(printf);

            return libVersion;
        }

        public byte[] Compress(byte[] source, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level)
        {
            ObjectDisposedException.ThrowIf(handle == IntPtr.Zero, this);

            OodleLZ_CompressOptions options = Marshal.PtrToStructure<OodleLZ_CompressOptions>(compressOptionsGetDefault(compressor, level));
            // Required for the game to not crash
            options.seekChunkReset = true;
            // This is already the default but I am including it for authenticity to game code
            options.seekChunkLen = 0x40000;
            IntPtr pOptions = Marshal.AllocHGlobal(Marshal.SizeOf<OodleLZ_CompressOptions>());

            try
            {
                Marshal.StructureToPtr(options, pOptions, false);
                long compressedBufferSizeNeeded = isOodle26
                    ? ((delegate* unmanaged<long, long>)getCompressedBufferSizeNeeded)(source.LongLength)
                    : ((delegate* unmanaged<OodleLZ_Compressor, long, long>)getCompressedBufferSizeNeeded)(compressor, source.LongLength);
                byte[] compBuf = new byte[compressedBufferSizeNeeded];
                long compLen;
                fixed (byte* pSource = source)
                fixed (byte* pCompBuf = compBuf)
                {
                    compLen = compress(compressor, pSource, source.LongLength, pCompBuf, level, pOptions,
                        IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0);
                }
                Array.Resize(ref compBuf, (int)compLen);
                return compBuf;
            }
            finally
            {
                Marshal.FreeHGlobal(pOptions);
            }
        }

        public byte[] Decompress(byte[] source, long uncompressedSize)
        {
            ObjectDisposedException.ThrowIf(handle == IntPtr.Zero, this);

            // corruptionPossible is a 4-byte BOOL
            long decodeBufferSize = isOodle26
                ? ((delegate* unmanaged<long, int, long>)getDecodeBufferSize)(uncompressedSize, 1)
                : ((delegate* unmanaged<OodleLZ_Compressor, long, int, long>)getDecodeBufferSize)(
                    OodleLZ_Compressor.OodleLZ_Compressor_Invalid, uncompressedSize, 1);
            byte[] rawBuf = new byte[decodeBufferSize];
            long rawLen;
            fixed (byte* pSource = source)
            fixed (byte* pRawBuf = rawBuf)
            {
                rawLen = decompress(pSource, source.LongLength, pRawBuf, uncompressedSize,
                    OodleLZ_FuzzSafe.OodleLZ_FuzzSafe_Yes, OodleLZ_CheckCRC.OodleLZ_CheckCRC_No,
                    OodleLZ_Verbosity.OodleLZ_Verbosity_None,
                    IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0,
                    OodleLZ_Decode_ThreadPhase.OodleLZ_Decode_Unthreaded);
            }
            Array.Resize(ref rawBuf, (int)rawLen);
            return rawBuf;
        }

        public void Dispose()
        {
            if (handle == IntPtr.Zero)
                return;

            InteropNativeLibrary.Free(handle);
            handle = IntPtr.Zero;
        }
    }
}
