using SoulsFormats.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoulsFormats
{
    /// <summary>
    /// Class for handling the Oodle compression library included with FromSoftware games.
    /// </summary>
    public static class Oodle
    {
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

        /// <summary>
        /// Directories to look for the Oodle library in.
        /// </summary>
        public static List<string> SearchPaths { get; } = [];

        public static readonly Lazy<IOodleCompressor> Default = new(() => Load(SearchPaths));

        // Oodle versions are (46 << 24) | (major << 16) | (minor << 8) | sizeof(OodleLZ_SeekTable),
        internal static uint Oodle2Version(uint version) => version >> 16 & 0xFF;

        public static IOodleCompressor Load() => Load(SearchPaths);

        public static IOodleCompressor Load(IEnumerable<string> searchPaths)
        {
            var exceptions = new List<Exception>();

            foreach (string name in Candidates)
            {
                foreach (string path in searchPaths.Select(directory => Path.Combine(directory, name)).Append(name))
                {
                    try
                    {
                        return OodleCompressor.Load(path);
                    }
                    catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                    {
                        exceptions.Add(ex);
                    }
                }
            }

            throw new NoOodleFoundException(
                "Could not find a supported version of the Oodle compression library.\n"
                + $"Please copy {Candidates[0]}, {Candidates[1]} or {Candidates[2]} into the program directory, or add the directory containing one to Oodle.SearchPaths",
                new AggregateException(exceptions));
        }

        public static byte[] Compress(byte[] source, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level)
            => Default.Value.Compress(source, compressor, level);

        public static byte[] Decompress(byte[] source, long uncompressedSize)
            => Default.Value.Decompress(source, uncompressedSize);

        /// <inheritdoc cref="Load()"/>
        [Obsolete("Use Oodle.Load.")]
        public static IOodleCompressor GetOodleCompressor() => Load();

        /// <inheritdoc cref="Load()"/>
        [Obsolete("Use Oodle.Load.")]
        public static IOodleCompressor GetOodleCompressor2() => Load();

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
            [MarshalAs(UnmanagedType.Bool)] public bool seekChunkReset;
            public int seekChunkLen;
            public OodleLZ_Profile profile;
            public int dictionarySize;
            public int spaceSpeedTradeoffBytes;
            public int maxHuffmansPerChunk;
            [MarshalAs(UnmanagedType.Bool)] public bool sendQuantumCRCs;
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