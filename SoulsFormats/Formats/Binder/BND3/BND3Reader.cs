using System;
using System.IO;

namespace SoulsFormats
{
    /// <summary>
    /// An on-demand reader for BND3 containers.
    /// </summary>
    public class BND3Reader : BinderReader, IBND3
    {
        /// <summary>
        /// Unknown; always 0 except in DeS where it's occasionally 0x80000000 (probably a byte).
        /// </summary>
        public int Unk18 { get; set; }

        /// <summary>
        /// Whether or not to write the file headers end value or 0.<br/>
        /// Some Binders have this as 0 and require it to be as such for some reason.
        /// </summary>
        public bool WriteFileHeadersEnd { get; set; }

        /// <summary>
        /// Type of compression used, if any.
        /// </summary>
        public DCX.CompressionInfo Compression { get; set; }

        /// <summary>
        /// Creates a new <see cref="BND3Reader"/> from the specified <see cref="BinaryReaderEx"/>.
        /// </summary>
        /// <param name="br">The reader.</param>
        private BND3Reader(BinaryReaderEx br)
        {
            Read(br);
        }

        /// <summary>
        /// Reads a BND3 from the given path, decompressing if necessary.
        /// </summary>
        public BND3Reader(string path) : this(new BinaryReaderEx(false, File.OpenRead(path))) { }

        /// <summary>
        /// Reads a BND3 from the given bytes, decompressing if necessary.
        /// </summary>
        public BND3Reader(byte[] bytes) : this(new BinaryReaderEx(false, bytes)) { }

        /// <summary>
        /// Reads a BND3 from the given <see cref="Stream"/>, decompressing if necessary.
        /// </summary>
        public BND3Reader(Stream stream)
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
        /// Reads a BND3 from the given path, decompressing if necessary.
        /// </summary>
        public static BND3Reader Read(string path)
            => new BND3Reader(path);

        /// <summary>
        /// Reads a BND3 from the given bytes, decompressing if necessary.
        /// </summary>
        public static BND3Reader Read(byte[] bytes)
            => new BND3Reader(bytes);

        /// <summary>
        /// Reads a BND3 from the given <see cref="Stream"/>, decompressing if necessary.
        /// </summary>
        public static BND3Reader Read(Stream stream)
            => new BND3Reader(stream);

        /// <summary>
        /// Returns whether the file appears to be a file of this type and reads it if so.
        /// </summary>
        private static bool IsRead(BinaryReaderEx br, out BND3Reader reader)
        {
            if (BND3.IsFormat(br))
            {
                reader = new BND3Reader(br);
                return true;
            }

            br.Dispose();
            reader = null;
            return false;
        }

        /// <summary>
        /// Returns whether the file appears to be a file of this type and reads it if so.
        /// </summary>
        public static bool IsRead(string path, out BND3Reader reader)
        {
            FileStream fs = File.OpenRead(path);
            var br = new BinaryReaderEx(false, fs);
            return IsRead(br, out reader);
        }

        /// <summary>
        /// Returns whether the bytes appear to be a file of this type and reads it if so.
        /// </summary>
        public static bool IsRead(byte[] bytes, out BND3Reader reader)
        {
            var br = new BinaryReaderEx(false, bytes);
            return IsRead(br, out reader);
        }

        /// <summary>
        /// Returns whether the stream appears to be a file of this type and reads it if so.
        /// </summary>
        public static bool IsRead(Stream stream, out BND3Reader reader)
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
            Files = BND3.ReadHeader(this, br);
            DataBR = br;
        }
    }
}
