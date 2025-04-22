using System;
using System.Collections.Generic;

namespace SoulsFormats
{
    /// <summary>
    /// A companion format to dbp params that describes each field present in a dbp param. Extension: .dbp
    /// </summary>
    public partial class PARAMDBP : SoulsFile<PARAMDBP>
    {
        /// <summary>
        /// Whether or not the dbp should be written in bigendian.
        /// </summary>
        public bool BigEndian { get; set; }

        /// <summary>
        /// The fields in this PARAMDBP.
        /// </summary>
        public List<Field> Fields { get; set; }

        /// <summary>
        /// Creates a new, empty PARAMDBP.
        /// </summary>
        public PARAMDBP()
        {
            BigEndian = true;
            Fields = new List<Field>();
        }

        /// <summary>
        /// Create a new PARAMDBP from PARAMDEF.
        /// </summary>
        /// <param name="def">A PARAMDEF.</param>
        public PARAMDBP(PARAMDEF def)
        {
            var dbp = new PARAMDBP
            {
                BigEndian = def.BigEndian,
                Compression = def.Compression,
                Fields = new List<Field>(def.Fields.Count)
            };

            foreach (var field in def.Fields)
            {
                var dbpfield = new Field
                {
                    Type = field.DisplayType,
                    Name = field.DisplayName,
                    Format = field.DisplayFormat ?? ParamUtil.GetDefaultFormat(field.DisplayType),
                    Default = field.Default,
                    Minimum = field.Minimum,
                    Maximum = field.Maximum,
                    Increment = field.Increment
                };

                dbp.Fields.Add(dbpfield);
            }
        }

        /// <summary>
        /// Deserializes file data from a stream.
        /// </summary>
        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = true;
            int fieldCount = br.ReadInt32();

            // Verify endianness
            // Some depreciated dbps in AC4 use little-endian.
            if (fieldCount > br.Length)
            {
                br.Position = 0;
                br.BigEndian = false;
                fieldCount = br.ReadInt32();
            }

            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertPattern(fieldCount * 4, 0);
            Fields = new List<Field>();
            for (int i = 0; i < fieldCount; i++)
            {
                Fields.Add(new Field(br));
            }

            for (int i = 0; i < fieldCount; i++)
            {
                Fields[i].Name = br.ReadShiftJIS();
                Fields[i].Format = br.ReadShiftJIS();
            }
        }

        /// <summary>
        /// Serializes file data to a stream.
        /// </summary>
        protected override void Write(BinaryWriterEx bw)
        {
            bw.BigEndian = BigEndian;
            bw.WriteInt32(Fields.Count);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WritePattern(Fields.Count * 4, 0);
            for (int i = 0; i < Fields.Count; i++)
            {
                Fields[i].Write(bw);
            }

            for (int i = 0; i < Fields.Count; i++)
            {
                bw.WriteShiftJIS(Fields[i].Name, true);
                bw.WriteShiftJIS(Fields[i].Format, true);
            }
        }

        /// <summary>
        /// Calculate the size of the PARAM this DBP goes to using it's fields.
        /// </summary>
        /// <returns>The size of the PARAM this DBP goes to.</returns>
        public int CalculateParamSize()
        {
            int size = 0;
            foreach (Field field in Fields)
            {
                switch (field.Type)
                {
                    case PARAMDEF.DefType.s8:
                    case PARAMDEF.DefType.u8:
                        size += 1;
                        break;
                    case PARAMDEF.DefType.s16:
                    case PARAMDEF.DefType.u16:
                        size += 2;
                        break;
                    case PARAMDEF.DefType.s32:
                    case PARAMDEF.DefType.u32:
                    case PARAMDEF.DefType.f32:
                        size += 4;
                        break;
                    default:
                        throw new NotImplementedException($"Display Type: {field.Type} invalid or not implemented.");
                }
            }
            return size;
        }

        /// <summary>
        /// Create a new DBPPARAM using only this DBP.
        /// </summary>
        /// <returns>A new DBPPARAM.</returns>
        public DBPPARAM CreateParam()
        {
            return new DBPPARAM(this);
        }

        /// <summary>
        /// A field in a PARAMDBP containing values for a field, displayName for a field, and a string formatter for a field.
        /// </summary>
        public class Field
        {
            private PARAMDEF.DefType _Type;

            /// <summary>
            /// Type of value to display in the editor.
            /// </summary>
            public PARAMDEF.DefType Type
            {   get => _Type;
                set
                {
                    if (!IsValidDisplayType(value))
                        throw new NotSupportedException($"{nameof(PARAMDEF.DefType)} {Type} is not supported by {nameof(PARAMDBP)}.");
                    else
                        _Type = value;
                }
            }

            /// <summary>
            /// The description for this field describing what it is in the DBP param.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The string formatter indicating how this field's values should be formatted.
            /// </summary>
            public string Format { get; set; }

            /// <summary>
            /// Default value for new fields.
            /// </summary>
            public object Default { get; set; }

            /// <summary>
            /// Amount of increase or decrease per step when scrolling in the editor.
            /// </summary>
            public object Increment { get; set; }

            /// <summary>
            /// Minimum valid value.
            /// </summary>
            public object Minimum { get; set; }

            /// <summary>
            /// Maximum valid value.
            /// </summary>
            public object Maximum { get; set; }

            /// <summary>
            /// Creates a new, empty Field.
            /// </summary>
            public Field()
            {
                _Type = PARAMDEF.DefType.s32;
                Name = string.Empty;
                Format = string.Empty;
                Default = 0;
                Increment = 1;
                Minimum = 0;
                Maximum = 100;
            }

            /// <summary>
            /// Create a new Field with default values using the specified options.
            /// </summary>
            /// <param name="type">The type the values in the field will be.</param>
            public Field(PARAMDEF.DefType type) : this(type, string.Empty, string.Empty) { }

            /// <summary>
            /// Create a new Field with default values using the specified options.
            /// </summary>
            /// <param name="type">The type the values in the field will be.</param>
            /// <param name="name">The name of the field.</param>
            public Field(PARAMDEF.DefType type, string name) : this(type, name, string.Empty) { }

            /// <summary>
            /// Create a new Field with default values using the specified options.
            /// </summary>
            /// <param name="type">The type the values in the field will be.</param>
            /// <param name="name">The display name of this field.</param>
            /// <param name="format">The display format of this field.</param>
            public Field(PARAMDEF.DefType type, string name, string format)
            {
                if (!IsValidDisplayType(type))
                    throw new NotSupportedException($"{nameof(PARAMDEF.DefType)} {type} is not supported by PARAMDBP.");

                Type = type;
                Name = name;
                Format = format;
                Default = ParamUtil.GetDefaultDefault(type);
                Increment = ParamUtil.GetDefaultIncrement(type);
                Minimum = ParamUtil.GetDefaultMinimum(type);
                Maximum = ParamUtil.GetDefaultMaximum(type);
            }

            /// <summary>
            /// Read a new field from a stream.
            /// </summary>
            internal Field(BinaryReaderEx br)
            {
                Type = (PARAMDEF.DefType)br.ReadUInt32();
                br.AssertInt32(0);
                br.AssertInt32(0);
                switch (Type)
                {
                    case PARAMDEF.DefType.s8:
                        Default = br.ReadSByte();
                        Increment = br.ReadSByte();
                        Minimum = br.ReadSByte();
                        Maximum = br.ReadSByte();
                        break;
                    case PARAMDEF.DefType.u8:
                        Default = br.ReadByte();
                        Increment = br.ReadByte();
                        Minimum = br.ReadByte();
                        Maximum = br.ReadByte();
                        break;
                    case PARAMDEF.DefType.s16:
                        Default = br.ReadInt16();
                        Increment = br.ReadInt16();
                        Minimum = br.ReadInt16();
                        Maximum = br.ReadInt16();
                        break;
                    case PARAMDEF.DefType.u16:
                        Default = br.ReadUInt16();
                        Increment = br.ReadUInt16();
                        Minimum = br.ReadUInt16();
                        Maximum = br.ReadUInt16();
                        break;
                    case PARAMDEF.DefType.s32:
                        Default = br.ReadInt32();
                        Increment = br.ReadInt32();
                        Minimum = br.ReadInt32();
                        Maximum = br.ReadInt32();
                        break;
                    case PARAMDEF.DefType.u32:
                        Default = br.ReadUInt32();
                        Increment = br.ReadUInt32();
                        Minimum = br.ReadUInt32();
                        Maximum = br.ReadUInt32();
                        break;
                    case PARAMDEF.DefType.f32:
                        Default = br.ReadSingle();
                        Increment = br.ReadSingle();
                        Minimum = br.ReadSingle();
                        Maximum = br.ReadSingle();
                        break;
                    default:
                        throw new NotImplementedException($"{nameof(PARAMDEF.DefType)}: {Type} invalid or not implemented.");
                }
            }

            /// <summary>
            /// Write this field to a stream.
            /// </summary>
            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32((int)Type);
                bw.WriteInt32(0);
                bw.WriteInt32(0);
                switch (Type)
                {
                    case PARAMDEF.DefType.s8:
                        bw.WriteSByte((sbyte)Default);
                        bw.WriteSByte((sbyte)Increment);
                        bw.WriteSByte((sbyte)Minimum);
                        bw.WriteSByte((sbyte)Maximum);
                        break;
                    case PARAMDEF.DefType.u8:
                        bw.WriteByte((byte)Default);
                        bw.WriteByte((byte)Increment);
                        bw.WriteByte((byte)Minimum);
                        bw.WriteByte((byte)Maximum);
                        break;
                    case PARAMDEF.DefType.s16:
                        bw.WriteInt16((short)Default);
                        bw.WriteInt16((short)Increment);
                        bw.WriteInt16((short)Minimum);
                        bw.WriteInt16((short)Maximum);
                        break;
                    case PARAMDEF.DefType.u16:
                        bw.WriteUInt16((ushort)Default);
                        bw.WriteUInt16((ushort)Increment);
                        bw.WriteUInt16((ushort)Minimum);
                        bw.WriteUInt16((ushort)Maximum);
                        break;
                    case PARAMDEF.DefType.s32:
                        bw.WriteInt32((int)Default);
                        bw.WriteInt32((int)Increment);
                        bw.WriteInt32((int)Minimum);
                        bw.WriteInt32((int)Maximum);
                        break;
                    case PARAMDEF.DefType.u32:
                        bw.WriteUInt32((uint)Default);
                        bw.WriteUInt32((uint)Increment);
                        bw.WriteUInt32((uint)Minimum);
                        bw.WriteUInt32((uint)Maximum);
                        break;
                    case PARAMDEF.DefType.f32:
                        bw.WriteSingle((float)Default);
                        bw.WriteSingle((float)Increment);
                        bw.WriteSingle((float)Minimum);
                        bw.WriteSingle((float)Maximum);
                        break;
                    default:
                        throw new NotImplementedException($"{nameof(PARAMDEF.DefType)}: {Type} invalid or not implemented.");
                }
            }

            /// <summary>
            /// Get a DisplayType using a string.
            /// </summary>
            /// <param name="str">A string representing a DisplayType.</param>
            /// <returns>A DisplayType.</returns>
            public static PARAMDEF.DefType GetDisplayType(string str)
            {
                switch (str.ToLower())
                {
                    case "s8": return PARAMDEF.DefType.s8;
                    case "u8": return PARAMDEF.DefType.u8;
                    case "s16": return PARAMDEF.DefType.s16;
                    case "u16": return PARAMDEF.DefType.u16;
                    case "s32": return PARAMDEF.DefType.s32;
                    case "u32": return PARAMDEF.DefType.u32;
                    case "f32": return PARAMDEF.DefType.f32;
                    default:
                        throw new NotImplementedException($"{nameof(PARAMDEF.DefType)}: {str} invalid or not implemented.");
                }
            }

            /// <summary>
            /// Convert an object value to the specified type using a FieldType.
            /// </summary>
            /// <param name="str">A string to convert to the specified FieldType.</param>
            /// <param name="type">A FieldType.</param>
            /// <returns>An object from the provided string converted to the specified type.</returns>
            public static object ConvertToDisplayType(string str, PARAMDEF.DefType type)
            {
                switch (type)
                {
                    case PARAMDEF.DefType.s8: return Convert.ToSByte(str);
                    case PARAMDEF.DefType.u8: return Convert.ToByte(str);
                    case PARAMDEF.DefType.s16: return Convert.ToInt16(str);
                    case PARAMDEF.DefType.u16: return Convert.ToUInt16(str);
                    case PARAMDEF.DefType.s32: return Convert.ToInt32(str);
                    case PARAMDEF.DefType.u32: return Convert.ToUInt32(str);
                    case PARAMDEF.DefType.f32: return Convert.ToSingle(str);
                    default:
                        throw new NotImplementedException($"{nameof(PARAMDEF.DefType)}: {type} invalid or not implemented.");
                }
            }

            /// <summary>
            /// Check if a DisplayType is valid for dbp.
            /// </summary>
            /// <param name="type">A DefType.</param>
            /// <returns>Whether or not the provided DefType is valid for dbp.</returns>
            public static bool IsValidDisplayType(PARAMDEF.DefType type)
            {
                switch (type)
                {
                    case PARAMDEF.DefType.s8:
                    case PARAMDEF.DefType.u8:
                    case PARAMDEF.DefType.s16:
                    case PARAMDEF.DefType.u16:
                    case PARAMDEF.DefType.s32:
                    case PARAMDEF.DefType.u32:
                    case PARAMDEF.DefType.f32:
                        return true;
                    default:
                        return false;
                }
            }

            /// <summary>
            /// Returns a string representation of this field.
            /// </summary>
            public override string ToString()
            {
                return $"Type: {Type}\n" +
                       $"Format: {Format}\n" +
                       $"Default: {Default}\n" +
                       $"Increment: {Increment}\n" +
                       $"Minimum: {Minimum}\n" +
                       $"Maximum:{Maximum}\n" +
                       $"Description: {Name}";
            }
        }
    }
}