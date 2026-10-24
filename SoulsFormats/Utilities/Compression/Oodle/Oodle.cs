using SoulsFormats.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using InteropNativeLibrary = System.Runtime.InteropServices.NativeLibrary;

namespace SoulsFormats
{
    /// <summary>
    /// Class for handling the Oodle compression library included with FromSoftware games.
    /// </summary>
    public static class Oodle
    {
        private const string LibraryName = "oo2core";

        private static readonly string[] WindowsCandidates =
            ["oo2core_9_win64.dll", "oo2core_8_win64.dll", "oo2core_6_win64.dll"];

        private static readonly string[] MacOSCandidates =
            ["liboo2coremac64.2.9.dylib", "liboo2coremac64.2.8.dylib", "liboo2coremac64.2.6.dylib"];

        private static readonly string[] LinuxCandidates =
            ["liboo2corelinux64.so.9", "liboo2corelinux64.so.8", "liboo2corelinux64.so.6"];

        private static string[] Candidates
        {
            get
            {
                if (OperatingSystem.IsWindows())
                    return WindowsCandidates;
                else if (OperatingSystem.IsMacOS())
                    return MacOSCandidates;
                else
                    return LinuxCandidates;
            }
        }

        private static readonly Lazy<(IntPtr Handle, uint Version, string Errors)> Loaded = new(Load);

        static Oodle()
        {
            InteropNativeLibrary.SetDllImportResolver(typeof(Oodle).Assembly, (name, _, _) =>
                name == LibraryName ? Loaded.Value.Handle : IntPtr.Zero);
        }

        /// <summary>
        /// Whether a supported Oodle library could be found.
        /// </summary>
        public static bool IsAvailable => Loaded.Value.Handle != IntPtr.Zero;

        /// <summary>
        /// Directories to look for the Oodle library in.
        /// </summary>
        public static List<string> SearchPaths { get; } = [];

        // Oodle versions are (46 << 24) | (major << 16) | (minor << 8) | sizeof(OodleLZ_SeekTable),
        private static uint Oodle2Version(uint version) => version >> 16 & 0xFF;

        private static bool IsOodle26 => Oodle2Version(Loaded.Value.Version) == 6;

        private static (IntPtr, uint, string) Load()
        {
            var errors = new List<string>();

            foreach (string name in Candidates)
            {
                foreach (string path in SearchPaths.Select(directory => Path.Combine(directory, name)).Append(name))
                {
                    IntPtr handle;
                    try
                    {
                        handle = InteropNativeLibrary.Load(path, typeof(Oodle).Assembly, null);
                    }
                    catch (DllNotFoundException ex)
                    {
                        errors.Add($"{path}: {ex.Message}");
                        continue;
                    }

                    if (QueryVersion(handle) is { } version)
                        return (handle, version, null);

                    errors.Add($"{path}: not Oodle 2.6 or later");
                    InteropNativeLibrary.Free(handle);
                }
            }

            return (IntPtr.Zero, default, string.Join("\n", errors));
        }

        private static unsafe uint? QueryVersion(IntPtr handle)
        {
            // Can't rely on [DllImport] until we have the resolver setup
            if (!InteropNativeLibrary.TryGetExport(handle, "Oodle_CheckVersion", out IntPtr checkVersion)
                || !InteropNativeLibrary.TryGetExport(handle, "OodleCore_Plugins_SetPrintf", out IntPtr setPrintf))
                return null;

            // Oodle prints a warning here, so we disable the logging
            IntPtr printf = ((delegate* unmanaged<IntPtr, IntPtr>)setPrintf)(IntPtr.Zero);
            uint libVersion = 0;
            ((delegate* unmanaged<uint, uint*, int>)checkVersion)(0, &libVersion);
            ((delegate* unmanaged<IntPtr, IntPtr>)setPrintf)(printf);

            return libVersion;
        }

        private static void EnsureAvailable()
        {
            if (IsAvailable)
                return;

            throw new NoOodleFoundException("Could not find a supported version of the Oodle compression library.\n"
                                            + $"Please copy {Candidates[0]}, {Candidates[1]} or {Candidates[2]} into the program directory, or add the directory containing one to Oodle.SearchPaths\n"
                                            + Loaded.Value.Errors);
        }

        public static byte[] Compress(byte[] source, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level)
        {
            EnsureAvailable();

            IntPtr pOptions = OodleLZ_CompressOptions_GetDefault(compressor, level);
            OodleLZ_CompressOptions options = Marshal.PtrToStructure<OodleLZ_CompressOptions>(pOptions);
            // Required for the game to not crash
            options.seekChunkReset = true;
            // This is already the default but I am including it for authenticity to game code
            options.seekChunkLen = 0x40000;
            pOptions = Marshal.AllocHGlobal(Marshal.SizeOf<OodleLZ_CompressOptions>());

            try
            {
                Marshal.StructureToPtr(options, pOptions, false);
                long compressedBufferSizeNeeded = IsOodle26
                    ? OodleLZ_GetCompressedBufferSizeNeeded_V6(source.LongLength)
                    : OodleLZ_GetCompressedBufferSizeNeeded(compressor, source.LongLength);
                byte[] compBuf = new byte[compressedBufferSizeNeeded];
                long compLen = OodleLZ_Compress(compressor, source, source.LongLength, compBuf, level, pOptions,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0);
                Array.Resize(ref compBuf, (int)compLen);
                return compBuf;
            }
            finally
            {
                Marshal.FreeHGlobal(pOptions);
            }
        }

        public static byte[] Decompress(byte[] source, long uncompressedSize)
        {
            EnsureAvailable();

            long decodeBufferSize = IsOodle26
                ? OodleLZ_GetDecodeBufferSize_V6(uncompressedSize, true)
                : OodleLZ_GetDecodeBufferSize(OodleLZ_Compressor.OodleLZ_Compressor_Invalid, uncompressedSize, true);
            byte[] rawBuf = new byte[decodeBufferSize];
            long rawLen = OodleLZ_Decompress(source, source.LongLength, rawBuf, uncompressedSize,
                OodleLZ_FuzzSafe.OodleLZ_FuzzSafe_Yes, OodleLZ_CheckCRC.OodleLZ_CheckCRC_No,
                OodleLZ_Verbosity.OodleLZ_Verbosity_None,
                IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0,
                OodleLZ_Decode_ThreadPhase.OodleLZ_Decode_Unthreaded);
            Array.Resize(ref rawBuf, (int)rawLen);
            return rawBuf;
        }

        /// <summary>
        /// Gets a compressor for the Oodle library in use.
        /// </summary>
        /// <exception cref="NoOodleFoundException">No supported Oodle library could be found.</exception>
        [Obsolete("Use Oodle.Compress and Oodle.Decompress.")]
        public static IOodleCompressor GetOodleCompressor()
        {
            EnsureAvailable();
            return Compressor.Instance;
        }

        /// <inheritdoc cref="GetOodleCompressor"/>
        [Obsolete("Use Oodle.Compress and Oodle.Decompress.")]
        public static IOodleCompressor GetOodleCompressor2() => GetOodleCompressor();

        private sealed class Compressor : IOodleCompressor
        {
            public static readonly Compressor Instance = new();

            public byte[] Compress(byte[] source, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level)
                => Oodle.Compress(source, compressor, level);

            public byte[] Decompress(byte[] source, long uncompressedSize) => Oodle.Decompress(source, uncompressedSize);
        }

        #region Imports

        /// <param name="compressor"></param>
        /// <param name="rawBuf"></param>
        /// <param name="rawLen"></param>
        /// <param name="compBuf"></param>
        /// <param name="level"></param>
        /// <param name="pOptions">= NULL</param>
        /// <param name="dictionaryBase">= NULL</param>
        /// <param name="lrm">= NULL</param>
        /// <param name="scratchMem">= NULL</param>
        /// <param name="scratchSize">= 0</param>
        [DllImport(LibraryName)]
        private static extern long OodleLZ_Compress(
            OodleLZ_Compressor compressor,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] rawBuf,
            long rawLen,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] compBuf,
            OodleLZ_CompressionLevel level,
            IntPtr pOptions,
            IntPtr dictionaryBase,
            IntPtr lrm,
            IntPtr scratchMem,
            long scratchSize);

        /// <param name="compressor">= OodleLZ_Compressor_Invalid</param>
        /// <param name="lzLevel">= OodleLZ_CompressionLevel_Normal</param>
        [DllImport(LibraryName)]
        private static extern IntPtr OodleLZ_CompressOptions_GetDefault(
            OodleLZ_Compressor compressor,
            OodleLZ_CompressionLevel lzLevel);

        /// <param name="compBuf"></param>
        /// <param name="compBufSize"></param>
        /// <param name="rawBuf"></param>
        /// <param name="rawLen"></param>
        /// <param name="fuzzSafe">= OodleLZ_FuzzSafe_Yes</param>
        /// <param name="checkCRC">= OodleLZ_CheckCRC_No</param>
        /// <param name="verbosity">= OodleLZ_Verbosity_None</param>
        /// <param name="decBufBase">= NULL</param>
        /// <param name="decBufSize">= 0</param>
        /// <param name="fpCallback">= NULL</param>
        /// <param name="callbackUserData">= NULL</param>
        /// <param name="decoderMemory">= NULL</param>
        /// <param name="decoderMemorySize">= 0</param>
        /// <param name="threadPhase">= OodleLZ_Decode_Unthreaded</param>
        [DllImport(LibraryName)]
        private static extern long OodleLZ_Decompress(
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] compBuf,
            long compBufSize,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] rawBuf,
            long rawLen,
            OodleLZ_FuzzSafe fuzzSafe,
            OodleLZ_CheckCRC checkCRC,
            OodleLZ_Verbosity verbosity,
            IntPtr decBufBase,
            long decBufSize,
            IntPtr fpCallback,
            IntPtr callbackUserData,
            IntPtr decoderMemory,
            long decoderMemorySize,
            OodleLZ_Decode_ThreadPhase threadPhase);

        /// <summary>
        /// Oodle 2.8 onwards. Relevant Info: http://cbloomrants.blogspot.com/2019/04/oodle-280-release.html?m=1
        /// </summary>
        [DllImport(LibraryName)]
        private static extern long OodleLZ_GetCompressedBufferSizeNeeded(
            OodleLZ_Compressor compressor,
            long rawSize);

        /// <summary>
        /// Oodle 2.6, which has no compressor argument.
        /// </summary>
        [DllImport(LibraryName, EntryPoint = "OodleLZ_GetCompressedBufferSizeNeeded")]
        private static extern long OodleLZ_GetCompressedBufferSizeNeeded_V6(long rawSize);

        /// <summary>
        /// Oodle 2.8 onwards. Relevant Info: http://cbloomrants.blogspot.com/2019/04/oodle-280-release.html?m=1
        /// </summary>
        [DllImport(LibraryName)]
        private static extern long OodleLZ_GetDecodeBufferSize(
            OodleLZ_Compressor compressor,
            long rawSize,
            [MarshalAs(UnmanagedType.Bool)] bool corruptionPossible);

        /// <summary>
        /// Oodle 2.6, which has no compressor argument.
        /// </summary>
        [DllImport(LibraryName, EntryPoint = "OodleLZ_GetDecodeBufferSize")]
        private static extern long OodleLZ_GetDecodeBufferSize_V6(
            long rawSize,
            [MarshalAs(UnmanagedType.Bool)] bool corruptionPossible);

        #endregion

        public enum OodleLZ_CompressionLevel : int
        {
            OodleLZ_CompressionLevel_None = 0,
            OodleLZ_CompressionLevel_SuperFast = 1,
            OodleLZ_CompressionLevel_VeryFast = 2,
            OodleLZ_CompressionLevel_Fast = 3,
            OodleLZ_CompressionLevel_Normal = 4,

            OodleLZ_CompressionLevel_Optimal1 = 5,
            OodleLZ_CompressionLevel_Optimal2 = 6,
            OodleLZ_CompressionLevel_Optimal3 = 7,
            OodleLZ_CompressionLevel_Optimal4 = 8,
            OodleLZ_CompressionLevel_Optimal5 = 9,

            OodleLZ_CompressionLevel_HyperFast1 = -1,
            OodleLZ_CompressionLevel_HyperFast2 = -2,
            OodleLZ_CompressionLevel_HyperFast3 = -3,
            OodleLZ_CompressionLevel_HyperFast4 = -4,

            OodleLZ_CompressionLevel_HyperFast = OodleLZ_CompressionLevel_HyperFast1,
            OodleLZ_CompressionLevel_Optimal = OodleLZ_CompressionLevel_Optimal2,
            OodleLZ_CompressionLevel_Max = OodleLZ_CompressionLevel_Optimal5,
            OodleLZ_CompressionLevel_Min = OodleLZ_CompressionLevel_HyperFast4,

            OodleLZ_CompressionLevel_Force32 = 0x40000000,
            OodleLZ_CompressionLevel_Invalid = OodleLZ_CompressionLevel_Force32
        }

        public enum OodleLZ_Compressor : int
        {
            OodleLZ_Compressor_Invalid = -1,
            OodleLZ_Compressor_None = 3,

            OodleLZ_Compressor_Kraken = 8,
            OodleLZ_Compressor_Leviathan = 13,
            OodleLZ_Compressor_Mermaid = 9,
            OodleLZ_Compressor_Selkie = 11,
            OodleLZ_Compressor_Hydra = 12,

            OodleLZ_Compressor_BitKnit = 10,
            OodleLZ_Compressor_LZB16 = 4,
            OodleLZ_Compressor_LZNA = 7,
            OodleLZ_Compressor_LZH = 0,
            OodleLZ_Compressor_LZHLW = 1,
            OodleLZ_Compressor_LZNIB = 2,
            OodleLZ_Compressor_LZBLW = 5,
            OodleLZ_Compressor_LZA = 6,

            OodleLZ_Compressor_Count = 14,
            OodleLZ_Compressor_Force32 = 0x40000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OodleLZ_CompressOptions
        {
            public uint verbosity;
            public int minMatchLen;
            [MarshalAs(UnmanagedType.Bool)]
            public bool seekChunkReset;
            public int seekChunkLen;
            public OodleLZ_Profile profile;
            public int dictionarySize;
            public int spaceSpeedTradeoffBytes;
            public int maxHuffmansPerChunk;
            [MarshalAs(UnmanagedType.Bool)]
            public bool sendQuantumCRCs;
            public int maxLocalDictionarySize;
            public int makeLongRangeMatcher;
            public int matchTableSizeLog2;
        }


        public enum OodleLZ_CheckCRC : int
        {
            OodleLZ_CheckCRC_No = 0,
            OodleLZ_CheckCRC_Yes = 1,
            OodleLZ_CheckCRC_Force32 = 0x40000000
        }

        public enum OodleLZ_Decode_ThreadPhase : int
        {
            OodleLZ_Decode_ThreadPhase1 = 1,
            OodleLZ_Decode_ThreadPhase2 = 2,
            OodleLZ_Decode_ThreadPhaseAll = 3,
            OodleLZ_Decode_Unthreaded = OodleLZ_Decode_ThreadPhaseAll
        }

        public enum OodleLZ_FuzzSafe : int
        {
            OodleLZ_FuzzSafe_No = 0,
            OodleLZ_FuzzSafe_Yes = 1
        }

        public enum OodleLZ_Profile : int
        {
            OodleLZ_Profile_Main = 0,
            OodleLZ_Profile_Reduced = 1,
            OodleLZ_Profile_Force32 = 0x40000000
        }

        public enum OodleLZ_Verbosity : int
        {
            OodleLZ_Verbosity_None = 0,
            OodleLZ_Verbosity_Minimal = 1,
            OodleLZ_Verbosity_Some = 2,
            OodleLZ_Verbosity_Lots = 3,
            OodleLZ_Verbosity_Force32 = 0x40000000
        }
    }
}