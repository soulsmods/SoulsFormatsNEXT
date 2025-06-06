﻿using SoulsFormats.Utilities;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SoulsFormats
{
    /// <summary>
    /// Common format information for BND3, BXF3, BND4, and BXF4.
    /// </summary>
    public static class Binder
    {
        /// <summary>
        /// Flags indicating the features supported by a binder.
        /// </summary>
        [Flags]
        public enum Format : byte
        {
            /// <summary>
            /// Minimal file information.
            /// </summary>
            None = 0,

            /// <summary>
            /// File is big-endian regardless of the big-endian byte.
            /// </summary>
            BigEndian = 0b0000_0001,

            /// <summary>
            /// Files have ID numbers.
            /// </summary>
            IDs = 0b0000_0010,

            /// <summary>
            /// Files have name strings; Names2 may or may not be set. Perhaps the distinction is related to whether it's a full path or just the filename?
            /// </summary>
            Names1 = 0b0000_0100,

            /// <summary>
            /// Files have name strings; Names1 may or may not be set.
            /// </summary>
            Names2 = 0b0000_1000,

            /// <summary>
            /// File data offsets are 64-bit.
            /// </summary>
            LongOffsets = 0b0001_0000,

            /// <summary>
            /// Files may be compressed.
            /// </summary>
            Compression = 0b0010_0000,

            /// <summary>
            /// Unknown.
            /// </summary>
            Flag6 = 0b0100_0000,

            /// <summary>
            /// Unknown.
            /// </summary>
            Flag7 = 0b1000_0000,
        }

        /// <summary>
        /// Reads a binder format byte, reversed according to the big-endian setting.
        /// </summary>
        public static Format ReadFormat(BinaryReaderEx br, bool bitBigEndian)
        {
            byte rawFormat = br.ReadByte();
            bool reverse = bitBigEndian || (rawFormat & 1) != 0 && (rawFormat & 0b1000_0000) == 0;
            return (Format)(reverse ? rawFormat : EndianHelper.ReverseBits(rawFormat));
        }

        /// <summary>
        /// Writes a binder format byte, reversed according to the big-endian setting.
        /// </summary>
        public static void WriteFormat(BinaryWriterEx bw, bool bitBigEndian, Format format)
        {
            bool reverse = bitBigEndian || (ForceBigEndian(format) && (format & Format.Flag6) != 0);
            byte rawFormat = reverse ? (byte)format : EndianHelper.ReverseBits((byte)format);
            bw.WriteByte(rawFormat);
        }

        /// <summary>
        /// Whether the file is big-endian regardless of the big-endian byte.
        /// </summary>
        public static bool ForceBigEndian(Format format) => (format & Format.BigEndian) != 0;

        /// <summary>
        /// Whether the format includes file ID numbers.
        /// </summary>
        public static bool HasIDs(Format format) => (format & Format.IDs) != 0;

        /// <summary>
        /// Whether the format includes file names.
        /// </summary>
        public static bool HasNames(Format format) => (format & (Format.Names1 | Format.Names2)) != 0;

        /// <summary>
        /// Whether the format uses 64-bit data offsets.
        /// </summary>
        public static bool HasLongOffsets(Format format) => (format & Format.LongOffsets) != 0;

        /// <summary>
        /// Whether the format supports file compression.
        /// </summary>
        public static bool HasCompression(Format format) => (format & Format.Compression) != 0;

        /// <summary>
        /// Whether the format has flag 6 set.
        /// </summary>
        public static bool HasFlag6(Format format) => (format & Format.Flag6) != 0;

        /// <summary>
        /// Whether the format has flag 7 set.
        /// </summary>
        public static bool HasFlag7(Format format) => (format & Format.Flag7) != 0;

        /// <summary>
        /// Computes the size of each file header for BND4/BXF4.
        /// </summary>
        public static long GetBND4FileHeaderSize(Format format)
        {
            return 0x10
                + (HasLongOffsets(format) ? 8 : 4)
                + (HasCompression(format) ? 8 : 0)
                + (HasIDs(format) ? 4 : 0)
                + (HasNames(format) ? 4 : 0)
                + (format == Format.Names1 ? 8 : 0);
        }

        /// <summary>
        /// Flags indicating features for specific files.
        /// </summary>
        [Flags]
        public enum FileFlags : byte
        {
            /// <summary>
            /// No flags set.
            /// </summary>
            None = 0,

            /// <summary>
            /// File is compressed.
            /// </summary>
            Compressed = 0b0000_0001,

            /// <summary>
            /// Unknown; seems to be standard for all files.
            /// </summary>
            Flag1 = 0b0000_0010,

            /// <summary>
            /// Unknown.
            /// </summary>
            Flag2 = 0b0000_0100,

            /// <summary>
            /// Unknown.
            /// </summary>
            Flag3 = 0b0000_1000,

            /// <summary>
            /// Unknown.
            /// </summary>
            Flag4 = 0b0001_0000,

            /// <summary>
            /// Unknown.
            /// </summary>
            Flag5 = 0b0010_0000,

            /// <summary>
            /// Unknown.
            /// </summary>
            Flag6 = 0b0100_0000,

            /// <summary>
            /// Unknown.
            /// </summary>
            Flag7 = 0b1000_0000,
        }

        /// <summary>
        /// Reads a file flags byte, reversed according to the big-endian setting.
        /// </summary>
        public static FileFlags ReadFileFlags(BinaryReaderEx br, bool bitBigEndian)
        {
            bool reverse = bitBigEndian;
            byte rawFlags = br.ReadByte();
            return (FileFlags)(reverse ? rawFlags : EndianHelper.ReverseBits(rawFlags));
        }

        /// <summary>
        /// Writes a file flags byte, reversed according to the big-endian setting.
        /// </summary>
        public static void WriteFileFlags(BinaryWriterEx bw, bool bitBigEndian, FileFlags flags)
        {
            bool reverse = bitBigEndian;
            byte rawFlags = reverse ? (byte)flags : EndianHelper.ReverseBits((byte)flags);
            bw.WriteByte(rawFlags);
        }

        /// <summary>
        /// Whether the file data is compressed.
        /// </summary>
        public static bool IsCompressed(FileFlags flags) => (flags & FileFlags.Compressed) != 0;

        private static readonly Regex timestampRx = new Regex(@"(\d\d)(\w)(\d+)(\w)(\d+)");

        /// <summary>
        /// Converts a BND/BXF timestamp string to a DateTime object.
        /// </summary>
        public static DateTime BinderTimestampToDate(string timestamp)
        {
            Match match = timestampRx.Match(timestamp);
            if (!match.Success)
                throw new InvalidDataException("Unrecognized timestamp format.");

            int year = Int32.Parse(match.Groups[1].Value) + 2000;
            int month = match.Groups[2].Value[0] - 'A';
            int day = Int32.Parse(match.Groups[3].Value);
            int hour = match.Groups[4].Value[0] - 'A';
            int minute = Int32.Parse(match.Groups[5].Value);

            return new DateTime(year, month, day, hour, minute, 0);
        }

        /// <summary>
        /// Converts a DateTime object to a BND/BXF timestamp string.
        /// </summary>
        public static string DateToBinderTimestamp(DateTime dateTime)
        {
            int year = dateTime.Year - 2000;
            if (year < 0 || year > 99)
                throw new InvalidDataException("BND timestamp year must be between 2000 and 2099 inclusive.");

            char month = (char)(dateTime.Month + 'A');
            int day = dateTime.Day;
            char hour = (char)(dateTime.Hour + 'A');
            int minute = dateTime.Minute;

            return $"{year:D2}{month}{day}{hour}{minute}".PadRight(8, '\0');
        }
    }
}
