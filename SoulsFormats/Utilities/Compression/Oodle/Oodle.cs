using SoulsFormats.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SoulsFormats
{
    public enum OodleVersion
    {
        Oodle9,
        Oodle8,
        Oodle6
    }
    /// <summary>
    /// Class for handling the Oodle compression library included with FromSoftware games.
    /// </summary>
    public class Oodle
    {
        public static Dictionary<OodleVersion, IntPtr> OodlePtrs = new Dictionary<OodleVersion, IntPtr>
        {
            {OodleVersion.Oodle9, IntPtr.Zero},
            {OodleVersion.Oodle8, IntPtr.Zero},
            {OodleVersion.Oodle6, IntPtr.Zero},
        };
        /// <summary>
        /// Pointer to Oodle version 9, provided by Nightreign.
        /// </summary>
        public static IntPtr Oodle9Ptr
        {
            get => OodlePtrs[OodleVersion.Oodle9];
            set => OodlePtrs[OodleVersion.Oodle9] = value;
        }

        /// <summary>
        /// Pointer to Oodle version 8, provided by Armored Core 6.
        /// </summary>
        public static IntPtr Oodle8Ptr
        {
            get => OodlePtrs[OodleVersion.Oodle8];
            set => OodlePtrs[OodleVersion.Oodle8] = value;
        }
        /// <summary>
        /// Pointer to Oodle version 6, provided by ELDEN RING and Sekiro.
        /// </summary>
        public static IntPtr Oodle6Ptr
        {
            get => OodlePtrs[OodleVersion.Oodle6];
            set => OodlePtrs[OodleVersion.Oodle6] = value;
        }

        public static IOodleCompressor GetOodleCompressor()
        {
            if (Oodle9Ptr != IntPtr.Zero)
            {
                return new Oodle29();
            }
            if (Oodle8Ptr != IntPtr.Zero)
            {
                return new Oodle28();
            }
            if (Oodle6Ptr != IntPtr.Zero)
            {
                return new Oodle26();
            }

            string oodle9ErrorMessage, oodle8ErrorMessage, oodle6ErrorMessage;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr oodle9Ptr = NativeLibrary.LoadLibrary("oo2core_9_win64");
                if (oodle9Ptr != IntPtr.Zero)
                {
                    Oodle9Ptr = oodle9Ptr;
                    return new Oodle29();
                }
                oodle9ErrorMessage = NativeLibrary.GetLastError();

                IntPtr oodle8Ptr = NativeLibrary.LoadLibrary("oo2core_8_win64");
                if (oodle8Ptr != IntPtr.Zero)
                {
                    Oodle8Ptr = oodle8Ptr;
                    return new Oodle28();
                }
                oodle8ErrorMessage = NativeLibrary.GetLastError();

                IntPtr oodle6Ptr = NativeLibrary.LoadLibrary("oo2core_6_win64");
                if (oodle6Ptr != IntPtr.Zero)
                {
                    Oodle6Ptr = oodle6Ptr;
                    return new Oodle26();
                }
                oodle6ErrorMessage = NativeLibrary.GetLastError();
            }
            else
            {
                IntPtr oodle9Ptr = NativeLibrary.LoadLibrary("liboo2corelinux64.so.9");
                if (oodle9Ptr != IntPtr.Zero)
                {
                    Oodle9Ptr = oodle9Ptr;
                    return new Oodle29();
                }
                oodle9ErrorMessage = NativeLibrary.GetLastError();

                IntPtr oodle8Ptr = NativeLibrary.LoadLibrary("liboo2corelinux64.so.8");
                if (oodle8Ptr != IntPtr.Zero)
                {
                    Oodle8Ptr = oodle8Ptr;
                    return new Oodle28();
                }
                oodle8ErrorMessage = NativeLibrary.GetLastError();

                IntPtr oodle6Ptr = NativeLibrary.LoadLibrary("liboo2corelinux64.so.6");
                if (oodle6Ptr != IntPtr.Zero)
                {
                    Oodle6Ptr = oodle6Ptr;
                    return new Oodle26();
                }
                oodle6ErrorMessage = NativeLibrary.GetLastError();
            }

            var message = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "Please copy oo2core_9_win64.dll, oo2core_8_win64.dll or oo2core_6_win64.dll into the program directory\n"
                : "Please copy liboo2corelinux64.so.9, liboo2corelinux64.so.8 or liboo2corelinux64.so.6 into the program directory\n";

            throw new NoOodleFoundException($"Could not find a supported version of the Oodle compression library.\n"
                                            + message
                                            + $"Last Error Oodle 9: {oodle9ErrorMessage}\n"
                                            + $"Last Error Oodle 8: {oodle8ErrorMessage}\n"
                                            + $"Last Error Oodle 6: {oodle6ErrorMessage}\n"
            );
        }

        public IntPtr GetOodlePtr()
        {
            foreach (IntPtr oodlePtr in OodlePtrs.Values)
            {
                if (oodlePtr != IntPtr.Zero)
                    return oodlePtr;
            }

            return IntPtr.Zero;
        }

        private static int _oodleVersion = -1;
        public static IOodleCompressor GetOodleCompressor2()
        {
            switch (_oodleVersion)
            {
                case 9:
                    return new Oodle29();
                case 8:
                    return new Oodle28();
                case 6:
                    return new Oodle26();
            }

            List<string> errors = new List<string>();
            try
            {
                Oodle26.OodleLZ_CompressOptions_GetDefault();
                _oodleVersion = 9;
                return new Oodle29();
            }
            catch (EntryPointNotFoundException ex)
            {
                errors.Add(ex.Message);
            }

            try
            {
                Oodle28.OodleLZ_CompressOptions_GetDefault();
                _oodleVersion = 8;
                return new Oodle28();
            }
            catch (EntryPointNotFoundException ex)
            {
                errors.Add(ex.Message);
            }

            try
            {
                Oodle29.OodleLZ_CompressOptions_GetDefault();
                _oodleVersion = 6;
                return new Oodle26();
            }
            catch (EntryPointNotFoundException ex)
            {

                var message = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "Please copy oo2core_9_win64.dll, oo2core_8_win64.dll or oo2core_6_win64.dll into the program directory\n"
                    : "Please copy liboo2corelinux64.so.9, liboo2corelinux64.so.8 or liboo2corelinux64.so.6 into the program directory\n";

                throw new NoOodleFoundException($"Could not find a supported version of the Oodle compression library.\n"
                                                + message
                                                + $"{String.Join("\n", errors)}\n"
                                                + $"{ex.Message}"
                );
            }
        }

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
