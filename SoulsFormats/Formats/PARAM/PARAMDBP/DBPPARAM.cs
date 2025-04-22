using System;
using System.Collections.Generic;
using System.IO;

namespace SoulsFormats
{
    /// <summary>
    /// A DBP param containing several fields.
    /// </summary>
    public class DBPPARAM : SoulsFile<DBPPARAM>
    {
        /// <summary>
        /// Whether or not the DBPPARAM is in big endian.
        /// </summary>
        public bool BigEndian { get; set; } = true;

        /// <summary>
        /// Whether or not a dbp is currently applied.
        /// </summary>
        public bool DbpApplied => AppliedParamDbp != null;

        /// <summary>
        /// The currently applied PARAMDBP.
        /// </summary>
        public PARAMDBP AppliedParamDbp { get; private set; }

        /// <summary>
        /// Cells contained in this row. Must be loaded with PARAM.ApplyParamDbp() before use.
        /// </summary>
        public IReadOnlyList<Cell> Cells { get; private set; }

        /// <summary>
        /// The raw stream of the DBPPARAM.
        /// </summary>
        private BinaryReaderEx CellReader;

        /// <summary>
        /// Create a new, empty DBPPARAM.
        /// </summary>
        public DBPPARAM()
        {
            
        }

        /// <summary>
        /// Create a DBPPARAM using its DBP.
        /// </summary>
        /// <param name="dbp">The DBP of this DBPPARAM.</param>
        public DBPPARAM(PARAMDBP dbp)
        {
            AppliedParamDbp = dbp;
            var cells = new Cell[dbp.Fields.Count];
            for (int i = 0; i < dbp.Fields.Count; i++)
            {
                PARAMDBP.Field field = dbp.Fields[i];
                cells[i] = new Cell(field, field.Default);
            }
            Cells = cells;
        }

        /// <summary>
        /// Set the reader of the DBPPARAM for use when applying the PARAMDBP.
        /// </summary>
        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = BigEndian;

            // Make a private copy of the file to read row data from later
            byte[] copy = br.GetBytes(0, (int)br.Stream.Length);
            CellReader = new BinaryReaderEx(br.BigEndian, copy);
        }

        /// <summary>
        /// Serialize DBPPARAM cell values to a stream.
        /// </summary>
        protected override void Write(BinaryWriterEx bw)
        {
            bw.BigEndian = BigEndian;
            foreach (var cell in Cells)
            {
                switch (cell.Dbp.Type)
                {
                    case PARAMDEF.DefType.s8:
                        bw.WriteSByte(Convert.ToSByte(cell.Value));
                        break;
                    case PARAMDEF.DefType.u8:
                        bw.WriteByte(Convert.ToByte(cell.Value));
                        break;
                    case PARAMDEF.DefType.s16:
                        bw.WriteInt16(Convert.ToInt16(cell.Value));
                        break;
                    case PARAMDEF.DefType.u16:
                        bw.WriteUInt16(Convert.ToUInt16(cell.Value));
                        break;
                    case PARAMDEF.DefType.s32:
                        bw.WriteInt32(Convert.ToInt32(cell.Value));
                        break;
                    case PARAMDEF.DefType.u32:
                        bw.WriteUInt32(Convert.ToUInt32(cell.Value));
                        break;
                    case PARAMDEF.DefType.f32:
                        bw.WriteSingle(Convert.ToSingle(cell.Value));
                        break;
                    default:
                        throw new NotImplementedException($"{nameof(PARAMDEF.DefType)}: {cell.Dbp.Type} invalid or not implemented.");
                }
            }
        }

        /// <summary>
        /// Apply a dbp to this param, checking a list of dbps for matches.
        /// Returns true if a match was found.
        /// </summary>
        /// <param name="dbps">The dbps to check for a match.</param>
        /// <returns>Whether or not a match was found.</returns>
        public bool ApplyParamDbp(IEnumerable<PARAMDBP> dbps)
        {
            foreach (PARAMDBP dbp in dbps)
                if (ApplyParamDbp(dbp))
                    return true;
            return false;
        }

        /// <summary>
        /// Apply a dbp to this param, returning true if it applied.
        /// </summary>
        /// <param name="dbp">The PARAMDBP to apply.</param>
        public bool ApplyParamDbp(PARAMDBP dbp)
        {
            int dbpParamSize = dbp.CalculateParamSize();
            if (CellReader.Length < dbpParamSize)
                return false;
            CellReader.Position = 0;

            var cells = new Cell[dbp.Fields.Count];
            for (int i = 0; i < dbp.Fields.Count; i++)
            {
                PARAMDBP.Field field = dbp.Fields[i];
                object value = ReadCellValue(CellReader, field.Type);
                cells[i] = new Cell(field, value);
            }
            Cells = cells;

            AppliedParamDbp = dbp;
            return true;
        }

        /// <summary>
        /// Read the value of a cell using its display type.
        /// </summary>
        /// <param name="br">A BinaryReaderEx stream representing the DBPPARAM.</param>
        /// <param name="type">The display type of the cell.</param>
        /// <returns>The value of the cell as an object.</returns>
        /// <exception cref="NotImplementedException">If the provided display type does is not supported or does not exist.</exception>
        private object ReadCellValue(BinaryReaderEx br, PARAMDEF.DefType type)
        {
            object value;
            switch (type)
            {
                case PARAMDEF.DefType.s8:
                    value = br.ReadSByte();
                    break;
                case PARAMDEF.DefType.u8:
                    value = br.ReadByte();
                    break;
                case PARAMDEF.DefType.s16:
                    value = br.ReadInt16();
                    break;
                case PARAMDEF.DefType.u16:
                    value = br.ReadUInt16();
                    break;
                case PARAMDEF.DefType.s32:
                    value = br.ReadInt32();
                    break;
                case PARAMDEF.DefType.u32:
                    value = br.ReadUInt32();
                    break;
                case PARAMDEF.DefType.f32:
                    value = br.ReadSingle();
                    break;
                default:
                    throw new NotImplementedException($"{nameof(PARAMDEF.DefType)}: {type} invalid or not implemented.");
            }
            return value;
        }

        /// <summary>
        /// A single value in a PARAM.
        /// </summary>
        public class Cell
        {
            /// <summary>
            /// The PARAMDBP Field that describes this cell.
            /// </summary>
            public PARAMDBP.Field Dbp { get; }

            /// <summary>
            /// The DisplayType of the cell from its dbp field as a string.
            /// </summary>
            public PARAMDEF.DefType Type
                => Dbp.Type;

            /// <summary>
            /// The description of the cell from its dbp field.
            /// </summary>
            public string Name
                => Dbp.Name;

            /// <summary>
            /// The DisplayFormat of the cell from its dbp field.
            /// </summary>
            public string Format
                => Dbp.Format;

            /// <summary>
            /// The default editor value of the cell from its dbp field.
            /// </summary>
            public object Default
                => Dbp.Default;

            /// <summary>
            /// The maximum editor value of the cell from its dbp field.
            /// </summary>
            public object Maximum
                => Dbp.Maximum;

            /// <summary>
            /// The minimum editor value of the cell from its dbp field.
            /// </summary>
            public object Minimum
                => Dbp.Minimum;

            /// <summary>
            /// The increment editor value of the cell from its dbp field.
            /// </summary>
            public object Increment
                => Dbp.Increment;

            /// <summary>
            /// The value of this cell.
            /// </summary>
            private object _Value;

            /// <summary>
            /// The value of this cell.
            /// </summary>
            public object Value
            {
                get => _Value;
                set
                {
                    if (value == null)
                        throw new NullReferenceException($"Cell value may not be null.");

                    switch (Dbp.Type)
                    {
                        case PARAMDEF.DefType.s8: _Value = Convert.ToSByte(value); break;
                        case PARAMDEF.DefType.u8: _Value = Convert.ToByte(value); break;
                        case PARAMDEF.DefType.s16: _Value = Convert.ToInt16(value); break;
                        case PARAMDEF.DefType.u16: _Value = Convert.ToUInt16(value); break;
                        case PARAMDEF.DefType.s32: _Value = Convert.ToInt32(value); break;
                        case PARAMDEF.DefType.u32: _Value = Convert.ToUInt32(value); break;
                        case PARAMDEF.DefType.f32: _Value = Convert.ToSingle(value); break;
                        default:
                            throw new NotImplementedException($"Conversion not specified for {nameof(PARAMDEF.DefType)}: {Dbp.Type}");
                    }
                }
            }

            /// <summary>
            /// Create a new DBPPARAM cell.
            /// </summary>
            /// <param name="dbp">The DBP field to apply to this cell.</param>
            /// <param name="value">The value to set to this cell.</param>
            internal Cell(PARAMDBP.Field dbp, object value)
            {
                Dbp = dbp;
                Value = value;
            }

            /// <summary>
            /// Returns a string representation of the cell.
            /// </summary>
            public override string ToString()
            {
                return $"{Dbp.Type} {Dbp.Name} = {Value}";
            }
        }
    }
}