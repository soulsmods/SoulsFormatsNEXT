using System;
using System.Collections.Generic;
using System.Numerics;

namespace SoulsFormats
{
    public partial class FLVER2
    {
        internal static string[] TextureScrollingSlots =
        {
            "GX20",
            "GX22",
            "GX24",
            "GX26",
            "GX27",
            "GX28",
            "GX30",
            "GX32",
            "GX34"
        };

        /// <summary>
        /// A GX item that can be stored in a <see cref="GXList"/>.
        /// </summary>
        public interface IGXItem
        {
            /// <summary>4-char ASCII item identifier, e.g. "GX00".</summary>
            string Name { get; }

            /// <summary>Format version or type ID for this item, typically 100. Interpretation depends on GX item type</summary>
            int Type { get; set; }

            void Read(BinaryReaderEx br, FLVERHeader header);

            void Write(BinaryWriterEx bw, FLVERHeader header);
        }

        /// <summary>
        /// A collection of items that set various material properties.
        /// </summary>
        public class GXList : List<IGXItem>
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

            internal GXList(BinaryReaderEx br, FLVERHeader header) : base()
            {
                if (header.Version < 0x20010)
                {
                    Add(new GXItem(br, header));
                }
                else
                {
                    int id;
                    while ((id = br.GetInt32(br.Position)) != int.MaxValue && id != -1)
                    {
                        var name = br.ReadFixStr(4);
                        var type = br.ReadInt32();
                        var length = br.ReadInt32();
                        var data = br.ReadBytes(length - 0xC);
                        var dataReader = new BinaryReaderEx(br.BigEndian, data);

                        IGXItem item = name switch
                        {
                            "GX00" => new GXSoftVolumeItem(),
                            "GX03" => new GXEmissiveIntensityItem(),
                            "GX14" => new GXAlbedoColorItem(),
                            "GX15" => new GXSubsurfaceScatteringItem(),
                            "GX16" => new GXPhantomLightItem(),
                            "GX17" => new GXSuperLODItem(),
                            "GX18" => new GXTextureScrolling(),
                            "GX20" => new GXTextureScrollingSpeed(0),
                            "GX22" => new GXTextureScrollingSpeed(1),
                            "GX24" => new GXTextureScrollingSpeed(2),
                            "GX26" => new GXTextureScrollingSpeed(3),
                            "GX27" => new GXTextureScrollingSpeed(4),
                            "GX28" => new GXTextureScrollingSpeed(5),
                            "GX30" => new GXTextureScrollingSpeed(6),
                            "GX32" => new GXTextureScrollingSpeed(7),
                            "GX34" => new GXTextureScrollingSpeed(8),
                            "GX50" => new GXTranslucencyScale(),
                            "GXMD" => new GXMaterialParametersItem(),
                            "GXUD" => new GXSpeedTreeItem(),
                            _ => new GXItem(name)
                        };

                        item.Type = type;
                        item.Read(dataReader, header);
                        if (dataReader.Remaining != 0)
                            Console.WriteLine(
                                $"GX item \"{name}\" (type {type}) left {dataReader.Remaining} byte(s) unread.");
                        Add(item);
                    }

                    TerminatorID = br.AssertInt32(id);
                    br.AssertInt32(100);
                    int lengthSubtraction = 0xC;
                    if (header.Unk68 == 0x5)
                    {
                        lengthSubtraction = 0;
                    }

                    TerminatorLength = br.ReadInt32() - lengthSubtraction;
                    br.AssertPattern(TerminatorLength, 0x00);
                }
            }

            internal void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                if (header.Version < 0x20010)
                {
                    var item = (GXItem)this[0];
                    if (!int.TryParse(item.Name, out int dsId))
                        throw new FormatException("For Dark Souls 2, GX IDs must be convertible to int.");
                    bw.WriteInt32(dsId);
                    bw.WriteInt32(item.Type);
                    bw.WriteInt32(item.Data.Length + 0xC);
                    item.Write(bw, header);
                }
                else
                {
                    int i = 0;
                    foreach (IGXItem item in this)
                    {
                        bw.WriteFixStr(item.Name, 4);
                        bw.WriteInt32(item.Type);
                        string sizeKey = $"gxItemSize{i++}";
                        bw.ReserveInt32(sizeKey);
                        long payloadStart = bw.Position;
                        item.Write(bw, header);
                        bw.FillInt32(sizeKey, (int)(bw.Position - payloadStart) + 0xC);
                    }

                    bw.WriteInt32(TerminatorID);
                    bw.WriteInt32(100);
                    bw.WriteInt32(TerminatorLength + 0xC);
                    bw.WritePattern(TerminatorLength, 0x00);
                }
            }
        }

        /// <summary>
        /// Fallback GX item used for unknown item types and the DS2 format.
        /// Stores the payload as raw bytes.
        /// </summary>
        public class GXItem : IGXItem
        {
            /// <summary>
            /// In DS2, the ID is an integer stringified; in other games it's 4 ASCII characters.
            /// </summary>
            public string Name { get; set; }

            public int Type { get; set; }

            /// <summary>Raw payload bytes.</summary>
            public byte[] Data { get; set; } = [];

            public GXItem()
            {
                Name = "0";
                Type = 100;
            }

            public GXItem(string name)
            {
                Name = name;
                Type = 100;
            }

            public GXItem(string name, int type, byte[] data)
            {
                Name = name;
                Type = type;
                Data = data;
            }

            /// <summary>Reads the full item header and payload; used only for the DS2 path.</summary>
            internal GXItem(BinaryReaderEx br, FLVERHeader header)
            {
                Name = header.Version <= 0x20010 ? br.ReadInt32().ToString() : br.ReadFixStr(4);
                Type = br.ReadInt32();
                Data = br.ReadBytes(br.ReadInt32() - 0xC);
            }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                Data = br.ReadBytes((int)br.Remaining);
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteBytes(Data);
            }
        }

        /// <summary>
        /// GX00: Soft volume / phantom feature flags.
        /// </summary>
        public class GXSoftVolumeItem : IGXItem
        {
            public string Name => "GX00";
            public int Type { get; set; }

            /// <summary>
            /// Something to do with rendering decals.
            /// </summary>
            public uint SoftVolumeFlags { get; set; }

            public uint UnkParam { get; set; }

            public uint UnkParam2 { get; set; }

            /// <summary>
            /// Maybe bitmask for low-detail textures?
            /// </summary>
            public int TextureMaskArg { get; set; }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                SoftVolumeFlags = br.ReadUInt32();
                UnkParam = br.ReadUInt32();
                if (Type >= 101) UnkParam2 = br.ReadUInt32();
                if (Type >= 102) TextureMaskArg = br.ReadInt32();
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteUInt32(SoftVolumeFlags);
                bw.WriteUInt32(UnkParam);
                if (Type >= 101) bw.WriteUInt32(UnkParam2);
                if (Type >= 102) bw.WriteInt32(TextureMaskArg);
            }
        }

        /// <summary>
        /// GX03: Emissive intensity multiplier.
        /// </summary>
        public class GXEmissiveIntensityItem : IGXItem
        {
            public string Name => "GX03";
            public int Type { get; set; }

            public float EmissiveIntensity { get; set; }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                EmissiveIntensity = br.ReadSingle();
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteSingle(EmissiveIntensity);
            }
        }

        /// <summary>
        /// GX14: Albedo color override.
        /// </summary>
        public class GXAlbedoColorItem : IGXItem
        {
            public string Name => "GX14";

            /// <summary>
            /// Only alpha is used when type == 100
            /// </summary>
            public int Type { get; set; }

            public Vector4 Color { get; set; }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                Color = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteSingle(Color.X);
                bw.WriteSingle(Color.Y);
                bw.WriteSingle(Color.Z);
                bw.WriteSingle(Color.W);
            }
        }

        /// <summary>
        /// GX15: Subsurface scattering width parameters (g_SSSWidth x/y/z).
        /// </summary>
        public class GXSubsurfaceScatteringItem : IGXItem
        {
            public string Name => "GX15";
            public int Type { get; set; }

            public Vector3 SSSParams { get; set; }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                SSSParams = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteSingle(SSSParams.X);
                bw.WriteSingle(SSSParams.Y);
                bw.WriteSingle(SSSParams.Z);
            }
        }

        /// <summary>
        /// GX16: Phantom (ghost/outline) effect parameters.
        /// </summary>
        public class GXPhantomLightItem : IGXItem
        {
            public string Name => "GX16";
            public int Type { get; set; }

            public bool EdgeSubtractBlend { get; set; }
            public bool FrontSubtractBlend { get; set; }
            public Vector4 EdgeColor { get; set; }
            public Vector4 FrontColor { get; set; }
            public Vector4 DiffuseColor { get; set; }
            public Vector4 SpecularColor { get; set; }
            public Vector4 LightColor { get; set; }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                EdgeSubtractBlend = br.ReadUInt32() != 0;
                FrontSubtractBlend = br.ReadUInt32() != 0;
                EdgeColor = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                FrontColor = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                DiffuseColor = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                SpecularColor = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                LightColor = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteUInt32(EdgeSubtractBlend ? 1u : 0u);
                bw.WriteUInt32(FrontSubtractBlend ? 1u : 0u);
                WriteVector4(bw, EdgeColor);
                WriteVector4(bw, FrontColor);
                WriteVector4(bw, DiffuseColor);
                WriteVector4(bw, SpecularColor);
                WriteVector4(bw, LightColor);
            }

            private static void WriteVector4(BinaryWriterEx bw, Vector4 v)
            {
                bw.WriteSingle(v.X);
                bw.WriteSingle(v.Y);
                bw.WriteSingle(v.Z);
                bw.WriteSingle(v.W);
            }
        }

        /// <summary>
        /// GX17: SuperLOD / billboard camera fade angles (stored in degrees).
        /// </summary>
        public class GXSuperLODItem : IGXItem
        {
            public string Name => "GX17";
            public int Type { get; set; }

            public float LookCameraFadeAngle { get; set; }

            public float Unk10 { get; set; }

            public float Unk14 { get; set; }

            public float CrossBillboardFadeAngle { get; set; }
            public uint Unk1C { get; set; }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                LookCameraFadeAngle = br.ReadSingle();
                Unk10 = br.ReadSingle();
                Unk14 = br.ReadSingle();
                CrossBillboardFadeAngle = br.ReadSingle();
                Unk1C = br.ReadUInt32();
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteSingle(LookCameraFadeAngle);
                bw.WriteSingle(Unk10);
                bw.WriteSingle(Unk14);
                bw.WriteSingle(CrossBillboardFadeAngle);
                bw.WriteUInt32(Unk1C);
            }
        }

        /// <summary>
        /// GX18: Texture scroll global enable and step interval.
        /// Per-slot UV speed vectors are defined by GX20–GX34.
        /// </summary>
        public class GXTextureScrolling : IGXItem
        {
            public string Name => "GX18";
            public int Type { get; set; }

            public float Step { get; set; }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                Step = br.ReadSingle();
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteSingle(Step);
            }
        }

        /// <summary>
        /// GX20/22/24/26/27/28/30/32/34: Per-slot UV scroll speed.
        /// </summary>
        public class GXTextureScrollingSpeed : IGXItem
        {
            public string Name => TextureScrollingSlots[Slot];
            public int Type { get; set; }

            public int Slot { get; }
            public Vector2 Speed { get; set; }

            public GXTextureScrollingSpeed(int slot)
            {
                Slot = slot;
            }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                Speed = new Vector2(br.ReadSingle(), br.ReadSingle());
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteSingle(Speed.X);
                bw.WriteSingle(Speed.Y);
            }
        }

        /// <summary>
        /// GX50: Translucency scale.
        /// </summary>
        public class GXTranslucencyScale : IGXItem
        {
            public string Name => "GX50";
            public int Type { get; set; }

            public float TranslucencyScale { get; set; }

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                TranslucencyScale = br.ReadSingle();
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                bw.WriteSingle(TranslucencyScale);
            }
        }

        public class GXMaterialParametersItem : IGXItem
        {
            public string Name => "GXMD";
            public int Type { get; set; }

            public List<GXMDParam> Params { get; } = new List<GXMDParam>();

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                if (Type == 100) return;

                uint paramCount = br.ReadUInt32();
                for (int i = 0; i < paramCount; i++)
                    Params.Add(new GXMDParam(br));
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                if (Type == 100) return;

                bw.WriteUInt32((uint)Params.Count);
                foreach (var p in Params)
                    p.Write(bw);
            }
        }

        /// <summary>
        /// A single parameter entry within a <see cref="GXMaterialParametersItem"/>.
        /// </summary>
        public class GXMDParam
        {
            public int ParamId { get; set; }
            public int Type { get; set; }
            public float[] Values { get; set; }

            internal GXMDParam(BinaryReaderEx br)
            {
                ParamId = br.ReadInt32();
                Type = br.ReadInt32();

                // TODO(gtierney): fix this with ref from https://github.com/JKAnderson/SoulsFormats/issues/14. Some float3 params are packed as float5 with x/y filled to 0.
                int valueCount = Type switch
                {
                    0 or 1 or 5 => 1,
                    2 or 6 => 2,
                    3 or 7 => 3,
                    4 or 8 => 4,
                    11 => 5,
                    _ => throw new NotSupportedException($"Unknown GXMD value type {Type}")
                };
                Values = new float[valueCount];
                for (int i = 0; i < valueCount; i++)
                    Values[i] = br.ReadSingle();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteSingle(ParamId);
                bw.WriteSingle(Type);
                foreach (float v in Values)
                    bw.WriteSingle(v);
            }
        }

        public enum GXUDFormatVersion
        {
            /// <summary>No tail sections.</summary>
            Base = 0,

            /// <summary>Adds <see cref="GXUDTreeParams.Unk59C"/></summary>
            V1 = 1,

            /// <summary>Adds per-geometry camera dither fades.</summary>
            ShaderDither = 2,

            /// <summary>Adds per-geometry translucency range min/max.</summary>
            Translucency = 3,

            /// <summary>Adds billboard backface specular.</summary>
            BillboardSpec = 4,
        }

        public class GXUDTreeParams
        {
            public GXUDFormatVersion FormatVersion { get; set; }

            // LOD distances
            public float LodNear { get; set; }
            public float LodUnk08 { get; set; }
            public float LodUnk0C { get; set; }
            public float LodUnk10 { get; set; }
            public float LodFar { get; set; }
            public uint Unk18 { get; set; }
            public uint Unk1C { get; set; }

            // Hue variation -> cbuf c1.z/w (u_fHueVariationByPos/ByVertex), c30.xyz (u_vHueVariationColor)
            public float HueVarMin { get; set; } /* +0x020 */
            public float HueVarMax { get; set; } /* +0x024 */
            public float HueVarUnk28 { get; set; } /* +0x028 */
            public float HueVarUnk2C { get; set; } /* +0x02C */
            public float HueVarUnk30 { get; set; } /* +0x030 */

            public float LodThreshold { get; set; } /* +0x034 */

            public uint BillboardAtlasSize { get; set; } /* +0x038 */

            public float[] AnimParams { get; set; } = new float[5];

            public byte[] CurveData { get; set; } = new byte[0x510];

            public float[] LodBlendKnots { get; set; } = new float[4];

            public bool[] LayerFlags { get; set; } = new bool[28];

            public float[] Extents { get; set; } = new float[4];

            // Version >= V1
            public float Unk59C { get; set; } /* +0x59C */

            // Version >= ShaderDither
            public float LeafDitherFade { get; set; } /* +0x5A0 -> cbuf c28.z */
            public float FrondDitherFade { get; set; } /* +0x5A4 -> cbuf c28.w */
            public float BranchDitherFade { get; set; } /* +0x5A8 -> cbuf c29.x */

            // Version >= Translucency
            public float LeafRangeMin { get; set; } /* +0x5AC -> cbuf c29.y */
            public float FrondRangeMin { get; set; } /* +0x5B0 -> cbuf c29.z */
            public float BranchRangeMin { get; set; } /* +0x5B4 -> cbuf c29.w */
            public float LeafRangeMax { get; set; } /* +0x5B8 -> cbuf c30.x */
            public float FrondRangeMax { get; set; } /* +0x5BC -> cbuf c30.y */
            public float BranchRangeMax { get; set; } /* +0x5C0 -> cbuf c30.z */

            // Version >= BillboardSpec
            public float BillboardBackSpecularWeaken { get; set; } /* +0x5C4 */

            internal void Read(BinaryReaderEx br)
            {
                FormatVersion = (GXUDFormatVersion)br.ReadInt32();
                LodNear = br.ReadSingle();
                LodUnk08 = br.ReadSingle();
                LodUnk0C = br.ReadSingle();
                LodUnk10 = br.ReadSingle();
                LodFar = br.ReadSingle();
                Unk18 = br.ReadUInt32();
                Unk1C = br.ReadUInt32();
                HueVarMin = br.ReadSingle();
                HueVarMax = br.ReadSingle();
                HueVarUnk28 = br.ReadSingle();
                HueVarUnk2C = br.ReadSingle();
                HueVarUnk30 = br.ReadSingle();
                LodThreshold = br.ReadSingle();
                BillboardAtlasSize = br.ReadUInt32();
                AnimParams = br.ReadSingles(5);
                CurveData = br.ReadBytes(0x510);
                LodBlendKnots = br.ReadSingles(4);
                LayerFlags = br.ReadBooleans(28);
                Extents = br.ReadSingles(4);

                if (FormatVersion >= GXUDFormatVersion.V1)
                    Unk59C = br.ReadSingle();

                if (FormatVersion >= GXUDFormatVersion.ShaderDither)
                {
                    LeafDitherFade = br.ReadSingle();
                    FrondDitherFade = br.ReadSingle();
                    BranchDitherFade = br.ReadSingle();
                }

                if (FormatVersion >= GXUDFormatVersion.Translucency)
                {
                    LeafRangeMin = br.ReadSingle();
                    FrondRangeMin = br.ReadSingle();
                    BranchRangeMin = br.ReadSingle();
                    LeafRangeMax = br.ReadSingle();
                    FrondRangeMax = br.ReadSingle();
                    BranchRangeMax = br.ReadSingle();
                }

                if (FormatVersion >= GXUDFormatVersion.BillboardSpec)
                    BillboardBackSpecularWeaken = br.ReadSingle();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteInt32((int)FormatVersion);
                bw.WriteSingle(LodNear);
                bw.WriteSingle(LodUnk08);
                bw.WriteSingle(LodUnk0C);
                bw.WriteSingle(LodUnk10);
                bw.WriteSingle(LodFar);
                bw.WriteUInt32(Unk18);
                bw.WriteUInt32(Unk1C);
                bw.WriteSingle(HueVarMin);
                bw.WriteSingle(HueVarMax);
                bw.WriteSingle(HueVarUnk28);
                bw.WriteSingle(HueVarUnk2C);
                bw.WriteSingle(HueVarUnk30);
                bw.WriteSingle(LodThreshold);
                bw.WriteUInt32(BillboardAtlasSize);
                bw.WriteSingles(AnimParams);
                bw.WriteBytes(CurveData);
                bw.WriteSingles(LodBlendKnots);
                bw.WriteBooleans(LayerFlags);
                bw.WriteSingles(Extents);

                if (FormatVersion >= GXUDFormatVersion.V1)
                    bw.WriteSingle(Unk59C);

                if (FormatVersion >= GXUDFormatVersion.ShaderDither)
                {
                    bw.WriteSingle(LeafDitherFade);
                    bw.WriteSingle(FrondDitherFade);
                    bw.WriteSingle(BranchDitherFade);
                }

                if (FormatVersion >= GXUDFormatVersion.Translucency)
                {
                    bw.WriteSingle(LeafRangeMin);
                    bw.WriteSingle(FrondRangeMin);
                    bw.WriteSingle(BranchRangeMin);
                    bw.WriteSingle(LeafRangeMax);
                    bw.WriteSingle(FrondRangeMax);
                    bw.WriteSingle(BranchRangeMax);
                }

                if (FormatVersion >= GXUDFormatVersion.BillboardSpec)
                    bw.WriteSingle(BillboardBackSpecularWeaken);
            }

            // TODO(gtierney): gross, just track read/write size
            internal int BlockSize() => (int)FormatVersion switch
            {
                0 => 0x59C,
                1 => 0x5A0,
                2 => 0x5AC,
                3 => 0x5C4,
                4 => 0x5C8,
                _ => throw new NotSupportedException($"Unknown GXUD format version {FormatVersion}")
            };
        }

        /// <summary>
        /// GXUD: SpeedTree data. Contains tree parameters and billboard atlas UV entries.
        /// </summary>
        public class GXSpeedTreeItem : IGXItem
        {
            public string Name => "GXUD";
            public int Type { get; set; }

            public GXUDTreeParams TreeParams { get; set; } = new GXUDTreeParams();

            public GXUDBillboardAtlasEntry[] Billboards { get; set; } = [];

            public void Read(BinaryReaderEx br, FLVERHeader header)
            {
                TreeParams = new GXUDTreeParams();
                TreeParams.Read(br);

                Billboards = new GXUDBillboardAtlasEntry[TreeParams.BillboardAtlasSize];
                for (int i = 0; i < TreeParams.BillboardAtlasSize; i++)
                    Billboards[i] = new GXUDBillboardAtlasEntry(br);

                br.AssertPattern(TreeParams.BlockSize(), 0);
            }

            public void Write(BinaryWriterEx bw, FLVERHeader header)
            {
                TreeParams.Write(bw);
                foreach (var pivot in Billboards)
                    pivot.Write(bw);
                bw.WritePattern(TreeParams.BlockSize(), 0x00);
            }
        }

        /// <summary>
        /// A UV rectangle within the SpeedTree billboard texture atlas.
        /// Negative width or height indicates a flipped axis.
        /// </summary>
        public struct GXUDBillboardAtlasEntry
        {
            public float U { get; set; }
            public float V { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }

            internal GXUDBillboardAtlasEntry(BinaryReaderEx br)
            {
                U = br.ReadSingle();
                V = br.ReadSingle();
                Width = br.ReadSingle();
                Height = br.ReadSingle();
            }

            internal void Write(BinaryWriterEx bw)
            {
                bw.WriteSingle(U);
                bw.WriteSingle(V);
                bw.WriteSingle(Width);
                bw.WriteSingle(Height);
            }
        }
    }
}