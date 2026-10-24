using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SoulsFormats
{
    /// <summary>
    /// An extended reader for binary data supporting big and little endianness, value assertions, and arrays.
    /// </summary>
    public class BinaryReaderEx : IDisposable
    {
        private const int BufferSize = 16 * 1024;

        /// <summary>
        /// Skips various assertions in order to load a file despite potential mismatches.
        /// This is mainly a tool to circumvent malicious format hacking by modders who try to
        /// "obfuscate" their files. This should not be set to TRUE for most purposes unless
        /// you are extremely sure what you are doing.
        /// </summary>
        public static bool IsFlexible { get; set; }

        private Stack<long> steps;
        
        /// <summary>
        /// Internal IO buffer.
        /// </summary>
        private IoSlice slice;
        
        /// <summary>
        /// Underlying Stream the buffer is filled from, `null` when using an in-memory buffer.
        /// </summary>
        private readonly Stream stream;

        /// <summary>
        /// Whether <see cref="stream"/> is left open when the reader is disposed.
        /// </summary>
        private readonly bool leaveOpen;

        /// <summary>
        /// Whether or not the <see cref="BinaryReaderEx"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Interpret values as big-endian if set, or little-endian if not.
        /// </summary>
        public bool BigEndian { get; set; }

        /// <summary>
        /// Varints are read as Int64 if set, otherwise Int32.
        /// </summary>
        public bool VarintLong { get; set; }

        /// <summary>
        /// Current size of varints in bytes.
        /// </summary>
        public int VarintSize => VarintLong ? 8 : 4;

        /// <summary>
        /// The current position of the stream.
        /// </summary>
        public long Position
        {
            get
            {
                ThrowIfDisposed();
                return slice.Position;
            }
            set
            {
                ThrowIfDisposed();
                SetPosition(value);
            }
        }

        /// <summary>
        /// The length of the stream.
        /// </summary>
        public long Length
        {
            get
            {
                ThrowIfDisposed();
                return stream?.Length ?? slice.Length;
            }
        }

        /// <summary>
        /// The amount of bytes from the current position to the end of the stream.
        /// </summary>
        public long Remaining => Length - Position;

        /// <summary>
        /// Initializes a new BinaryReaderEx reading from the specified file.
        /// </summary>
        public BinaryReaderEx(bool bigEndian, string path)
            : this(bigEndian, new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1), false) { }

        /// <summary>
        /// Initializes a new BinaryReaderEx reading from the specified byte array.
        /// </summary>
        public BinaryReaderEx(bool bigEndian, byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input, "buffer");
            BigEndian = bigEndian;
            steps = new Stack<long>(8);
            slice = new IoSlice { Buffer = input, Length = input.Length };
        }

        /// <summary>
        /// Initializes a new BinaryReaderEx reading from the specified stream.
        /// </summary>
        public BinaryReaderEx(bool bigEndian, Stream stream, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(stream, "input");
            if (!stream.CanRead)
                throw new ArgumentException("Stream was not readable.");

            BigEndian = bigEndian;
            steps = new Stack<long>(8);

            this.stream = stream;
            this.leaveOpen = leaveOpen;
            this.slice = new IoSlice { Buffer = ArrayPool<byte>.Shared.Rent(BufferSize), Start = stream.CanSeek ? stream.Position : 0 };
        }

        /// <summary>Ensure there are at least <paramref name="minimum"/> bytes available in the IO buffer</summary>
        private ReadOnlySpan<byte> EnsureBuffered(int minimum)
        {
            ReadOnlySpan<byte> buffered = Buffered;
            if (minimum <= buffered.Length && minimum >= 0)
                return buffered;

            ThrowIfDisposed();

            if (minimum > 0)
                Fill(minimum);

            return Buffered;
        }
    
        private ReadOnlySpan<byte> Buffered => slice.Cursor < slice.Length
            ? new ReadOnlySpan<byte>(slice.Buffer, slice.Cursor, slice.Length - slice.Cursor)
            : default;

        /// <summary>Moves the reader past <paramref name="count"/> bytes that <see cref="EnsureBuffered"/> gave it.</summary>
        private void Consume(int count) => slice.Cursor += count;

        private ReadOnlySpan<byte> Take(int count)
        {
            ReadOnlySpan<byte> buffered = EnsureBuffered(count);
            if (buffered.Length < count)
                throw EndOfData();

            Consume(count);
            return buffered[..count];
        }

        private T Take<T>() where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            ReadOnlySpan<byte> buffered = EnsureBuffered(size);
            if (buffered.Length < size)
                throw EndOfData();

            T value = MemoryMarshal.Read<T>(buffered);
            Consume(size);

            if (BigEndian)
                ReverseEach(MemoryMarshal.AsBytes(new Span<T>(ref value)), size);

            return value;
        }

        /// <summary>
        /// Attempt to read <paramref name="count"/> bytes at <paramref name="offset"/> without refilling the IO buffer.
        /// </summary>
        private bool TryPeek(long offset, int count, out ReadOnlySpan<byte> bytes)
        {
            long start = offset - slice.Start;
            if (count > 0 && start >= 0 && start <= slice.Length - count)
            {
                bytes = new ReadOnlySpan<byte>(slice.Buffer, (int)start, count);
                return true;
            }

            bytes = default;
            return false;
        }

        /// <summary>
        /// Attempt to read <typeparamref name="T"/> at <paramref name="offset"/> without refilling the IO buffer.
        /// </summary>
        private bool TryPeek<T>(long offset, out T value) where T : unmanaged
        {
            if (TryPeek(offset, Unsafe.SizeOf<T>(), out var bytes))
            {
                value = MemoryMarshal.Read<T>(bytes);
                if (BigEndian)
                    ReverseEach(MemoryMarshal.AsBytes(new Span<T>(ref value)), bytes.Length);

                return true;
            }

            value = default;
            return false;
        }

        private EndOfStreamException EndOfData()
        {
            slice.Cursor = Math.Max(slice.Cursor, slice.Length);
            return new EndOfStreamException("Remaining size of stream was smaller than requested number of bytes.");
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(null, "Cannot access a closed Stream.");
        }
 
        /// <summary>
        /// Reads <paramref name="count"/> values of <typeparamref name="T"/>
        /// </summary>
        private T[] ReadArray<T>(int count) where T : unmanaged
        {
            var result = new T[count];
            Span<byte> bytes = MemoryMarshal.AsBytes(result.AsSpan());
            ReadInto(bytes);

            if (BigEndian)
                ReverseEach(bytes, Unsafe.SizeOf<T>());

            return result;
        }

        /// <summary>
        /// Reverses the byte order of each <paramref name="size"/>-byte value in <paramref name="bytes"/>.
        /// </summary>
        private static void ReverseEach(Span<byte> bytes, int size)
        {
            if (size == 2)
                BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<byte, ushort>(bytes), MemoryMarshal.Cast<byte, ushort>(bytes));
            else if (size == 4)
                BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<byte, uint>(bytes), MemoryMarshal.Cast<byte, uint>(bytes));
            else if (size == 8)
                BinaryPrimitives.ReverseEndianness(MemoryMarshal.Cast<byte, ulong>(bytes), MemoryMarshal.Cast<byte, ulong>(bytes));
        }

        private T GetValue<T>(long offset) where T : unmanaged
        {
            if (TryPeek(offset, out T value))
                return value;

            StepIn(offset);
            value = Take<T>();
            StepOut();
            return value;
        }

        private T[] GetArray<T>(long offset, int count) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            if (count > 0 && count <= int.MaxValue / size && TryPeek(offset, count * size, out var bytes))
            {
                var result = new T[count];
                var destination = MemoryMarshal.AsBytes(result.AsSpan());
                bytes.CopyTo(destination);

                if (BigEndian)
                    ReverseEach(destination, size);

                return result;
            }

            StepIn(offset);
            T[] values = ReadArray<T>(count);
            StepOut();
            return values;
        }

        internal void GetValues<T>(long offset, Span<T> destination) where T : unmanaged
        {
            StepIn(offset);
            Span<byte> bytes = MemoryMarshal.AsBytes(destination);
            ReadInto(bytes);

            if (BigEndian)
                ReverseEach(bytes, Unsafe.SizeOf<T>());

            StepOut();
        }

        internal void GetUInt16s(long offset, Span<int> destination)
        {
            StepIn(offset);
            ReadOnlySpan<ushort> values = MemoryMarshal.Cast<byte, ushort>(Take(checked(destination.Length * sizeof(ushort))));

            if (BigEndian)
            {
                for (int i = 0; i < values.Length; i++)
                    destination[i] = BinaryPrimitives.ReverseEndianness(values[i]);
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                    destination[i] = values[i];
            }

            StepOut();
        }

        /// <summary>
        /// Compares a value to a list of options, returning it if found or excepting if not.
        /// </summary>
        private T AssertValue<T>(T value, string typeName, string valueFormat, ReadOnlySpan<T> options) where T : IEquatable<T>
        {
            if (IsFlexible) return value;

            foreach (T option in options)
                if (value.Equals(option))
                    return value;

            throw Mismatch(value, typeName, valueFormat, options.ToArray());
        }

        private T AssertValue<T>(T value, string typeName, string valueFormat, T option) where T : IEquatable<T>
        {
            if (IsFlexible || value.Equals(option)) return value;

            throw Mismatch(value, typeName, valueFormat, [option]);
        }

        private InvalidDataException Mismatch<T>(T value, string typeName, string valueFormat, T[] options)
        {
            string strValue = string.Format(valueFormat, value);
            string strOptions = string.Join(", ", options.Select(o => string.Format(valueFormat, o)));
            return new InvalidDataException($"Read {typeName}: {strValue} | Expected: {strOptions} | Ending position: 0x{Position:X}");
        }

        /// <summary>
        /// Store the current position of the stream on a stack, then move to the specified offset.
        /// </summary>
        public void StepIn(long offset)
        {
            steps.Push(Position);
            Position = offset;
        }

        /// <summary>
        /// Restore the previous position of the stream from a stack.
        /// </summary>
        public void StepOut()
        {
            if (steps.Count == 0)
                throw new InvalidOperationException("Reader is already stepped all the way out.");

            Position = steps.Pop();
        }

        /// <summary>
        /// Advances the stream position until it meets the specified alignment.
        /// </summary>
        public void Pad(int align)
        {
            if (Position % align > 0)
                Position += align - (Position % align);
        }

        /// <summary>
        /// Advances the stream position until it meets the specified alignment relative to the given starting position.
        /// </summary>
        public void PadRelative(long start, int align)
        {
            long relPos = Position - start;
            if (relPos % align > 0)
                Position += align - (relPos % align);
        }

        /// <summary>
        /// Advances the stream position by count bytes.
        /// </summary>
        public void Skip(int count)
        {
            Position += count;
        }

        // From SoulsAssetPipeline -> BinaryReaderWriterExtensions.cs by Meowmaritus
        public long GetNextPaddedOffsetAfterCurrentField(int currentFieldLength, int align)
        {
            long pos = Position;
            pos += currentFieldLength;
            if (align <= 0)
                return pos;
            if (pos % align > 0)
                pos += align - (pos % align);
            return pos;
        }

        #region Boolean
        /// <summary>
        /// Reads a one-byte boolean value.
        /// </summary>
        public bool ReadBoolean()
        {
            return DecodeBoolean(Take<byte>());
        }

        private static bool DecodeBoolean(byte b)
        {
            // BinaryReader.ReadBoolean accepts any non-zero value as true, which I don't want.
            if (b == 0)
                return false;
            else if (b == 1)
                return true;
            else
                throw new InvalidDataException($"ReadBoolean encountered non-boolean value: 0x{b:X2}");
        }

        /// <summary>
        /// Reads an array of one-byte boolean values.
        /// </summary>
        public bool[] ReadBooleans(int count)
        {
            var result = new bool[count];
            for (int i = 0; i < count; i++)
                result[i] = ReadBoolean();
            return result;
        }

        /// <summary>
        /// Reads a one-byte boolean value from the specified offset without advancing the stream.
        /// </summary>
        public bool GetBoolean(long offset)
        {
            if (TryPeek(offset, out byte value) && value <= 1)
                return value == 1;

            StepIn(offset);
            bool result = ReadBoolean();
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads an array of one-byte boolean values from the specified offset without advancing the stream.
        /// </summary>
        public bool[] GetBooleans(long offset, int count)
        {
            StepIn(offset);
            bool[] result = ReadBooleans(count);
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads a one-byte boolean value and throws an exception if it does not match the specified option.
        /// </summary>
        public bool AssertBoolean(bool option)
        {
            return AssertValue(ReadBoolean(), "Boolean", "{0}", option);
        }
        #endregion

        #region SByte
        /// <summary>
        /// Reads a one-byte signed integer.
        /// </summary>
        public sbyte ReadSByte()
        {
            return Take<sbyte>();
        }

        /// <summary>
        /// Reads an array of one-byte signed integers.
        /// </summary>
        public sbyte[] ReadSBytes(int count)
        {
            return ReadArray<sbyte>(count);
        }

        /// <summary>
        /// Reads a one-byte signed integer from the specified offset without advancing the stream.
        /// </summary>
        public sbyte GetSByte(long offset)
        {
            return GetValue<sbyte>(offset);
        }

        /// <summary>
        /// Reads an array of one-byte signed integers from the specified offset without advancing the stream.
        /// </summary>
        public sbyte[] GetSBytes(long offset, int count)
        {
            return GetArray<sbyte>(offset, count);
        }

        /// <summary>
        /// Reads a one-byte signed integer and throws an exception if it does not match any of the specified options.
        /// </summary>
        public sbyte AssertSByte(params ReadOnlySpan<sbyte> options)
        {
            return AssertValue(ReadSByte(), "SByte", "0x{0:X}", options);
        }

        /// <summary>
        /// Reads a one-byte signed integer and throws an exception if it does not match the specified option.
        /// </summary>
        public sbyte AssertSByte(sbyte option)
        {
            return AssertValue(ReadSByte(), "SByte", "0x{0:X}", option);
        }
        #endregion

        #region Byte
        /// <summary>
        /// Reads a one-byte unsigned integer.
        /// </summary>
        public byte ReadByte()
        {
            return Take<byte>();
        }

        /// <summary>
        /// Reads an array of one-byte unsigned integers.
        /// </summary>
        public byte[] ReadBytes(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            byte[] result = GC.AllocateUninitializedArray<byte>(count);
            ReadInto(result);
            return result;
        }

        /// <summary>
        /// Reads the specified number of bytes from the stream into the buffer starting at the specified index.
        /// </summary>
        public void ReadBytes(byte[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (buffer.Length - index < count)
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

            ReadInto(buffer.AsSpan(index, count));
        }

        /// <summary>
        /// Reads a one-byte unsigned integer from the specified offset without advancing the stream.
        /// </summary>
        public byte GetByte(long offset)
        {
            return GetValue<byte>(offset);
        }

        /// <summary>
        /// Reads an array of one-byte unsigned integers from the specified offset without advancing the stream.
        /// </summary>
        public byte[] GetBytes(long offset, int count)
        {
            if (TryPeek(offset, count, out var bytes))
                return bytes.ToArray();

            StepIn(offset);
            byte[] result = ReadBytes(count);
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads the specified number of bytes from the offset into the buffer starting at the specified index without advancing the stream.
        /// </summary>
        public void GetBytes(long offset, byte[] buffer, int index, int count)
        {
            StepIn(offset);
            ReadBytes(buffer, index, count);
            StepOut();
        }

        /// <summary>
        /// Reads a one-byte unsigned integer and throws an exception if it does not match any of the specified options.
        /// </summary>
        public byte AssertByte(params ReadOnlySpan<byte> options)
        {
            return AssertValue(ReadByte(), "Byte", "0x{0:X}", options);
        }

        /// <summary>
        /// Reads a one-byte unsigned integer and throws an exception if it does not match the specified option.
        /// </summary>
        public byte AssertByte(byte option)
        {
            return AssertValue(ReadByte(), "Byte", "0x{0:X}", option);
        }
        #endregion

        #region Int16
        /// <summary>
        /// Reads a two-byte signed integer.
        /// </summary>
        public short ReadInt16()
        {
            return Take<short>();
        }

        /// <summary>
        /// Reads an array of two-byte signed integers.
        /// </summary>
        public short[] ReadInt16s(int count)
        {
            return ReadArray<short>(count);
        }

        /// <summary>
        /// Reads a two-byte signed integer from the specified offset without advancing the stream.
        /// </summary>
        public short GetInt16(long offset)
        {
            return GetValue<short>(offset);
        }

        /// <summary>
        /// Reads an array of two-byte signed integers from the specified offset without advancing the stream.
        /// </summary>
        public short[] GetInt16s(long offset, int count)
        {
            return GetArray<short>(offset, count);
        }

        /// <summary>
        /// Reads a two-byte signed integer and throws an exception if it does not match any of the specified options.
        /// </summary>
        public short AssertInt16(params ReadOnlySpan<short> options)
        {
            return AssertValue(ReadInt16(), "Int16", "0x{0:X}", options);
        }

        /// <summary>
        /// Reads a two-byte signed integer and throws an exception if it does not match the specified option.
        /// </summary>
        public short AssertInt16(short option)
        {
            return AssertValue(ReadInt16(), "Int16", "0x{0:X}", option);
        }
        #endregion

        #region UInt16
        /// <summary>
        /// Reads a two-byte unsigned integer.
        /// </summary>
        public ushort ReadUInt16()
        {
            return Take<ushort>();
        }

        /// <summary>
        /// Reads an array of two-byte unsigned integers.
        /// </summary>
        public ushort[] ReadUInt16s(int count)
        {
            return ReadArray<ushort>(count);
        }

        /// <summary>
        /// Reads a two-byte unsigned integer from the specified position without advancing the stream.
        /// </summary>
        public ushort GetUInt16(long offset)
        {
            return GetValue<ushort>(offset);
        }

        /// <summary>
        /// Reads an array of two-byte unsigned integers from the specified position without advancing the stream.
        /// </summary>
        public ushort[] GetUInt16s(long offset, int count)
        {
            return GetArray<ushort>(offset, count);
        }

        /// <summary>
        /// Reads a two-byte unsigned integer and throws an exception if it does not match any of the specified options.
        /// </summary>
        public ushort AssertUInt16(params ReadOnlySpan<ushort> options)
        {
            return AssertValue(ReadUInt16(), "UInt16", "0x{0:X}", options);
        }

        /// <summary>
        /// Reads a two-byte unsigned integer and throws an exception if it does not match the specified option.
        /// </summary>
        public ushort AssertUInt16(ushort option)
        {
            return AssertValue(ReadUInt16(), "UInt16", "0x{0:X}", option);
        }
        #endregion

        #region Int32
        /// <summary>
        /// Reads a four-byte signed integer.
        /// </summary>
        public int ReadInt32()
        {
            return Take<int>();
        }

        /// <summary>
        /// Reads an array of four-byte signed integers.
        /// </summary>
        public int[] ReadInt32s(int count)
        {
            return ReadArray<int>(count);
        }

        /// <summary>
        /// Reads a four-byte signed integer from the specified position without advancing the stream.
        /// </summary>
        public int GetInt32(long offset)
        {
            return GetValue<int>(offset);
        }

        /// <summary>
        /// Reads an array of four-byte signed integers from the specified position without advancing the stream.
        /// </summary>
        public int[] GetInt32s(long offset, int count)
        {
            return GetArray<int>(offset, count);
        }

        /// <summary>
        /// Reads a four-byte signed integer and throws an exception if it does not match any of the specified options.
        /// </summary>
        public int AssertInt32(params ReadOnlySpan<int> options)
        {
            return AssertValue(ReadInt32(), "Int32", "0x{0:X}", options);
        }

        /// <summary>
        /// Reads a four-byte signed integer and throws an exception if it does not match the specified option.
        /// </summary>
        public int AssertInt32(int option)
        {
            return AssertValue(ReadInt32(), "Int32", "0x{0:X}", option);
        }
        #endregion

        #region UInt32
        /// <summary>
        /// Reads a four-byte unsigned integer.
        /// </summary>
        public uint ReadUInt32()
        {
            return Take<uint>();
        }

        /// <summary>
        /// Reads an array of four-byte unsigned integers.
        /// </summary>
        public uint[] ReadUInt32s(int count)
        {
            return ReadArray<uint>(count);
        }

        /// <summary>
        /// Reads a four-byte unsigned integer from the specified position without advancing the stream.
        /// </summary>
        public uint GetUInt32(long offset)
        {
            return GetValue<uint>(offset);
        }

        /// <summary>
        /// Reads an array of four-byte unsigned integers from the specified position without advancing the stream.
        /// </summary>
        public uint[] GetUInt32s(long offset, int count)
        {
            return GetArray<uint>(offset, count);
        }

        /// <summary>
        /// Reads a four-byte unsigned integer and throws an exception if it does not match any of the specified options.
        /// </summary>
        public uint AssertUInt32(params ReadOnlySpan<uint> options)
        {
            return AssertValue(ReadUInt32(), "UInt32", "0x{0:X}", options);
        }

        /// <summary>
        /// Reads a four-byte unsigned integer and throws an exception if it does not match the specified option.
        /// </summary>
        public uint AssertUInt32(uint option)
        {
            return AssertValue(ReadUInt32(), "UInt32", "0x{0:X}", option);
        }
        #endregion

        #region Int64
        /// <summary>
        /// Reads an eight-byte signed integer.
        /// </summary>
        public long ReadInt64()
        {
            return Take<long>();
        }

        /// <summary>
        /// Reads an array of eight-byte signed integers.
        /// </summary>
        public long[] ReadInt64s(int count)
        {
            return ReadArray<long>(count);
        }

        /// <summary>
        /// Reads an eight-byte signed integer from the specified position without advancing the stream.
        /// </summary>
        public long GetInt64(long offset)
        {
            return GetValue<long>(offset);
        }

        /// <summary>
        /// Reads an array eight-byte signed integers from the specified position without advancing the stream.
        /// </summary>
        public long[] GetInt64s(long offset, int count)
        {
            return GetArray<long>(offset, count);
        }

        /// <summary>
        /// Reads an eight-byte signed integer and throws an exception if it does not match any of the specified options.
        /// </summary>
        public long AssertInt64(params ReadOnlySpan<long> options)
        {
            return AssertValue(ReadInt64(), "Int64", "0x{0:X}", options);
        }

        /// <summary>
        /// Reads an eight-byte signed integer and throws an exception if it does not match the specified option.
        /// </summary>
        public long AssertInt64(long option)
        {
            return AssertValue(ReadInt64(), "Int64", "0x{0:X}", option);
        }
        #endregion

        #region UInt64
        /// <summary>
        /// Reads an eight-byte unsigned integer.
        /// </summary>
        public ulong ReadUInt64()
        {
            return Take<ulong>();
        }

        /// <summary>
        /// Reads an array of eight-byte unsigned integers.
        /// </summary>
        public ulong[] ReadUInt64s(int count)
        {
            return ReadArray<ulong>(count);
        }

        /// <summary>
        /// Reads an eight-byte unsigned integer from the specified position without advancing the stream.
        /// </summary>
        public ulong GetUInt64(long offset)
        {
            return GetValue<ulong>(offset);
        }

        /// <summary>
        /// Reads an array of eight-byte unsigned integers from the specified position without advancing the stream.
        /// </summary>
        public ulong[] GetUInt64s(long offset, int count)
        {
            return GetArray<ulong>(offset, count);
        }

        /// <summary>
        /// Reads an eight-byte unsigned integer and throws an exception if it does not match any of the specified options.
        /// </summary>
        public ulong AssertUInt64(params ReadOnlySpan<ulong> options)
        {
            return AssertValue(ReadUInt64(), "UInt64", "0x{0:X}", options);
        }

        /// <summary>
        /// Reads an eight-byte unsigned integer and throws an exception if it does not match the specified option.
        /// </summary>
        public ulong AssertUInt64(ulong option)
        {
            return AssertValue(ReadUInt64(), "UInt64", "0x{0:X}", option);
        }
        #endregion

        #region Varint
        /// <summary>
        /// Reads either a four or eight-byte signed integer depending on VarintLong.
        /// </summary>
        public long ReadVarint()
        {
            if (VarintLong)
                return ReadInt64();
            else
                return ReadInt32();
        }

        /// <summary>
        /// Reads an array of either four or eight-byte signed integers depending on VarintLong.
        /// </summary>
        public long[] ReadVarints(int count)
        {
            long[] result = new long[count];
            for (int i = 0; i < count; i++)
            {
                if (VarintLong)
                    result[i] = ReadInt64();
                else
                    result[i] = ReadInt32();
            }
            return result;
        }

        /// <summary>
        /// Reads either a four or eight-byte signed integer depending on VarintLong from the specified position without advancing the stream.
        /// </summary>
        public long GetVarint(long offset)
        {
            if (VarintLong)
                return GetInt64(offset);
            else
                return GetInt32(offset);
        }

        /// <summary>
        /// Reads an array of either four or eight-byte signed integers depending on VarintLong from the specified position without advancing the stream.
        /// </summary>
        public long[] GetVarints(long offset, int count)
        {
            StepIn(offset);
            long[] result = ReadVarints(count);
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads either a four or eight-byte signed integer depending on VarintLong and throws an exception if it does not match any of the specified options.
        /// </summary>
        public long AssertVarint(params ReadOnlySpan<long> options)
        {
            return AssertValue(ReadVarint(), VarintLong ? "Varint64" : "Varint32", "0x{0:X}", options);
        }


        /// <summary>
        /// Reads either a four or eight-byte signed integer depending on VarintLong and throws an exception if it does not match the specified option.
        /// </summary>
        public long AssertVarint(long option)
        {
            return AssertValue(ReadVarint(), VarintLong ? "Varint64" : "Varint32", "0x{0:X}", option);
        }
        #endregion

        #region Single
        /// <summary>
        /// Reads a four-byte floating point number.
        /// </summary>
        public float ReadSingle()
        {
            return Take<float>();
        }

        /// <summary>
        /// Reads an array of four-byte floating point numbers.
        /// </summary>
        public float[] ReadSingles(int count)
        {
            return ReadArray<float>(count);
        }

        /// <summary>
        /// Reads a four-byte floating point number from the specified position without advancing the stream.
        /// </summary>
        public float GetSingle(long offset)
        {
            return GetValue<float>(offset);
        }

        /// <summary>
        /// Reads an array of four-byte floating point numbers from the specified position without advancing the stream.
        /// </summary>
        public float[] GetSingles(long offset, int count)
        {
            return GetArray<float>(offset, count);
        }

        /// <summary>
        /// Reads a four-byte floating point number and throws an exception if it does not match any of the specified options.
        /// </summary>
        public float AssertSingle(params ReadOnlySpan<float> options)
        {
            return AssertValue(ReadSingle(), "Single", "{0}", options);
        }


        /// <summary>
        /// Reads a four-byte floating point number and throws an exception if it does not match the specified option.
        /// </summary>
        public float AssertSingle(float option)
        {
            return AssertValue(ReadSingle(), "Single", "{0}", option);
        }
        #endregion

        #region Double
        /// <summary>
        /// Reads an eight-byte floating point number.
        /// </summary>
        public double ReadDouble()
        {
            return Take<double>();
        }

        /// <summary>
        /// Reads an array of eight-byte floating point numbers.
        /// </summary>
        public double[] ReadDoubles(int count)
        {
            return ReadArray<double>(count);
        }

        /// <summary>
        /// Reads an eight-byte floating point number from the specified position without advancing the stream.
        /// </summary>
        public double GetDouble(long offset)
        {
            return GetValue<double>(offset);
        }

        /// <summary>
        /// Reads an array of eight-byte floating point numbers from the specified position without advancing the stream.
        /// </summary>
        public double[] GetDoubles(long offset, int count)
        {
            return GetArray<double>(offset, count);
        }

        /// <summary>
        /// Reads an eight-byte floating point number and throws an exception if it does not match any of the specified options.
        /// </summary>
        public double AssertDouble(params ReadOnlySpan<double> options)
        {
            return AssertValue(ReadDouble(), "Double", "{0}", options);
        }

        /// <summary>
        /// Reads an eight-byte floating point number and throws an exception if it does not match the specified option.
        /// </summary>
        public double AssertDouble(double option)
        {
            return AssertValue(ReadDouble(), "Double", "{0}", option);
        }
        #endregion

        #region Enum
        private static TEnum TransmuteEnum<TEnum, TValue>(TValue value, string valueFormat) where TEnum : struct, Enum where TValue : struct
        {
            TEnum result = Unsafe.As<TValue, TEnum>(ref value);
            if (!Enum.IsDefined(result))
                throw new InvalidDataException($"Read Byte not present in enum: {string.Format(valueFormat, value)}");

            return result;
        }

        /// <summary>
        /// Reads a one-byte value as the specified enum, throwing an exception if not present.
        /// </summary>
        public TEnum ReadEnum8<TEnum>() where TEnum : struct, Enum
        {
            Type typ = Enum.GetUnderlyingType(typeof(TEnum));
            if (typ == typeof(byte))
                return TransmuteEnum<TEnum, byte>(ReadByte(), "0x{0:X}");
            if (typ == typeof(sbyte))
                return TransmuteEnum<TEnum, sbyte>(ReadSByte(), "0x{0:X}");
            throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {typ.Name}");
        }


        /// <summary>
        /// Reads a one-byte enum from the specified position without advancing the stream.
        /// </summary>
        public TEnum GetEnum8<TEnum>(long position) where TEnum : struct, Enum
        {
            StepIn(position);
            TEnum result = ReadEnum8<TEnum>();
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads a two-byte value as the specified enum, throwing an exception if not present.
        /// </summary>
        public TEnum ReadEnum16<TEnum>() where TEnum : struct, Enum
        {
            Type typ = Enum.GetUnderlyingType(typeof(TEnum));
            if (typ == typeof(short))
                return TransmuteEnum<TEnum, short>(ReadInt16(), "0x{0:X}");
            if (typ == typeof(ushort))
                return TransmuteEnum<TEnum, ushort>(ReadUInt16(), "0x{0:X}");
            throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {typ.Name}");
        }

        /// <summary>
        /// Reads a two-byte enum from the specified position without advancing the stream.
        /// </summary>
        public TEnum GetEnum16<TEnum>(long position) where TEnum : struct, Enum
        {
            StepIn(position);
            TEnum result = ReadEnum16<TEnum>();
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads a four-byte value as the specified enum, throwing an exception if not present.
        /// </summary>
        public TEnum ReadEnum32<TEnum>() where TEnum : struct, Enum
        {
            Type typ = Enum.GetUnderlyingType(typeof(TEnum));
            if (typ == typeof(int))
                return TransmuteEnum<TEnum, int>(ReadInt32(), "0x{0:X}");
            if (typ == typeof(uint))
                return TransmuteEnum<TEnum, uint>(ReadUInt32(), "0x{0:X}");
            throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {typ.Name}");
        }

        /// <summary>
        /// Reads a four-byte enum from the specified position without advancing the stream.
        /// </summary>
        public TEnum GetEnum32<TEnum>(long position) where TEnum : struct, Enum
        {
            StepIn(position);
            TEnum result = ReadEnum32<TEnum>();
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads an eight-byte value as the specified enum, throwing an exception if not present.
        /// </summary>
        public TEnum ReadEnum64<TEnum>() where TEnum : struct, Enum
        {
            Type typ = Enum.GetUnderlyingType(typeof(TEnum));
            if (typ == typeof(long))
                return TransmuteEnum<TEnum, long>(ReadInt64(), "0x{0:X}");
            if (typ == typeof(ulong))
                return TransmuteEnum<TEnum, ulong>(ReadUInt64(), "0x{0:X}");
            throw new InvalidDataException($"Enum {typeof(TEnum).Name} has an invalid underlying value type: {typ.Name}");
        }

        /// <summary>
        /// Reads an eight-byte enum from the specified position without advancing the stream.
        /// </summary>
        public TEnum GetEnum64<TEnum>(long position) where TEnum : struct, Enum
        {
            StepIn(position);
            TEnum result = ReadEnum64<TEnum>();
            StepOut();
            return result;
        }
        #endregion

        #region String
        /// <summary>
        /// Reads the specified number of bytes and interprets them according to the specified encoding.
        /// </summary>
        private string ReadChars(Encoding encoding, int length)
        {
            return encoding.GetString(Take(length));
        }

        private void ReadInto(Span<byte> bytes)
        {
            ReadOnlySpan<byte> buffered = Buffered;
            if (bytes.Length <= buffered.Length)
            {
                buffered[..bytes.Length].CopyTo(bytes);
                Consume(bytes.Length);
                return;
            }

            ThrowIfDisposed();

            buffered.CopyTo(bytes);
            Consume(buffered.Length);
            bytes = bytes[buffered.Length..];

            // If we exhaust the bytes currently available in the slice then read into the users buffer
            // so we can potentially avoid an intermediate copy.
            if (TryReadUnbuffered(bytes))
                return;

            buffered = EnsureBuffered(bytes.Length);
            if (buffered.Length < bytes.Length)
                throw EndOfData();

            buffered[..bytes.Length].CopyTo(bytes);
            Consume(bytes.Length);
        }

        private ReadOnlySpan<byte> TakeNulTerminated(int charWidth)
        {
            ThrowIfDisposed();

            int searched = 0;
            while (true)
            {
                ReadOnlySpan<byte> buffered = EnsureBuffered(searched + charWidth);

                int found = charWidth == 1
                    ? buffered[searched..].IndexOf((byte)0)
                    : MemoryMarshal.Cast<byte, char>(buffered.Slice(searched, (buffered.Length - searched) & ~1)).IndexOf('\0');

                if (found >= 0)
                {
                    int length = searched + found * charWidth;
                    Consume(length + charWidth);
                    return buffered[..length];
                }

                if (buffered.Length < searched + charWidth)
                    throw EndOfData();

                searched = buffered.Length & ~(charWidth - 1);
            }
        }

        /// <summary>
        /// Reads bytes until a single-byte null terminator is found, then interprets them according to the specified encoding.
        /// </summary>
        private string ReadCharsTerminated(Encoding encoding)
        {
            return encoding.GetString(TakeNulTerminated(1));
        }

        /// <summary>
        /// Reads a null-terminated ASCII string.
        /// </summary>
        public string ReadASCII()
        {
            return ReadCharsTerminated(SFEncoding.ASCII);
        }

        /// <summary>
        /// Reads an ASCII string with the specified length in bytes.
        /// </summary>
        public string ReadASCII(int length)
        {
            return ReadChars(SFEncoding.ASCII, length);
        }

        /// <summary>
        /// Reads a null-terminated ASCII string from the specified position without advancing the stream.
        /// </summary>
        public string GetASCII(long offset)
        {
            StepIn(offset);
            string result = ReadASCII();
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads an ASCII string with the specified length in bytes from the specified position without advancing the stream.
        /// </summary>
        public string GetASCII(long offset, int length)
        {
            if (TryPeek(offset, length, out var bytes))
                return SFEncoding.ASCII.GetString(bytes);

            StepIn(offset);
            string result = ReadASCII(length);
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads as many ASCII characters as are in the specified value and throws an exception if they do not match.
        /// </summary>
        public string AssertASCII(params string[] values)
        {
            string s = ReadASCII(values[0].Length);
            bool valid = false;
            foreach (string value in values)
                if (s == value)
                    valid = true;

            if (!valid)
                throw new InvalidDataException(string.Format(
                    "Read ASCII: {0} | Expected ASCII: {1}", s, string.Join(", ", values)));

            return s;
        }

        /// <summary>
        /// Reads a null-terminated Shift JIS string.
        /// </summary>
        public string ReadShiftJIS()
        {
            return ReadCharsTerminated(SFEncoding.ShiftJIS);
        }

        /// <summary>
        /// Reads a Shift JIS string with the specified length in bytes.
        /// </summary>
        public string ReadShiftJIS(int length)
        {
            return ReadChars(SFEncoding.ShiftJIS, length);
        }

        /// <summary>
        /// Reads a null-terminated Shift JIS string from the specified position without advancing the stream.
        /// </summary>
        public string GetShiftJIS(long offset)
        {
            StepIn(offset);
            string result = ReadShiftJIS();
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads a Shift JIS string with the specified length in bytes from the specified position without advancing the stream.
        /// </summary>
        public string GetShiftJIS(long offset, int length)
        {
            if (TryPeek(offset, length, out var bytes))
                return SFEncoding.ShiftJIS.GetString(bytes);

            StepIn(offset);
            string result = ReadShiftJIS(length);
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads a null-terminated UTF-16 string.
        /// </summary>
        public string ReadUTF16()
        {
            ReadOnlySpan<byte> text = TakeNulTerminated(2);
            return BigEndian ? SFEncoding.UTF16BE.GetString(text) : SFEncoding.UTF16.GetString(text);
        }

        /// <summary>
        /// Reads a null-terminated UTF-16 string from the specified position without advancing the stream.
        /// </summary>
        public string GetUTF16(long offset)
        {
            StepIn(offset);
            string result = ReadUTF16();
            StepOut();
            return result;
        }

        /// <summary>
        /// Reads a null-terminated Shift JIS string in a fixed-size field.
        /// </summary>
        public string ReadFixStr(int size)
        {
            ReadOnlySpan<byte> bytes = Take(size);
            int terminator = bytes.IndexOf((byte)0);
            return SFEncoding.ShiftJIS.GetString(terminator < 0 ? bytes : bytes[..terminator]);
        }

        /// <summary>
        /// Reads a null-terminated UTF-16 string in a fixed-size field.
        /// </summary>
        public string ReadFixStrW(int size)
        {
            ReadOnlySpan<byte> bytes = Take(size);
            int units = MemoryMarshal.Cast<byte, char>(bytes[..(size & ~1)]).IndexOf('\0');
            ReadOnlySpan<byte> text = units < 0 ? bytes : bytes[..(units * 2)];

            if (BigEndian)
                return SFEncoding.UTF16BE.GetString(text);
            else
                return SFEncoding.UTF16.GetString(text);
        }
        #endregion

        #region Other
        /// <summary>
        /// Reads a vector of two four-byte floating point numbers.
        /// </summary>
        public Vector2 ReadVector2()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            return new Vector2(x, y);
        }

        /// <summary>
        /// Reads a vector of three four-byte floating point numbers.
        /// </summary>
        public Vector3 ReadVector3()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Reads a vector of four four-byte floating point numbers.
        /// </summary>
        public Vector4 ReadVector4()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            float w = ReadSingle();
            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Reads a Quaternion of floating point numbers in XYZW order.
        /// </summary>
        public Quaternion ReadQuaternion()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            float w = ReadSingle();
            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Reads a Vector3 from an int. Expects a Vector3 of floating points compressed to 11, 11, and 10 bits per float.
        /// </summary>
        public Vector3 Read11_11_10Vector3()
        {
            int vector = ReadInt32();
            int x = vector << 21 >> 21;
            int y = vector << 10 >> 21;
            int z = vector << 0 >> 22;
            return new Vector3(x / (float)0b11_1111_1111, y / (float)0b11_1111_1111, z / (float)0b1_1111_1111);
        }

        /// <summary>
        /// Read length number of bytes and assert that they all match the given value.
        /// </summary>
        public void AssertPattern(int length, byte pattern)
        {
            ReadOnlySpan<byte> bytes = Take(length);

            int i = bytes.IndexOfAnyExcept(pattern);
            if (i >= 0)
                throw new InvalidDataException($"Expected {length} 0x{pattern:X2}, got {bytes[i]:X2} at position {i}");
        }

        /// <summary>
        /// Reads a 4-byte color in ARGB order.
        /// </summary>
        public Color ReadARGB()
        {
            ReadOnlySpan<byte> bytes = Take(4);
            byte a = bytes[0];
            byte r = bytes[1];
            byte g = bytes[2];
            byte b = bytes[3];
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Reads a 4-byte color in ABGR order.
        /// </summary>
        public Color ReadABGR()
        {
            ReadOnlySpan<byte> bytes = Take(4);
            byte a = bytes[0];
            byte b = bytes[1];
            byte g = bytes[2];
            byte r = bytes[3];
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Reads a 4-byte color in RGBA order.
        /// </summary>
        public Color ReadRGBA()
        {
            ReadOnlySpan<byte> bytes = Take(4);
            byte r = bytes[0];
            byte g = bytes[1];
            byte b = bytes[2];
            byte a = bytes[3];
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Reads a 4-byte color in BGRA order.
        /// </summary>
        public Color ReadBGRA()
        {
            ReadOnlySpan<byte> bytes = Take(4);
            byte b = bytes[0];
            byte g = bytes[1];
            byte r = bytes[2];
            byte a = bytes[3];
            return Color.FromArgb(a, r, g, b);
        }
        #endregion

        #region IDisposable Support

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (stream != null)
                    {
                        if (!leaveOpen)
                            stream.Dispose();

                        slice.Release();
                    }
                    else
                    {
                        slice = default;
                    }

                    steps.Clear();
                }

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
        
        private struct IoSlice
        {
            public byte[] Buffer;
            public long Start;
            public int Length;
            public int Cursor;

            public readonly long Position => Start + Cursor;

            public void Compact(int count, int minimumSize)
            {
                int kept = Math.Max(0, Length - Cursor);

                if (Buffer.Length < count)
                {
                    byte[] grown = ArrayPool<byte>.Shared.Rent(Math.Max(minimumSize, count));
                    if (kept > 0)
                        Array.Copy(Buffer, Cursor, grown, 0, kept);
                    ArrayPool<byte>.Shared.Return(Buffer);
                    Buffer = grown;
                }
                else if (kept > 0 && Cursor > 0)
                {
                    Array.Copy(Buffer, Cursor, Buffer, 0, kept);
                }

                Start += Cursor;
                Cursor = 0;
                Length = kept;
            }

            public void Release()
            {
                if (Buffer != null)
                    ArrayPool<byte>.Shared.Return(Buffer);

                this = default;
            }
        }

        private void SetPosition(long value)
        {
            if (stream == null)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);
                slice.Cursor = (int)value;
                return;
            }

            if (value < 0)
                stream.Position = value;

            if (value >= slice.Start && value <= slice.Start + slice.Length)
            {
                slice.Cursor = (int)(value - slice.Start);
            }
            else if (!stream.CanSeek)
            {
                stream.Position = value;
            }
            else
            {
                slice.Start = value;
                slice.Length = 0;
                slice.Cursor = 0;
            }
        }

        private bool Fill(int count)
        {
            if (stream == null)
                return false;

            slice.Compact(count, BufferSize);

            long end = slice.Start + slice.Length;
            if (stream.CanSeek && stream.Position != end)
                stream.Position = end;

            while (slice.Length < count)
            {
                int read = stream.Read(slice.Buffer, slice.Length, slice.Buffer.Length - slice.Length);
                if (read == 0)
                    return false;
                slice.Length += read;
            }

            return true;
        }

        private bool TryReadUnbuffered(Span<byte> bytes)
        {
            if (stream == null || bytes.Length < BufferSize)
                return false;

            long position = slice.Position;
            if (stream.CanSeek && stream.Position != position)
                stream.Position = position;

            int read = 0;
            try
            {
                while (read < bytes.Length)
                {
                    int more = stream.Read(bytes[read..]);
                    if (more == 0)
                        throw new EndOfStreamException("Remaining size of stream was smaller than requested number of bytes.");
                    read += more;
                }
            }
            finally
            {
                slice.Start = position + read;
                slice.Length = 0;
                slice.Cursor = 0;
            }

            return true;
        }
    }
}
