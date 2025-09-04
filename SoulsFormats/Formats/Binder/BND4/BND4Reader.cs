using System;
using System.IO;

namespace SoulsFormats
{
    /// <summary>
    /// An on-demand reader for BND4 containers.
    /// </summary>
    public class BND4Reader : BinderReader, IBND4
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        public bool Unk04 { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public bool Unk05 { get; set; }

        /// <summary>
        /// Whether to encode filenames as UTF-8 or Shift JIS.
        /// </summary>
        public bool Unicode { get; set; }

        /// <summary>
        /// Indicates presence of filename hash table.
        /// </summary>
        public byte Extended { get; set; }

        /// <summary>
        /// Type of compression used, if any.
        /// </summary>
        public DCX.CompressionInfo Compression { get; set; }

        /// <summary>
        /// Creates a new <see cref="BND4Reader"/> from the specified reader.
        /// </summary>
        /// <param name="br">The reader.</param>
        private BND4Reader(BinaryReaderEx br)
        {
            Read(br);
        }

        /// <summary>
        /// Reads a BND4 from the given path, decompressing if necessary.
        /// </summary>
        public BND4Reader(string path) : this(new BinaryReaderEx(false, File.OpenRead(path))) { }

        /// <summary>
        /// Reads a BND4 from the given bytes, decompressing if necessary.
        /// </summary>
        public BND4Reader(byte[] bytes) : this(new BinaryReaderEx(false, bytes)) { }

        /// <summary>
        /// Reads a BND4 from the given <see cref="Stream"/>, decompressing if necessary.
        /// </summary>
        public BND4Reader(Stream stream)
        {
            if (stream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            var br = new BinaryReaderEx(false, stream, true);
            Read(br);
        }

        /// <summary>
        /// Reads a BND4 from the given path, decompressing if necessary.
        /// </summary>
        public static BND4Reader Read(string path)
            => new BND4Reader(path);

        /// <summary>
        /// Reads a BND4 from the given bytes, decompressing if necessary.
        /// </summary>
        public static BND4Reader Read(byte[] bytes)
            => new BND4Reader(bytes);

        /// <summary>
        /// Reads a BND4 from the given <see cref="Stream"/>, decompressing if necessary.
        /// </summary>
        public static BND4Reader Read(Stream stream)
            => new BND4Reader(stream);

        /// <summary>
        /// Returns whether the file appears to be a file of this type and reads it if so.
        /// </summary>
        private static bool IsRead(BinaryReaderEx br, out BND4Reader reader)
        {
            if (BND4.IsFormat(br))
            {
                reader = new BND4Reader(br);
                return true;
            }

            br.Dispose();
            reader = null;
            return false;
        }

        /// <summary>
        /// Returns whether the file appears to be a file of this type and reads it if so.
        /// </summary>
        public static bool IsRead(string path, out BND4Reader reader)
        {
            FileStream fs = File.OpenRead(path);
            var br = new BinaryReaderEx(false, fs);
            return IsRead(br, out reader);
        }

        /// <summary>
        /// Returns whether the bytes appear to be a file of this type and reads it if so.
        /// </summary>
        public static bool IsRead(byte[] bytes, out BND4Reader reader)
        {
            var br = new BinaryReaderEx(false, bytes);
            return IsRead(br, out reader);
        }

        /// <summary>
        /// Returns whether the stream appears to be a file of this type and reads it if so.
        /// </summary>
        public static bool IsRead(Stream stream, out BND4Reader reader)
        {
            if (stream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            var br = new BinaryReaderEx(false, stream, true);
            return IsRead(br, out reader);
        }

        private void Read(BinaryReaderEx br)
        {
            br = SFUtil.GetDecompressedBinaryReader(br, out DCX.CompressionInfo compression);
            Compression = compression;
            Files = BND4.ReadHeader(this, br);
            DataBR = br;
        }
    }
}
