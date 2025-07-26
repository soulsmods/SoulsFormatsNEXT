using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulsFormats
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public partial class FLVER2
    {
        /// <summary>
        /// A collection of items that set various material properties.
        /// </summary>
        public class GXList : List<GXParam>
        {
            /// <summary>
            /// Value indicating the terminating item; typically int.MaxValue, sometimes -1.
            /// </summary>
            public int TerminatorID { get; set; }

            /// <summary>
            /// The length in bytes of the terminator data block; most likely not important, but varies in original files.
            /// </summary>
            public int TerminatorLength { get; set; }

            /// <summary>
            /// Creates an empty GXList.
            /// </summary>
            public GXList() : base()
            {
                TerminatorID = int.MaxValue;
            }

            internal GXList(BinaryReaderEx br, FLVER2.FLVERHeader header) : base()
            {
                if (header.Version < 0x20010)
                {
                    Add(new GXParam(br, header));
                }
                else
                {
                    int id;
                    while ((id = br.GetInt32(br.Position)) != int.MaxValue && id != -1)
                        Add(new GXParam(br, header));

                    TerminatorID = br.AssertInt32(id);
                    br.AssertInt32(100);
                    int lengthSubtraction = 0xC;
                    if(header.Unk68 == 0x5)
                    {
                        lengthSubtraction = 0;
                    }
                    TerminatorLength = br.ReadInt32() - lengthSubtraction;
                    br.AssertPattern(TerminatorLength, 0x00);
                }
            }

            internal void Write(BinaryWriterEx bw, FLVER2.FLVERHeader header)
            {
                if (header.Version < 0x20010)
                {
                    this[0].Write(bw, header);
                }
                else
                {
                    foreach (GXParam item in this)
                        item.Write(bw, header);

                    bw.WriteInt32(TerminatorID);
                    bw.WriteInt32(100);
                    bw.WriteInt32(TerminatorLength + 0xC);
                    bw.WritePattern(TerminatorLength, 0x00);
                }
            }

            public void ApplyGXListDef(GXListDef gxListDef)
            {
                foreach (GXParam gxParam in this)
                {
                    GXListDef.GXParamDef gxParamDef = gxListDef.FirstOrDefault(x => 
                        x.ID.Equals(gxParam.ID) && x.Unk04 == gxParam.Unk04);
                    if (gxParamDef == null) continue;

                    List<GXValue> values = new List<GXValue>();
                    
                    BinaryReaderEx br = new BinaryReaderEx(false, gxParam.Data);
                    foreach (GXListDef.ValueDef valueDef in gxParamDef.Items)
                    {
                        object val = null;
                        switch (valueDef.Type)
                        {
                            case GXListDef.ValueType.Unknown:
                            case GXListDef.ValueType.Int:
                            case GXListDef.ValueType.Enum:
                                val = br.ReadInt32();
                                break;
                            case GXListDef.ValueType.Float:
                                val = br.ReadSingle();
                                break;
                            case GXListDef.ValueType.Bool:
                                val = br.ReadInt32() != 0;
                                break;
                        }
                        GXValue gxValue = new GXValue(val, valueDef);
                        values.Add(gxValue);
                        
                    }
                    
                    gxParam.Values = values;
                }
            }
        }

        /// <summary>
        /// Rendering parameters used by materials.
        /// </summary>
        public class GXParam
        {
            /// <summary>
            /// In DS2, ID is just a number; in other games, it's 4 ASCII characters.
            /// </summary>
            public string ID { get; set; }

            /// <summary>
            /// Unknown; typically 100.
            /// </summary>
            public int Unk04 { get; set; }

            /// <summary>
            /// Raw parameter data, either float int values
            /// </summary>
            public byte[] Data { get; set; }

            /// <summary>
            /// Named and typed values from the byte data. Non-null if an EXParamDef was applied
            /// </summary>
            public IReadOnlyList<GXValue> Values { get; internal set; } = null;

            /// <summary>
            /// Creates a GXItem with default values.
            /// </summary>
            public GXParam()
            {
                ID = "0";
                Unk04 = 100;
                Data = new byte[0];
            }

            /// <summary>
            /// Creates a GXItem with the given values.
            /// </summary>
            public GXParam(string id, int unk04, byte[] data)
            {
                ID = id;
                Unk04 = unk04;
                Data = data;
            }

            internal GXParam(BinaryReaderEx br, FLVER2.FLVERHeader header)
            {
                if (header.Version <= 0x20010)
                {
                    ID = br.ReadInt32().ToString();
                }
                else
                {
                    ID = br.ReadFixStr(4);
                }
                Unk04 = br.ReadInt32();
                int length = br.ReadInt32();
                Data = br.ReadBytes(length - 0xC);
            }

            internal void Write(BinaryWriterEx bw, FLVER2.FLVERHeader header)
            {
                if (header.Version <= 0x20010)
                {
                    if (int.TryParse(ID, out int id))
                        bw.WriteInt32(id);
                    else
                        throw new FormatException("For Dark Souls 2, GX IDs must be convertible to int.");
                }
                else
                {
                    bw.WriteFixStr(ID, 4);
                }
                bw.WriteInt32(Unk04);
                bw.WriteInt32(Data.Length + 0xC);
                bw.WriteBytes(Data);
            }
        }
        
        public class GXValue
        {
            public GXListDef.ValueDef ValueDef { get; }
            public object Value { get; set; }
            public object Min => ValueDef.Min;
            public object Max => ValueDef.Max;
            public string Name => ValueDef.Name;
            public Dictionary<int, string> Enum => ValueDef.Enum;
            
            public GXValue(object value, GXListDef.ValueDef valueDef = null)
            {
                Value = value;
                ValueDef = valueDef;
            }
        }
    }
}
