﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SoulsFormats
{
    /// <summary>
    /// The header file of the dvdbnd container format used to package all game files with hashed filenames.
    /// </summary>
    public class BHD5
    {
        /// <summary>
        /// Indicates the format of a dvdbnd.
        /// </summary>
        public enum Game
        {
            /// <summary>
            /// Dark Souls 1, both PC and console versions.
            /// <para>The first known format of this header.</para>
            /// </summary>
            DarkSouls1,

            /// <summary>
            /// Dark Souls 2 and Scholar of the First Sin on PC.
            /// <para>Includes AES encryption and salting.</para>
            /// </summary>
            DarkSouls2,

            /// <summary>
            /// Dark Souls 3 and Sekiro on PC.
            /// <para>Includes size fields that identify how big the archive or its files are without padding.</para>
            /// </summary>
            DarkSouls3,

            /// <summary>
            /// Elden Ring on PC.
            /// <para>Improves the size of many fields by making them 64-bit, changes the unpadded size fields to be 32-bit, and resorts some fields.</para>
            /// </summary>
            EldenRing,
        }

        /// <summary>
        /// Format the file should be written in.
        /// </summary>
        public Game Format { get; set; }

        /// <summary>
        /// Whether the header is big-endian.
        /// </summary>
        public bool BigEndian { get; set; }

        /// <summary>
        /// Unknown; possibly whether crypto is allowed? Offsets are present regardless.
        /// </summary>
        public bool Unk05 { get; set; }

        /// <summary>
        /// A salt used to calculate SHA hashes for file data.
        /// </summary>
        public string Salt { get; set; }

        /// <summary>
        /// Collections of files grouped by their hash value for faster lookup.
        /// </summary>
        public List<Bucket> Buckets { get; set; }

        /// <summary>
        /// Whether or not the current format version supports encryption.
        /// </summary>
        public bool EncryptionSupported
            => Format >= Game.DarkSouls2;

        /// <summary>
        /// Whether or not the current format version supports unpadded size fields.
        /// </summary>
        public bool UnpaddedSizeSupported
            => Format >= Game.DarkSouls3;

        /// <summary>
        /// Whether or not the current format version supports upgraded 64-bit fields for many things.
        /// </summary>
        public bool LongFieldsSupported
            => Format >= Game.DarkSouls3;

        /// <summary>
        /// Creates an empty BHD5.
        /// </summary>
        public BHD5(Game game)
        {
            Format = game;
            Salt = "";
            Buckets = new List<Bucket>();
        }

        #region Read

        /// <summary>
        /// Read a header from the given path, formatted for the given game. Must already be decrypted, if applicable.
        /// </summary>
        public static BHD5 Read(string path, Game game)
        {
            using (BinaryReaderEx br = new BinaryReaderEx(false, path))
            {
                return new BHD5(br, game);
            }
        }

        /// <summary>
        /// Read a header from the given bytes, formatted for the given game. Must already be decrypted, if applicable.
        /// </summary>
        public static BHD5 Read(byte[] bytes, Game game)
        {
            using (BinaryReaderEx br = new BinaryReaderEx(false, bytes))
            {
                return new BHD5(br, game);
            }
        }

        /// <summary>
        /// Read a header from the given stream, formatted for the given game. Must already be decrypted, if applicable.
        /// </summary>
        public static BHD5 Read(Stream stream, Game game)
        {
            using (BinaryReaderEx br = new BinaryReaderEx(false, stream, true))
            {
                return new BHD5(br, game);
            }
        }

        private BHD5(BinaryReaderEx br, Game game)
        {
            Format = game;

            br.AssertASCII("BHD5");
            BigEndian = br.AssertSByte(0, -1) == 0;
            br.BigEndian = BigEndian;
            Unk05 = br.ReadBoolean();
            br.AssertByte(0);
            br.AssertByte(0);
            br.AssertInt32(1);
            br.ReadInt32(); // File size
            int bucketCount = br.ReadInt32();
            int bucketsOffset = br.ReadInt32();

            if (game >= Game.DarkSouls2)
            {
                int saltLength = br.ReadInt32();
                Salt = br.ReadASCII(saltLength);
                // No padding
            }

            br.Position = bucketsOffset;
            Buckets = new List<Bucket>(bucketCount);
            for (int i = 0; i < bucketCount; i++)
                Buckets.Add(new Bucket(br, game));
        }

        #endregion

        #region Write

        /// <summary>
        /// Write a header to the given path.
        /// </summary>
        public void Write(string path)
        {
            using (BinaryWriterEx bw = new BinaryWriterEx(false, path))
            {
                Write(bw);
            }
        }

        /// <summary>
        /// Write a header to an array of bytes.
        /// </summary>
        public byte[] Write()
        {
            BinaryWriterEx bw = new BinaryWriterEx(false);
            Write(bw);
            return bw.FinishBytes();
        }

        /// <summary>
        /// Write a header to the given <see cref="Stream"/>.
        /// </summary>
        public void Write(Stream stream)
        {
            using (BinaryWriterEx bw = new BinaryWriterEx(false, stream, true))
            {
                Write(bw);
            }
        }

        private void Write(BinaryWriterEx bw)
        {
            bw.BigEndian = BigEndian;
            bw.WriteASCII("BHD5");
            bw.WriteSByte((sbyte)(BigEndian ? 0 : -1));
            bw.WriteBoolean(Unk05);
            bw.WriteByte(0);
            bw.WriteByte(0);
            bw.WriteInt32(1);
            bw.ReserveInt32("FileSize");
            bw.WriteInt32(Buckets.Count);
            bw.ReserveInt32("BucketsOffset");

            if (Format >= Game.DarkSouls2)
            {
                bw.WriteInt32(Salt.Length);
                bw.WriteASCII(Salt);
            }

            bw.FillInt32("BucketsOffset", (int)bw.Position);
            for (int i = 0; i < Buckets.Count; i++)
                Buckets[i].Write(bw, i);

            for (int i = 0; i < Buckets.Count; i++)
                Buckets[i].WriteFileHeaders(bw, Format, i);

            for (int i = 0; i < Buckets.Count; i++)
                for (int j = 0; j < Buckets[i].Count; j++)
                    Buckets[i][j].WriteHashAndKey(bw, Format, i, j);

            bw.FillInt32("FileSize", (int)bw.Position);
        }

        #endregion

        #region Is

        /// <summary>
        /// Whether or not the data appears to be a header file.
        /// </summary>
        public static bool IsHeader(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                if (fs.Length < 4)
                {
                    return false;
                }

                using (BinaryReaderEx br = new BinaryReaderEx(false, fs))
                {
                    return IsHeader(br);
                }
            }
        }

        /// <summary>
        /// Whether or not the data appears to be a header file.
        /// </summary>
        public static bool IsHeader(byte[] bytes)
        {
            if (bytes.Length < 4)
            {
                return false;
            }

            using (BinaryReaderEx br = new BinaryReaderEx(false, bytes))
            {
                return IsHeader(br);
            }
        }

        /// <summary>
        /// Whether or not the data appears to be a header file.
        /// </summary>
        public static bool IsHeader(Stream stream)
        {
            if ((stream.Length - stream.Position) < 4)
            {
                return false;
            }

            using (BinaryReaderEx br = new BinaryReaderEx(false, stream, true))
            {
                return IsHeader(br);
            }
        }

        /// <summary>
        /// Whether or not the data appears to be a header file.
        /// </summary>
        private static bool IsHeader(BinaryReaderEx br) => br.Remaining >= 4 && br.GetASCII(br.Position, 4) == "BHD5";

        /// <summary>
        /// Whether or not the data appears to be a data file.
        /// </summary>
        public static bool IsData(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                if (fs.Length < 4)
                {
                    return false;
                }

                using (BinaryReaderEx br = new BinaryReaderEx(false, fs))
                {
                    return IsData(br);
                }
            }
        }

        /// <summary>
        /// Whether or not the data appears to be a data file.
        /// </summary>
        public static bool IsData(byte[] bytes)
        {
            if (bytes.Length < 4)
            {
                return false;
            }

            using (BinaryReaderEx br = new BinaryReaderEx(false, bytes))
            {
                return IsData(br);
            }
        }

        /// <summary>
        /// Whether or not the data appears to be a header file.
        /// </summary>
        public static bool IsData(Stream stream)
        {
            if ((stream.Length - stream.Position) < 4)
            {
                return false;
            }

            using (BinaryReaderEx br = new BinaryReaderEx(false, stream, true))
            {
                return IsData(br);
            }
        }

        /// <summary>
        /// Whether or not the data appears to be a data file.
        /// </summary>
        private static bool IsData(BinaryReaderEx br)
        {
            if (br.Remaining < 4)
            {
                return false;
            }

            string magic = br.GetASCII(br.Position, 4);
            return magic == "BDF3" || magic == "BDF4";
        }

        #endregion

        #region Bucket

        /// <summary>
        /// A collection of files grouped by their hash.
        /// </summary>
        public class Bucket : List<FileHeader>
        {
            /// <summary>
            /// Creates an empty Bucket.
            /// </summary>
            public Bucket() : base() { }

            internal Bucket(BinaryReaderEx br, Game game) : base()
            {
                int fileHeaderCount = br.ReadInt32();
                int fileHeadersOffset = br.ReadInt32();
                Capacity = fileHeaderCount;

                br.StepIn(fileHeadersOffset);
                {
                    for (int i = 0; i < fileHeaderCount; i++)
                        Add(new FileHeader(br, game));
                }
                br.StepOut();
            }

            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt32(Count);
                bw.ReserveInt32($"FileHeadersOffset{index}");
            }

            internal void WriteFileHeaders(BinaryWriterEx bw, Game game, int index)
            {
                bw.FillInt32($"FileHeadersOffset{index}", (int)bw.Position);
                for (int i = 0; i < Count; i++)
                    this[i].Write(bw, game, index, i);
            }
        }

        #endregion

        #region FileHeader

        /// <summary>
        /// Information about an individual file in the dvdbnd.
        /// </summary>
        public class FileHeader
        {
            /// <summary>
            /// Hash of the full file path using From's algorithm found in SFUtil.FromPathHash.
            /// </summary>
            public ulong FileNameHash { get; set; }

            /// <summary>
            /// Full size of the file data in the BDT.
            /// </summary>
            public int PaddedFileSize { get; set; }

            /// <summary>
            /// File size after decryption; only included in DS3.
            /// </summary>
            public long UnpaddedFileSize { get; set; }

            /// <summary>
            /// Beginning of file data in the BDT.
            /// </summary>
            public long FileOffset { get; set; }

            /// <summary>
            /// Hashing information for this file.
            /// </summary>
            public SHAHash SHAHash { get; set; }

            /// <summary>
            /// Encryption information for this file.
            /// </summary>
            public AESKey AESKey { get; set; }

            /// <summary>
            /// Creates a FileHeader with default values.
            /// </summary>
            public FileHeader() { }

            internal FileHeader(BinaryReaderEx br, Game game)
            {
                long shaHashOffset = 0;
                long aesKeyOffset = 0;
                UnpaddedFileSize = -1;

                if (game >= Game.EldenRing)
                {
                    FileNameHash = br.ReadUInt64();
                    PaddedFileSize = br.ReadInt32();
                    UnpaddedFileSize = br.ReadInt32();
                    FileOffset = br.ReadInt64();
                    shaHashOffset = br.ReadInt64();
                    aesKeyOffset = br.ReadInt64();
                }
                else
                {
                    FileNameHash = br.ReadUInt32();
                    PaddedFileSize = br.ReadInt32();
                    FileOffset = br.ReadInt64();

                    if (game >= Game.DarkSouls2)
                    {
                        shaHashOffset = br.ReadInt64();
                        aesKeyOffset = br.ReadInt64();
                    }

                    if (game >= Game.DarkSouls3)
                    {
                        UnpaddedFileSize = br.ReadInt64();
                    }
                }

                if (shaHashOffset != 0)
                {
                    br.StepIn(shaHashOffset);
                    {
                        SHAHash = new SHAHash(br);
                    }
                    br.StepOut();
                }

                if (aesKeyOffset != 0)
                {
                    br.StepIn(aesKeyOffset);
                    {
                        AESKey = new AESKey(br);
                    }
                    br.StepOut();
                }
            }

            internal void Write(BinaryWriterEx bw, Game game, int bucketIndex, int fileIndex)
            {
                if (game >= Game.EldenRing)
                {
                    bw.WriteUInt64(FileNameHash);
                    bw.WriteInt32(PaddedFileSize);
                    bw.WriteInt32((int)UnpaddedFileSize);
                    bw.WriteInt64(FileOffset);
                    bw.ReserveInt64($"AESKeyOffset{bucketIndex}:{fileIndex}");
                    bw.ReserveInt64($"SHAHashOffset{bucketIndex}:{fileIndex}");
                }
                else
                {
                    bw.WriteUInt32((uint)FileNameHash);
                    bw.WriteInt32(PaddedFileSize);
                    bw.WriteInt64(FileOffset);

                    if (game >= Game.DarkSouls2)
                    {
                        bw.ReserveInt64($"SHAHashOffset{bucketIndex}:{fileIndex}");
                        bw.ReserveInt64($"AESKeyOffset{bucketIndex}:{fileIndex}");
                    }

                    if (game >= Game.DarkSouls3)
                    {
                        bw.WriteInt64(UnpaddedFileSize);
                    }
                }
            }

            internal void WriteHashAndKey(BinaryWriterEx bw, Game game, int bucketIndex, int fileIndex)
            {
                if (game >= Game.DarkSouls2)
                {
                    if (SHAHash == null)
                    {
                        bw.FillInt64($"SHAHashOffset{bucketIndex}:{fileIndex}", 0);
                    }
                    else
                    {
                        bw.FillInt64($"SHAHashOffset{bucketIndex}:{fileIndex}", bw.Position);
                        SHAHash.Write(bw);
                    }

                    if (AESKey == null)
                    {
                        bw.FillInt64($"AESKeyOffset{bucketIndex}:{fileIndex}", 0);
                    }
                    else
                    {
                        bw.FillInt64($"AESKeyOffset{bucketIndex}:{fileIndex}", bw.Position);
                        AESKey.Write(bw);
                    }
                }
            }

            /// <summary>
            /// Read and decrypt (if necessary) file data from the BDT.
            /// </summary>
            public byte[] ReadFile(FileStream bdtStream)
            {
                byte[] bytes = new byte[PaddedFileSize];
                bdtStream.Position = FileOffset;
                bdtStream.Read(bytes, 0, PaddedFileSize);
                AESKey?.Decrypt(bytes);
                return bytes;
            }
        }

        #endregion

        #region SHAHash

        /// <summary>
        /// Hash information for a file in the dvdbnd.
        /// </summary>
        public class SHAHash
        {
            /// <summary>
            /// 32-byte salted SHA hash.
            /// </summary>
            public byte[] Hash { get; set; }

            /// <summary>
            /// Hashed sections of the file.
            /// </summary>
            public List<Range> Ranges { get; set; }

            /// <summary>
            /// Creates a SHAHash with default values.
            /// </summary>
            public SHAHash()
            {
                Hash = new byte[32];
                Ranges = new List<Range>();
            }

            internal SHAHash(BinaryReaderEx br)
            {
                Hash = br.ReadBytes(32);
                int rangeCount = br.ReadInt32();
                Ranges = new List<Range>(rangeCount);
                for (int i = 0; i < rangeCount; i++)
                    Ranges.Add(new Range(br));
            }

            internal void Write(BinaryWriterEx bw)
            {
                if (Hash.Length != 32)
                    throw new InvalidDataException("SHA hash must be 32 bytes long.");

                bw.WriteBytes(Hash);
                bw.WriteInt32(Ranges.Count);
                foreach (Range range in Ranges)
                    range.Write(bw);
            }
        }

        #endregion

        #region AESKey

        /// <summary>
        /// Encryption information for a file in the dvdbnd.
        /// </summary>
        public class AESKey
        {
            private static AesManaged AES = new AesManaged() { Mode = CipherMode.ECB, Padding = PaddingMode.None, KeySize = 128 };

            /// <summary>
            /// 16-byte encryption key.
            /// </summary>
            public byte[] Key { get; set; }

            /// <summary>
            /// Encrypted sections of the file.
            /// </summary>
            public List<Range> Ranges { get; set; }

            /// <summary>
            /// Creates an AESKey with default values.
            /// </summary>
            public AESKey()
            {
                Key = new byte[16];
                Ranges = new List<Range>();
            }

            internal AESKey(BinaryReaderEx br)
            {
                Key = br.ReadBytes(16);
                int rangeCount = br.ReadInt32();
                Ranges = new List<Range>(rangeCount);
                for (int i = 0; i < rangeCount; i++)
                    Ranges.Add(new Range(br));
            }

            internal void Write(BinaryWriterEx bw)
            {
                if (Key.Length != 16)
                    throw new InvalidDataException("AES key must be 16 bytes long.");

                bw.WriteBytes(Key);
                bw.WriteInt32(Ranges.Count);
                foreach (Range range in Ranges)
                    range.Write(bw);
            }

            /// <summary>
            /// Decrypt file data in-place.
            /// </summary>
            public void Decrypt(byte[] bytes)
            {
                using (ICryptoTransform decryptor = AES.CreateDecryptor(Key, new byte[16]))
                {
                    foreach (Range range in Ranges.Where(r => r.StartOffset != -1 && r.EndOffset != -1 && r.StartOffset != r.EndOffset))
                    {
                        int start = (int)range.StartOffset;
                        int count = (int)(range.EndOffset - range.StartOffset);
                        decryptor.TransformBlock(bytes, start, count, bytes, start);
                    }
                }
            }
        }

        #endregion

        #region Range

        /// <summary>
        /// Indicates a hashed or encrypted section of a file.
        /// </summary>
        public struct Range
        {
            /// <summary>
            /// The beginning of the range, inclusive.
            /// </summary>
            public long StartOffset { get; set; }

            /// <summary>
            /// The end of the range, exclusive.
            /// </summary>
            public long EndOffset { get; set; }

            /// <summary>
            /// Creates a Range with the given values.
            /// </summary>
            public Range(long startOffset, long endOffset)
            {
                StartOffset = startOffset;
                EndOffset = endOffset;
            }

            internal Range(BinaryReaderEx br)
            {
                StartOffset = br.ReadInt64();
                EndOffset = br.ReadInt64();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt64(StartOffset);
                bw.WriteInt64(EndOffset);
            }
        }

        #endregion
    }
}
