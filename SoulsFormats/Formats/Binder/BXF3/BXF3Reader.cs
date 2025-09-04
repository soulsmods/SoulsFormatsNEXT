using System;
using System.IO;

namespace SoulsFormats
{
    /// <summary>
    /// An on-demand reader for BXF3 containers.
    /// </summary>
    public class BXF3Reader : BinderReader, IBXF3
    {
        /// <summary>
        /// Creates a new <see cref="BXF3Reader"/> from the specified readers.
        /// </summary>
        /// <param name="brHeader">The header reader.</param>
        /// <param name="brData">The data reader.</param>
        private BXF3Reader(BinaryReaderEx brHeader, BinaryReaderEx brData)
        {
            Read(brHeader, brData);
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT paths.
        /// </summary>
        public BXF3Reader(string bhdPath, string bdtPath)
        {
            using (FileStream fsHeader = File.OpenRead(bhdPath))
            {
                var brHeader = new BinaryReaderEx(false, fsHeader);
                var brData = new BinaryReaderEx(false, bdtPath);
                Read(brHeader, brData);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD path and BDT bytes.
        /// </summary>
        public BXF3Reader(string bhdPath, byte[] bdtBytes)
        {
            using (FileStream fsHeader = File.OpenRead(bhdPath))
            {
                var brHeader = new BinaryReaderEx(false, fsHeader);
                var brData = new BinaryReaderEx(false, bdtBytes);
                Read(brHeader, brData);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD path and BDT stream.
        /// </summary>
        public BXF3Reader(string bhdPath, Stream bdtStream)
        {
            if (bdtStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            using (FileStream fsHeader = File.OpenRead(bhdPath))
            {
                var brHeader = new BinaryReaderEx(false, fsHeader);
                var brData = new BinaryReaderEx(false, bdtStream, true);
                Read(brHeader, brData);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT bytes.
        /// </summary>
        public BXF3Reader(byte[] bhdBytes, byte[] bdtBytes)
        {
            using (var msHeader = new MemoryStream(bhdBytes))
            {
                var brHeader = new BinaryReaderEx(false, msHeader);
                var brData = new BinaryReaderEx(false, bdtBytes);
                Read(brHeader, brData);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD bytes and BDT stream.
        /// </summary>
        public BXF3Reader(byte[] bhdBytes, string bdtPath)
        {
            using (var msHeader = new MemoryStream(bhdBytes))
            {
                var brHeader = new BinaryReaderEx(false, msHeader);
                var brData = new BinaryReaderEx(false, bdtPath);
                Read(brHeader, brData);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD bytes and BDT path.
        /// </summary>
        public BXF3Reader(byte[] bhdBytes, Stream bdtStream)
        {
            if (bdtStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            using (var msHeader = new MemoryStream(bhdBytes))
            {
                var brHeader = new BinaryReaderEx(false, msHeader);
                var brData = new BinaryReaderEx(false, bdtStream, true);
                Read(brHeader, brData);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT streams.
        /// </summary>
        public BXF3Reader(Stream bhdStream, Stream bdtStream)
        {
            if (bhdStream.Position != 0 && bdtStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if streams are not at position {0}.");
            }

            var brHeader = new BinaryReaderEx(false, bhdStream, true);
            var brData = new BinaryReaderEx(false, bdtStream, true);
            Read(brHeader, brData);
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD stream and BDT path.
        /// </summary>
        public BXF3Reader(Stream bhdStream, string bdtPath)
        {
            if (bhdStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            var brHeader = new BinaryReaderEx(false, bhdStream, true);
            var brData = new BinaryReaderEx(false, bdtPath);
            Read(brHeader, brData);
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD stream and BDT bytes.
        /// </summary>
        public BXF3Reader(Stream bhdStream, byte[] bdtBytes)
        {
            if (bhdStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            var brHeader = new BinaryReaderEx(false, bhdStream, true);
            var brData = new BinaryReaderEx(false, bdtBytes);
            Read(brHeader, brData);
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT paths.
        /// </summary>
        public static BXF3Reader Read(string bhdPath, string bdtPath)
            => new BXF3Reader(bhdPath, bdtPath);

        /// <summary>
        /// Reads a BXF3 from the given BHD path and BDT bytes.
        /// </summary>
        public static BXF3Reader Read(string bhdPath, byte[] bdtBytes)
            => new BXF3Reader(bhdPath, bdtBytes);

        /// <summary>
        /// Reads a BXF3 from the given BHD path and BDT stream.
        /// </summary>
        public static BXF3Reader Read(string bhdPath, Stream bdtStream)
            => new BXF3Reader(bhdPath, bdtStream);

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT bytes.
        /// </summary>
        public static BXF3Reader Read(byte[] bhdBytes, byte[] bdtBytes)
            => new BXF3Reader(bhdBytes, bdtBytes);

        /// <summary>
        /// Reads a BXF3 from the given BHD bytes and BDT stream.
        /// </summary>
        public static BXF3Reader Read(byte[] bhdBytes, string bdtPath)
            => new BXF3Reader(bhdBytes, bdtPath);

        /// <summary>
        /// Reads a BXF3 from the given BHD bytes and BDT path.
        /// </summary>
        public static BXF3Reader Read(byte[] bhdBytes, Stream bdtStream)
            => new BXF3Reader(bhdBytes, bdtStream);

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT streams.
        /// </summary>
        public static BXF3Reader Read(Stream bhdStream, Stream bdtStream)
            => new BXF3Reader(bhdStream, bdtStream);

        /// <summary>
        /// Reads a BXF3 from the given BHD stream and BDT path.
        /// </summary>
        public static BXF3Reader Read(Stream bhdStream, string bdtPath)
            => new BXF3Reader(bhdStream, bdtPath);

        /// <summary>
        /// Reads a BXF3 from the given BHD stream and BDT bytes.
        /// </summary>
        public static BXF3Reader Read(Stream bhdStream, byte[] bdtBytes)
            => new BXF3Reader(bhdStream, bdtBytes);

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT paths.
        /// </summary>
        public static bool IsRead(string bhdPath, string bdtPath, out BXF3Reader reader)
        {
            using (var brHeader = new BinaryReaderEx(false, bhdPath))
            {
                var brData = new BinaryReaderEx(false, bdtPath);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD path and BDT bytes.
        /// </summary>
        public static bool IsRead(string bhdPath, byte[] bdtBytes, out BXF3Reader reader)
        {
            using (var brHeader = new BinaryReaderEx(false, bhdPath))
            {
                var brData = new BinaryReaderEx(false, bdtBytes);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD path and BDT stream.
        /// </summary>
        public static bool IsRead(string bhdPath, Stream bdtStream, out BXF3Reader reader)
        {
            if (bdtStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            using (var brHeader = new BinaryReaderEx(false, bhdPath))
            {
                var brData = new BinaryReaderEx(false, bdtStream, true);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT bytes.
        /// </summary>
        public static bool IsRead(byte[] bhdBytes, byte[] bdtBytes, out BXF3Reader reader)
        {
            using (var brHeader = new BinaryReaderEx(false, bhdBytes))
            {
                var brData = new BinaryReaderEx(false, bdtBytes);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD bytes and BDT stream.
        /// </summary>
        public static bool IsRead(byte[] bhdBytes, string bdtPath, out BXF3Reader reader)
        {
            using (var brHeader = new BinaryReaderEx(false, bhdBytes))
            {
                var brData = new BinaryReaderEx(false, bdtPath);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD bytes and BDT path.
        /// </summary>
        public static bool IsRead(byte[] bhdBytes, Stream bdtStream, out BXF3Reader reader)
        {
            if (bdtStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            using (var brHeader = new BinaryReaderEx(false, bhdBytes))
            {
                var brData = new BinaryReaderEx(false, bdtStream, true);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD and BDT streams.
        /// </summary>
        public static bool IsRead(Stream bhdStream, Stream bdtStream, out BXF3Reader reader)
        {
            if (bhdStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            if (bdtStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            using (var brHeader = new BinaryReaderEx(false, bhdStream, true))
            {
                var brData = new BinaryReaderEx(false, bdtStream, true);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD stream and BDT path.
        /// </summary>
        public static bool IsRead(Stream bhdStream, string bdtPath, out BXF3Reader reader)
        {
            if (bhdStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            using (var brHeader = new BinaryReaderEx(false, bhdStream, true))
            {
                var brData = new BinaryReaderEx(false, bdtPath);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Reads a BXF3 from the given BHD stream and BDT bytes.
        /// </summary>
        public static bool IsRead(Stream bhdStream, byte[] bdtBytes, out BXF3Reader reader)
        {
            if (bhdStream.Position != 0)
            {
                // Cannot ensure offset jumping for every format will work otherwise
                throw new InvalidOperationException($"Cannot safely read if stream is not at position {0}.");
            }

            using (var brHeader = new BinaryReaderEx(false, bhdStream, true))
            {
                var brData = new BinaryReaderEx(false, bdtBytes);
                return IsRead(brHeader, brData, out reader);
            }
        }

        /// <summary>
        /// Returns whether the file appears to be a file of this type and reads it if so.
        /// </summary>
        private static bool IsRead(BinaryReaderEx brHeader, BinaryReaderEx brData, out BXF3Reader reader)
        {
            if (BXF3.IsHeader(brHeader))
            {
                reader = new BXF3Reader(brHeader, brData);
                return true;
            }

            brHeader.Dispose();
            brData.Dispose();
            reader = null;
            return false;
        }

        private void Read(BinaryReaderEx brHeader, BinaryReaderEx brData)
        {
            BXF3.ReadBDFHeader(brData);
            Files = BXF3.ReadBHFHeader(this, brHeader);
            DataBR = brData;
        }
    }
}
