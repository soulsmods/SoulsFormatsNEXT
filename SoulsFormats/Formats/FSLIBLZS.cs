using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace SoulsFormats
{
    /// <summary>
    /// A compression format used in ACLR and ACNB.<br/>
    /// Support currently incomplete.
    /// </summary>
    public class FSLIBLZS
    {
        #region Is

        internal static bool Is(BinaryReaderEx br)
        {
            if (br.Stream.Length < 4)
                return false;

            string magic = br.GetASCII(0, 8);
            return magic == "fsliblzs" || magic == "fsliblzs";
        }

        /// <summary>
        /// Returns true if the bytes appear to be an fsliblzs file.
        /// </summary>
        public static bool Is(byte[] bytes)
        {
            var br = new BinaryReaderEx(true, bytes);
            return Is(br);
        }

        /// <summary>
        /// Returns true if the file appears to be an fsliblzs file.
        /// </summary>
        public static bool Is(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                var br = new BinaryReaderEx(true, stream);
                return Is(br);
            }
        }

        #endregion

        #region Decompress

        /// <summary>
        /// Decompress an <see cref="FSLIBLZS"/> from the file on the specified path.
        /// </summary>
        /// <param name="path">The path of the file to decompress.</param>
        /// <returns>The decompressed file as an array of bytes.</returns>
        public static byte[] Decompress(string path)
        {
            using var br = new BinaryReaderEx(false, path);
            return Read(br);
        }

        /// <summary>
        /// Decompress an <see cref="FSLIBLZS"/> from the specified bytes.
        /// </summary>
        /// <param name="bytes">The bytes to decompress.</param>
        /// <returns>The decompressed bytes.</returns>
        public static byte[] Decompress(byte[] bytes)
        {
            using var br = new BinaryReaderEx(false, bytes);
            return Read(br);
        }

        #endregion

        #region Compress

        /// <summary>
        /// Compress the file on the specified path to <see cref="FSLIBLZS"/>.
        /// </summary>
        /// <param name="path">The path of the file to compress.</param>
        /// <returns>The <see cref="FSLIBLZS"/> as a byte array.</returns>
        public static byte[] Compress(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var bw = new BinaryWriterEx(false);
            Write(bw, bytes);
            return bw.FinishBytes();
        }

        /// <summary>
        /// Compress the specified bytes to <see cref="FSLIBLZS"/>.
        /// </summary>
        /// <param name="bytes">The bytes to compress.</param>
        /// <returns>The <see cref="FSLIBLZS"/> as a byte array.</returns>
        public static byte[] Compress(byte[] bytes)
        {
            using var bw = new BinaryWriterEx(false);
            Write(bw, bytes);
            return bw.FinishBytes();
        }

        #endregion

        #region Read

        /// <summary>
        /// Read and decompress an <see cref="FSLIBLZS"/>.
        /// </summary>
        internal static byte[] Read(BinaryReaderEx br)
        {
            br.BigEndian = false;
            br.AssertASCII("fsliblzs");
            br.AssertInt32(0);
            br.AssertInt32(0);
            int compressedSize = br.ReadInt32();
            br.AssertInt32(1);
            br.AssertInt32(0);
            br.AssertInt32(0);

            // Buffer header?
            br.BigEndian = true;
            br.AssertInt16(1);
            br.AssertInt16(0);
            int decompressedSize = br.ReadInt32();
            int isNotCompressed = br.AssertInt32(0, 1);
            br.BigEndian = false;

            const int headerSize = 44;
            byte[] compressedBytes = br.ReadBytes(compressedSize - headerSize);
            if (isNotCompressed == 1)
            {
                return compressedBytes;
            }

            return DecompressFsLzss(compressedBytes, decompressedSize);
        }

        #endregion

        #region Write

        /// <summary>
        /// Write and compress an <see cref="FSLIBLZS"/>.
        /// </summary>
        internal static void Write(BinaryWriterEx bw, byte[] bytes)
        {
            bool isNotCompressed = false;
            byte[] compressedBytes = CompressFsLzss(bytes);
            if (compressedBytes.Length > bytes.Length)
            {
                isNotCompressed = true;
            }

            bw.BigEndian = false;
            bw.WriteASCII("fsliblzs");
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.ReserveInt32("CompressedSize");
            bw.WriteInt32(1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);

            // Buffer header?
            bw.BigEndian = true;
            bw.WriteInt16(1);
            bw.WriteInt16(0);
            bw.WriteInt32(bytes.Length); // decompressedSize
            bw.WriteInt32(isNotCompressed ? 1 : 0); // isNotCompressed
            bw.BigEndian = false;

            if (isNotCompressed)
            {
                bw.WriteBytes(bytes);
            }
            else
            {
                bw.WriteBytes(compressedBytes);
            }

            bw.FillInt32("CompressedSize", (int)bw.Length);
        }

        #endregion

        #region Decompression Algorithm

        private static byte[] DecompressFsLzss(byte[] compressedBytes, int decompressedSize)
        {
            // Credit for this function goes to rabattini on the ResHax discord server who first wrote it in C++.
            // It has been rewritten for C# here.

            var decompressedBytes = new List<byte>(decompressedSize);

            byte flags = compressedBytes[0];
            int i = 1;
            ushort mask = 1;

            int windowBase = 0;
            bool firstAlign = true;

            void MaybeSlide(int u)
            {
                if ((decompressedBytes.Count - windowBase) > 0x0FFF)
                {
                    windowBase += u;
                    if (firstAlign)
                    {
                        if (decompressedBytes.Count >= 0x1000)
                            windowBase = decompressedBytes.Count - 0x1000;
                        else
                            windowBase = 0;

                        firstAlign = false;
                    }
                }
            }

            while (i < compressedBytes.Length && decompressedBytes.Count < decompressedSize)
            {
                // Compare the flags and mask to see if we are back referencing older original bytes, or copying literal ones next
                bool isBackReference = (flags & (byte)mask) != 0; // 0 literal, 1 back reference

                int u = 0;
                if (!isBackReference)
                {
                    // Literal
                    // This means the next byte is not back referencing, copy it directly.

                    decompressedBytes.Add(compressedBytes[i]);
                    i += 1;
                    u = 1;
                }
                else
                {
                    // Back reference
                    // This means the next bytes are back referencing older original bytes
                    // Copy the necessary bytes

                    if ((i + 1) >= compressedBytes.Length)
                        break;

                    byte b0 = compressedBytes[i];
                    byte b1 = compressedBytes[i + 1];
                    i += 2;

                    uint offset = ((uint)b0 << 4) + ((uint)b1 >> 4);
                    uint ln = (uint)(b1 & 0x0F);
                    if (ln == 0)
                        break;

                    u = (int)(ln + 1);

                    int src = windowBase + (int)offset;
                    for (int k = 0; k < u && decompressedBytes.Count < decompressedSize; ++k)
                    {
                        int index = src + k;
                        if (index >= decompressedBytes.Count)
                            throw new InvalidDataException($"Bad back reference: {index} >= {decompressedBytes.Count} (overlap/read-before-write mismatch).");

                        decompressedBytes.Add(decompressedBytes[index]);
                    }
                }

                // Slide if we need to
                MaybeSlide(u);

                mask <<= 1;
                if (mask == 0x100)
                {
                    if (i >= compressedBytes.Length)
                        break;

                    flags = compressedBytes[i];
                    i += 1;
                    mask = 1;
                }
            }

            return decompressedBytes.ToArray();
        }

        #endregion

        #region Compression Algorithm

        private static unsafe byte[] CompressFsLzss(byte[] decompressedBytes)
        {
            // TODO: Have someone who knows what they are doing rewrite this.
            byte[] compressed = new byte[decompressedBytes.Length * 3];
            fixed (byte* pOriginal = &decompressedBytes[0])
            fixed (byte* pCompressed = &compressed[0])
            {
                byte* pEnd = fsliblzs_compress(pCompressed, pOriginal, decompressedBytes.Length);
                int length = (int)(pEnd - pCompressed);
                compressed = compressed[..length];
            }

            return compressed;
        }

        static unsafe byte* fsliblzs_compress(byte* compressed_start, byte* original_start, int size_original)
        {
            int compress_zone_size;
            byte flag1;
            byte flag2;
            int unk1;
            int unk2;
            int unk3;
            byte* flag_pos;
            byte* compressed_pos;
            byte* stored_compress_zone_pos;
            byte* stored_original_pos;
            byte* compress_zone_pos;
            byte* compress_zone_start;
            byte* original_pos;

            original_pos = original_start;
            compressed_pos = compressed_start;
            flag1 = 0;
            flag2 = 0;
            flag_pos = (byte*)0;
            do
            {
                flag1 = (byte)(flag1 << 1);
                if (flag1 == 0)
                {
                    if (flag_pos != (byte*)0)
                    {
                        *flag_pos = flag2;
                    }
                    flag_pos = compressed_pos;
                    flag1 = 1;
                    flag2 = 0;
                    compressed_pos = compressed_pos + 1;
                }
                compress_zone_start = original_pos + -0x1000;
                if (compress_zone_start < original_start)
                {
                    compress_zone_start = original_start;
                }
                compress_zone_pos = compress_zone_start;
                unk3 = 0;
                unk2 = 0;
                while (compress_zone_pos < original_pos)
                {
                    stored_original_pos = original_pos;
                    stored_compress_zone_pos = compress_zone_pos;
                    compress_zone_size = (int)compress_zone_pos - (int)compress_zone_start;
                    unk1 = -1;
                    compress_zone_pos = compress_zone_pos + 1;
                    while (*stored_original_pos == *stored_compress_zone_pos)
                    {
                        unk1 = unk1 + 1;
                        stored_original_pos = stored_original_pos + 1;
                        stored_compress_zone_pos = stored_compress_zone_pos + 1;
                        if ((unk1 == 15) || (original_start + size_original < stored_original_pos)) break;
                    }
                    if (unk2 < unk1)
                    {
                        unk2 = unk1;
                        unk3 = compress_zone_size;
                    }
                }
                if (original_start + size_original <= original_pos)
                {
                    *compressed_pos = 0;
                    compressed_pos[1] = 0;
                    if (flag_pos != (byte*)0)
                    {
                        *flag_pos = (byte)(flag2 | flag1);
                    }
                    return compressed_pos;
                }
                if (unk2 < 1)
                {
                    *compressed_pos = *original_pos;
                    compressed_pos = compressed_pos + 1;
                    original_pos = original_pos + 1;
                }
                else
                {
                    *compressed_pos = (byte)(unk3 >> 4);
                    compressed_pos[1] = (byte)((byte)unk2 & 0xf | (byte)(unk3 << 4));
                    flag2 = (byte)(flag2 | flag1);
                    compressed_pos = compressed_pos + 2;
                    original_pos = original_pos + unk2 + 1;
                }
            } while (true);
        }

        #endregion
    }
}
