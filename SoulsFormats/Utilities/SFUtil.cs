using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ZstdNet;

namespace SoulsFormats
{
    /// <summary>
    /// Miscellaneous utility functions for SoulsFormats, mostly for internal use.
    /// </summary>
    public static class SFUtil
    {
        /// <summary>
        /// Decompresses data and returns a new BinaryReaderEx if necessary.
        /// </summary>
        public static BinaryReaderEx GetDecompressedBinaryReader(BinaryReaderEx br, out DCX.Type compression)
        {
            if (DCX.Is(br))
            {
                byte[] bytes = DCX.Decompress(br, out compression);
                return new BinaryReaderEx(false, bytes);
            }
            else
            {
                compression = DCX.Type.None;
                return br;
            }
        }

        /// <summary>
        /// FromSoft's basic filename hashing algorithm, used in some BND and BXF formats.
        /// </summary>
        public static uint FromPathHash(string text)
        {
            string hashable = text.ToLowerInvariant().Replace('\\', '/');
            if (!hashable.StartsWith("/"))
                hashable = '/' + hashable;
            return hashable.Aggregate(0u, (i, c) => i * 37u + c);
        }

        /// <summary>
        /// Determines whether a number is prime or not.
        /// </summary>
        public static bool IsPrime(uint candidate)
        {
            if (candidate < 2)
                return false;
            if (candidate == 2)
                return true;
            if (candidate % 2 == 0)
                return false;

            for (int i = 3; i * i <= candidate; i += 2)
            {
                if (candidate % i == 0)
                    return false;
            }

            return true;
        }

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

        /// <summary>
        /// Compresses data and writes it to a BinaryWriterEx with Zlib wrapper.
        /// </summary>
        public static int WriteZlib(BinaryWriterEx bw, byte formatByte, byte[] input)
        {
            long start = bw.Position;
            bw.WriteByte(0x78);
            bw.WriteByte(formatByte);

            using (var deflateStream = new DeflateStream(bw.Stream, CompressionMode.Compress, true))
            {
                deflateStream.Write(input, 0, input.Length);
            }

            bw.WriteUInt32(Adler32(input));
            return (int)(bw.Position - start);
        }

        /// <summary>
        /// Reads a Zlib block from a BinaryReaderEx and returns the uncompressed data.
        /// </summary>
        public static byte[] ReadZlib(BinaryReaderEx br, int compressedSize)
        {
            br.AssertByte(0x78);
            br.AssertByte(0x01, 0x5E, 0x9C, 0xDA);
            return DecompressZlibBytes(br.ReadBytes(compressedSize - 2));
        }

        /// <summary>
        /// Decompresses zlib starting at the current position in a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/>.</param>
        /// <param name="compressedSize">The size of the compressed data including the 2 byte zlib header.</param>
        /// <returns>Decompressed zlib data.</returns>
        /// <exception cref="EndOfStreamException">Cannot read beyond the end of the stream.</exception>
        /// <exception cref="InvalidDataException">A valid zlib header could not be detected.</exception>
        /// <exception cref="Exception">Did not read the expected number of compressed bytes from the <see cref="Stream"/>.</exception>
        public static byte[] DecompressZlib(Stream stream, int compressedSize)
        {
            var cmf = stream.ReadByte();
            var flg = stream.ReadByte();

            if (cmf == -1 || flg == -1)
            {
                throw new EndOfStreamException("Cannot read beyond the end of the stream.");
            }

            if (cmf != 0x78)
            {
                throw new InvalidDataException("Zlib header could not be detected.");
            }

            if (flg != 0x01 && flg != 0x5E && flg != 0x9C && flg != 0xDA)
            {
                throw new InvalidDataException("Valid zlib compression level could not be detected.");
            }

            byte[] bytes = new byte[compressedSize - 2];
            if (stream.Read(bytes, 0, bytes.Length) < bytes.Length)
            {
                throw new Exception("Could not read the expected number of bytes.");
            }
            return DecompressZlibBytes(bytes);
        }

        /// <summary>
        /// Decompresses zlib bytes coming after a zlib header.
        /// </summary>
        /// <param name="compressedBytes">Compressed bytes not including the zlib header.</param>
        /// <returns>Decompressed data.</returns>
        public static byte[] DecompressZlibBytes(byte[] compressedBytes)
        {
            using (var decompressedStream = new MemoryStream())
            using (var compressedStream = new MemoryStream(compressedBytes))
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, true))
            {
                deflateStream.CopyTo(decompressedStream);
                return decompressedStream.ToArray();
            }
        }

        /// <summary>
        /// Computes an Adler32 checksum used by Zlib.
        /// </summary>
        public static uint Adler32(byte[] data)
        {
            uint adlerA = 1;
            uint adlerB = 0;

            foreach (byte b in data)
            {
                adlerA = (adlerA + b) % 65521;
                adlerB = (adlerB + adlerA) % 65521;
            }

            return (adlerB << 16) | adlerA;
        }

        /// <summary>
        /// Concatenates multiple collections into one list.
        /// </summary>
        public static List<T> ConcatAll<T>(params IEnumerable<T>[] lists)
        {
            IEnumerable<T> all = new List<T>();
            foreach (IEnumerable<T> list in lists)
                all = all.Concat(list);
            return all.ToList();
        }

        /// <summary>
        /// Convert a list to a dictionary with indices as keys.
        /// </summary>
        public static Dictionary<int, T> Dictionize<T>(List<T> items)
        {
            var dict = new Dictionary<int, T>(items.Count);
            for (int i = 0; i < items.Count; i++)
                dict[i] = items[i];
            return dict;
        }

        /// <summary>
        /// Converts a hex string in format "AA BB CC DD" to a byte array.
        /// </summary>
        public static byte[] ParseHexString(string str)
        {
            string[] strings = str.Split(' ');
            byte[] bytes = new byte[strings.Length];
            for (int i = 0; i < strings.Length; i++)
                bytes[i] = Convert.ToByte(strings[i], 16);
            return bytes;
        }

        /// <summary>
        /// Returns a copy of the key used for encrypting original DS2 save files on PC.
        /// </summary>
        public static byte[] GetDS2SaveKey()
        {
            return (byte[])ds2SaveKey.Clone();
        }

        private static readonly byte[] ds2SaveKey = ParseHexString("B7 FD 46 3E 4A 9C 11 02 DF 17 39 E5 F3 B2 A5 0F");

        /// <summary>
        /// Returns a copy of the key used for encrypting DS2 SotFS save files on PC.
        /// </summary>
        public static byte[] GetScholarSaveKey()
        {
            return (byte[])scholarSaveKey.Clone();
        }

        private static readonly byte[] scholarSaveKey =
            ParseHexString("59 9F 9B 69 96 40 A5 52 36 EE 2D 70 83 5E C7 44");

        /// <summary>
        /// Returns a copy of the key used for encrypting DS3 save files on PC.
        /// </summary>
        public static byte[] GetDS3SaveKey()
        {
            return (byte[])ds3SaveKey.Clone();
        }

        private static readonly byte[] ds3SaveKey = ParseHexString("FD 46 4D 69 5E 69 A3 9A 10 E3 19 A7 AC E8 B7 FA");

        /// <summary>
        /// Decrypts a file from a DS2/DS3 SL2. Do not remove the hash and IV before calling.
        /// </summary>
        public static byte[] DecryptSL2File(byte[] encrypted, byte[] key)
        {
            // Just leaving this here for documentation
            //byte[] hash = new byte[16];
            //Buffer.BlockCopy(encrypted, 0, hash, 0, 16);

            byte[] iv = new byte[16];
            Buffer.BlockCopy(encrypted, 16, iv, 0, 16);

            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.BlockSize = 128;
                // PKCS7-style padding is used, but they don't include the minimum padding
                // so it can't be stripped safely
                aes.Padding = PaddingMode.None;
                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor();
                using (var encStream = new MemoryStream(encrypted, 32, encrypted.Length - 32))
                using (var cryptoStream = new CryptoStream(encStream, decryptor, CryptoStreamMode.Read))
                using (var decStream = new MemoryStream())
                {
                    cryptoStream.CopyTo(decStream);
                    return decStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Encrypts a file for a DS2/DS3 SL2. Result includes the hash and IV.
        /// </summary>
        public static byte[] EncryptSL2File(byte[] decrypted, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.None;
                aes.Key = key;
                aes.GenerateIV();

                ICryptoTransform encryptor = aes.CreateEncryptor();
                using (var decStream = new MemoryStream(decrypted))
                using (var cryptoStream = new CryptoStream(decStream, encryptor, CryptoStreamMode.Read))
                using (var encStream = new MemoryStream())
                using (var md5 = MD5.Create())
                {
                    encStream.Write(aes.IV, 0, 16);
                    cryptoStream.CopyTo(encStream);
                    byte[] encrypted = new byte[encStream.Length + 16];
                    encStream.Position = 0;
                    encStream.Read(encrypted, 16, (int)encStream.Length);
                    byte[] hash = md5.ComputeHash(encrypted, 16, encrypted.Length - 16);
                    Buffer.BlockCopy(hash, 0, encrypted, 0, 16);
                    return encrypted;
                }
            }
        }

        public enum RegulationKey
        {
            DarkSouls3 = 0,
            EldenRing = 1,
            ArmoredCore6 = 2,
        }

            private static readonly Dictionary<RegulationKey, byte[]> RegulationKeyDictionary = new Dictionary<RegulationKey, byte[]>
            {
                { RegulationKey.DarkSouls3, SFEncoding.ASCII.GetBytes("ds3#jn/8_7(rsY9pg55GFN7VFL#+3n/)") },
                { RegulationKey.EldenRing, ParseHexString(
                    "99 BF FC 36 6A 6B C8 C6 F5 82 7D 09 36 02 D6 76 C4 28 92 A0 1C 20 7F B0 24 D3 AF 4E 49 3F EF 99")},
                { RegulationKey.ArmoredCore6, ParseHexString(
                    "10 CE ED 47 7B 7C D9 D7 E6 93 8E 11 47 13 E7 87 D5 39 13 B1 D 31 8E C1 35 E4 BE 50 50 4E E 10")}
            };

        /// <summary>
        /// Decrypts and unpacks DS3's regulation BND4 from the specified path.
        /// </summary>
        public static BND4 DecryptDS3Regulation(string path)
        {
            return DecryptBndWithKey(path, RegulationKey.DarkSouls3);
        }
        /// <summary>
        /// Decrypts and unpacks ER's regulation BND4 from the specified path.
        /// </summary>
        public static BND4 DecryptERRegulation(string path)
        {
            return DecryptBndWithKey(path, RegulationKey.EldenRing);
        }
        /// <summary>
        /// Decrypts and unpacks AC6's regulation BND4 from the specified path.
        /// </summary>
        public static BND4 DecryptAC6Regulation(string path)
        {
            return DecryptBndWithKey(path, RegulationKey.ArmoredCore6);
        }

        /// <summary>
        /// Decrypts and unpacks a regulation BND4 from the specified path with a provided key.
        /// </summary>
        public static BND4 DecryptBndWithKey(string path, RegulationKey key)
        {
            byte[] bytes = File.ReadAllBytes(path);
            bytes = DecryptByteArray(key, bytes);
            return BND4.Read(bytes);
        }


        /// <summary>
        /// Repacks and encrypts DS3's regulation BND4 to the specified path.
        /// </summary>
        public static void EncryptDS3Regulation(string path, BND4 bnd)
        {
            EncryptRegulationWithKey(path, bnd, RegulationKey.DarkSouls3);
        }
        /// <summary>
        /// Repacks and encrypts ER's regulation BND4 to the specified path.
        /// </summary>
        public static void EncryptERRegulation(string path, BND4 bnd)
        {
            EncryptRegulationWithKey(path, bnd, RegulationKey.EldenRing);
        }

        /// <summary>
        /// Repacks and encrypts AC6's regulation BND4 to the specified path.
        /// </summary>
        public static void EncryptAC6Regulation(string path, BND4 bnd)
        {
            EncryptRegulationWithKey(path, bnd, RegulationKey.ArmoredCore6);
        }

        public static void EncryptRegulationWithKey(string path, BND4 bnd, RegulationKey key)
        {
            byte[] bytes = bnd.Write();
            bytes = EncryptByteArray(key, bytes);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
        }

        private static byte[] EncryptByteArray(RegulationKey key, byte[] secret)
        {
            using (MemoryStream ms = new MemoryStream())
            using (AesManaged cryptor = new AesManaged())
            {
                cryptor.Mode = CipherMode.CBC;
                cryptor.Padding = PaddingMode.PKCS7;
                cryptor.KeySize = 256;
                cryptor.BlockSize = 128;

                byte[] iv = cryptor.IV;

                using (CryptoStream cs = new CryptoStream(ms, cryptor.CreateEncryptor(RegulationKeyDictionary[key], iv), CryptoStreamMode.Write))
                {
                    cs.Write(secret, 0, secret.Length);
                }

                byte[] encryptedContent = ms.ToArray();

                byte[] result = new byte[iv.Length + encryptedContent.Length];

                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length);

                return result;
            }
        }

        private static byte[] DecryptByteArray(RegulationKey key, byte[] secret)
        {
            byte[] iv = new byte[16];
            byte[] encryptedContent = new byte[secret.Length - 16];

            Buffer.BlockCopy(secret, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(secret, iv.Length, encryptedContent, 0, encryptedContent.Length);

            using (MemoryStream ms = new MemoryStream())
            using (AesManaged cryptor = new AesManaged())
            {
                cryptor.Mode = CipherMode.CBC;
                cryptor.Padding = PaddingMode.None;
                cryptor.KeySize = 256;
                cryptor.BlockSize = 128;

                using (CryptoStream cs = new CryptoStream(ms, cryptor.CreateDecryptor(RegulationKeyDictionary[key], iv), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedContent, 0, encryptedContent.Length);
                }

                return ms.ToArray();
            }
        }

        // Written by ClayAmore
        public static byte[] ReadZstd(BinaryReaderEx br, int compressedSize)
        {
            byte[] compressed = br.ReadBytes(compressedSize);

            using (var decompressedStream = new MemoryStream())
            {
                using (var compressedStream = new MemoryStream(compressed))
                using (var deflateStream = new DecompressionStream(compressedStream))
                {
                    deflateStream.CopyTo(decompressedStream);
                }
                return decompressedStream.ToArray();
            }
        }

        public static byte[] WriteZstd(byte[] data, int compressionLevel)
        {
            var options = new CompressionOptions(null, new Dictionary<ZSTD_cParameter, int> { { ZSTD_cParameter.ZSTD_c_contentSizeFlag, 0 }, { ZSTD_cParameter.ZSTD_c_windowLog, 16 } }, compressionLevel);
            using (var compressor = new Compressor(options))
            {
                return compressor.Wrap(data).ToArray();
            }
        }

        internal static byte[] To4Bit(byte value)
        {
            byte[] values = new byte[2];
            values[0] = (byte)((byte)(value & 0b1111_0000) >> 4);
            values[1] = (byte)(value & 0b0000_1111);
            return values;
        }
    }
}