using System;
using System.Collections.Generic;
using System.IO;

namespace SoulsFormats;

/// <summary>
/// A shader material parameter definition format used in ELDENM RING, Armored Core VI and related titles.
/// Declares texture samplers, constant buffer parameters, and render pass bindings. Extension: .metaparam
/// </summary>
public class METAPARAM : SoulsFile<METAPARAM>
{
    /// <summary>
    /// Constant buffer size in bytes.
    /// </summary>
    public int CBufferSize { get; set; }

    /// <summary>
    /// Shader program hash/ID.
    /// </summary>
    public int ShaderId { get; set; }

    /// <summary>Unknown.</summary>
    public int Unk30 { get; set; }

    /// <summary>Unknown.</summary>
    public int Unk38 { get; set; }

    /// <summary>Unknown.</summary>
    public int Unk40 { get; set; }

    /// <summary>Unknown.</summary>
    public int Unk48 { get; set; }

    /// <summary>Unknown.</summary>
    public int Unk4C { get; set; }

    /// <summary>Unknown.</summary>
    public int Unk50 { get; set; }

    /// <summary>Unknown.</summary>
    public int Unk54 { get; set; }

    /// <summary>
    /// Texture sampler slot definitions.
    /// </summary>
    public IList<Texture> Textures { get; set; }

    /// <summary>
    /// Render pass sampler binding configuration.
    /// </summary>
    public RenderPassConfig RenderPasses { get; set; }

    /// <summary>
    /// Per-component scalar parameter definitions (used for color channel decomposition).
    /// </summary>
    public IList<ScalarParam> ScalarParams { get; set; }

    /// <summary>
    /// Vector parameter definitions (bools, floats, UV tiling, colors).
    /// </summary>
    public List<VectorParam> VectorParams { get; set; }

    /// <summary>
    /// Creates an empty METAPARAM.
    /// </summary>
    public METAPARAM()
    {
        Textures = new List<Texture>();
        RenderPasses = new RenderPassConfig();
        ScalarParams = new List<ScalarParam>();
        VectorParams = new List<VectorParam>();
    }

    /// <summary>
    /// Checks whether the data appears to be a file of this format.
    /// </summary>
    protected override bool Is(BinaryReaderEx br)
        => br.Length >= 4L && br.GetASCII(0L, 4) == "SMD\0";

    /// <summary>
    /// Deserializes file data from a stream.
    /// </summary>
    protected override void Read(BinaryReaderEx br)
    {
        br.BigEndian = false;

        br.AssertASCII("SMD\0");
        br.AssertInt32(0); // unk04
        br.AssertInt32(6); // version
        var textureCount = br.ReadInt32();

        // Always equal to scalarParamsOffset?
        var unkOffset = br.ReadInt64();
        var scalarParamsOffset = br.ReadInt64();
        var vectorParamsOffset = br.ReadInt64();

        var scalarParamCount = br.ReadInt32();
        var vectorParamCount = br.ReadInt32();
        Unk30 = br.ReadInt32();
        CBufferSize = br.ReadInt32();
        Unk38 = br.ReadInt32();
        br.AssertInt32(0); // unk3c
        Unk40 = br.ReadInt32();
        br.AssertInt32(0); // unk44
        Unk48 = br.ReadInt32();
        Unk4C = br.ReadInt32();
        Unk50 = br.ReadInt32();
        Unk54 = br.ReadInt32();
        ShaderId = br.ReadInt32();
        br.AssertPattern(0x3C, 0x00); // unk5c through unk94

        Textures = new List<Texture>(textureCount);
        for (int i = 0; i < textureCount; i++)
            Textures.Add(new Texture(br));

        RenderPasses = new RenderPassConfig(br);

        br.StepIn(scalarParamsOffset);
        ScalarParams = new List<ScalarParam>(scalarParamCount);
        for (int i = 0; i < scalarParamCount; i++)
            ScalarParams.Add(new ScalarParam(br));
        br.StepOut();

        br.StepIn(vectorParamsOffset);
        VectorParams = new List<VectorParam>(vectorParamCount);
        for (int i = 0; i < vectorParamCount; i++)
            VectorParams.Add(new VectorParam(br));
        br.StepOut();
    }


    /// <summary>
    /// Texture usage type
    /// </summary>
    public enum TextureType : byte
    {
        /// <summary>Unclassified / other texture.</summary>
        Other = 0x00,

        /// <summary>Albedo / diffuse map.</summary>
        Albedo = 0x01,

        /// <summary>Normal map.</summary>
        Normal = 0x04,

        /// <summary>Emissive map.</summary>
        Emissive = 0x06,

        /// <summary>Metallic map.</summary>
        Metallic = 0x0E,

        /// <summary>1 channel mask.</summary>
        Mask1 = 0x0F,

        /// <summary>3 channel mask.</summary>
        Mask3 = 0x10,

        /// <summary>Vector / vertex-animation map.</summary>
        Vector = 0x11,
    }

    /// <summary>
    /// Data type of vector parameter values.
    /// </summary>
    public enum ParamValueType : byte
    {
        /// <summary>Boolean (1 byte).</summary>
        Bool = 0x00,

        /// <summary>Single float.</summary>
        Float = 0x01,

        /// <summary>Two floats (UV tiling: scaleU, scaleV, offsetU, offsetV).</summary>
        Float2 = 0x02,

        /// <summary>Five floats (color RGBA + intensity).</summary>
        Float5 = 0x0D,
    }

    /// <summary>
    /// A texture slot declaration. Declares the texture name, default texture,
    /// UV group, slot index, and type for one texture bound by this shader.
    /// </summary>
    public class Texture
    {
        /// <summary>Name of the texture.</summary>
        public string Name { get; set; }

        /// <summary>Default texture path. Empty string if none.</summary>
        public string DefaultTexturePath { get; set; }

        /// <summary>UV group / tiling parameter group name.</summary>
        public string UvGroupName { get; set; }

        /// <summary>0 indexed texture slot (shader register?).</summary>
        public byte Slot { get; set; }

        /// <summary>Texture semantic category.</summary>
        public TextureType Type { get; set; }

        /// <summary>
        /// Unknown. Seems to be a bitfield, sometimes -1.
        /// </summary>
        public int MaybeBitmask { get; set; }

        /// <summary>
        /// Creates a default TextureSlot.
        /// </summary>
        public Texture()
        {
            Name = "";
            DefaultTexturePath = "";
            UvGroupName = "";
            MaybeBitmask = -1;
        }

        internal Texture(BinaryReaderEx br)
        {
            var nameOffset = br.ReadInt64();
            br.ReadByte();
            Slot = br.ReadByte();
            br.AssertByte(0x00);
            Type = br.ReadEnum8<TextureType>();
            MaybeBitmask = br.ReadInt32();
            var defaultTexOffset = br.ReadInt64();
            var uvGroupOffset = br.ReadInt64();
            br.Skip(0x10);

            Name = br.GetUTF16(nameOffset);
            DefaultTexturePath = br.GetUTF16(defaultTexOffset);
            UvGroupName = br.GetUTF16(uvGroupOffset);
        }
    }

    /// <summary>
    /// Declares which texture slots are bound for a render pass.
    /// </summary>
    public class RenderPassEntry
    {
        /// <summary>The render pass this entry applies to.</summary>
        public int MaybePassType { get; set; }

        /// <summary>
        /// Texture bitfield: bit N set means slot N is bound for this pass.
        /// </summary>
        public int SamplerMask { get; set; }

        /// <summary>Unknown.</summary>
        public int Unk0C { get; set; }

        /// <summary>
        /// Something related to vertex animation texttures? 0 for non-VA.
        /// </summary>
        public int VaExtraVertexSize { get; set; }

        /// <summary>
        /// Creates a default RenderPassEntry.
        /// </summary>
        public RenderPassEntry()
        {
        }

        internal RenderPassEntry(BinaryReaderEx br)
        {
            MaybePassType = br.ReadInt32();
            br.AssertInt32(0); // unk04
            SamplerMask = br.ReadInt32();
            Unk0C = br.ReadInt32();
            VaExtraVertexSize = br.ReadInt32();
            br.AssertInt32(0); // unk14
        }
    }

    /// <summary>
    /// Probably something related to configuration per render pass?
    /// </summary>
    public class RenderPassConfig
    {
        /// <summary>
        /// Per-pass sampler binding entries.
        /// </summary>
        public IList<RenderPassEntry> Entries { get; set; }

        /// <summary>Unknown.</summary>
        public int Unk0C { get; set; }

        /// <summary>
        /// Creates an empty RenderPassConfig.
        /// </summary>
        public RenderPassConfig()
        {
            Entries = new List<RenderPassEntry>();
        }

        internal RenderPassConfig(BinaryReaderEx br)
        {
            var entriesOffset = br.ReadInt64();
            var entryCount = br.ReadInt32();
            Unk0C = br.ReadInt32();
            br.AssertPattern(0x18, 0x00);

            Entries = new List<RenderPassEntry>(entryCount);
            if (entryCount > 0)
            {
                br.StepIn(entriesOffset);
                for (var i = 0; i < entryCount; i++)
                    Entries.Add(new RenderPassEntry(br));
                br.StepOut();
            }
        }
    }


    /// <summary>
    /// Per-component scalar parameter definition. Color parameters are split across multiple
    /// entries, one per channel.
    /// </summary>
    public class ScalarParam
    {
        /// <summary>Parameter name (e.g. "_color_0_0").</summary>
        public string Name { get; set; }

        /// <summary>Unknown.</summary>
        public int Unk24 { get; set; }

        /// <summary>Index of the parameter?</summary>
        public int Index { get; set; }

        /// <summary>Parameter type/name hash.</summary>
        public int ParamId { get; set; }

        /// <summary>
        /// Channel within the parameter (0-4 for colors, 0 for scalar floats).
        /// </summary>
        public int ComponentIndex { get; set; }

        /// <summary>Default value for this component.</summary>
        public float DefaultValue { get; set; }

        /// <summary>
        /// Component count; 1 for float, 267 (0x10B) for color components.
        /// </summary>
        public int ComponentCount { get; set; }

        /// <summary>Unknown.</summary>
        public int Unk3C { get; set; }

        /// <summary>Unknown.</summary>
        public int Unk40 { get; set; }

        /// <summary>Unknown.</summary>
        public int Unk44 { get; set; }

        /// <summary>
        /// Creates a default ScalarParam.
        /// </summary>
        public ScalarParam()
        {
            Name = "";
        }

        internal ScalarParam(BinaryReaderEx br)
        {
            long nameOffset = br.ReadInt64();
            br.AssertPattern(0x1C, 0x00);
            Unk24 = br.ReadInt32();
            Index = br.ReadInt32();
            ParamId = br.ReadInt32();
            ComponentIndex = br.ReadInt32();
            DefaultValue = br.ReadSingle();
            ComponentCount = br.ReadInt32();
            Unk3C = br.ReadInt32();
            Unk40 = br.ReadInt32();
            Unk44 = br.ReadInt32();
            br.AssertPattern(0x18, 0x00);

            Name = br.GetUTF16(nameOffset);
        }
    }

    /// <summary>
    /// Vector parameter definition. Stores the full default value inline as up to five floats.
    /// UV tiling parameters have CbufDwordOffset == 0 and ParamSeqIndex == -1.
    /// </summary>
    public class VectorParam
    {
        /// <summary>Parameter name (e.g. "_color_0", "group_0_CommonUV-UVParam").</summary>
        public string Name { get; set; }

        /// <summary>
        /// Offset of the first component in this parameter inside the constant buffer as 32 bit components
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// Sequential index in this shader's parameter list. -1 for UV tiling params.
        /// </summary>
        public int ParamSeqIndex { get; set; }

        /// <summary>Value type of this parameter.</summary>
        public byte ValueType { get; set; }

        /// <summary>
        /// Additional type flags, unknown
        /// </summary>
        public byte[] TypeFlags { get; set; }

        /// <summary>
        /// CRC32 of the UTF-16-LE bytes of an original parameter name, such as "[Albedo]_1_[Tint]"
        /// </summary>
        public uint ParamKey { get; set; }

        /// <summary>
        /// Default value components.
        /// </summary>
        public float[] DefaultValue { get; set; }

        /// <summary>
        /// Creates a default VectorParam.
        /// </summary>
        public VectorParam()
        {
            Name = "";
            TypeFlags = new byte[3];
            DefaultValue = new float[5];
            ParamSeqIndex = -1;
        }

        internal VectorParam(BinaryReaderEx br)
        {
            long nameOffset = br.ReadInt64();
            br.AssertPattern(0x1C, 0x00);
            StartOffset = br.ReadInt32();
            ParamSeqIndex = br.ReadInt32();
            ValueType = br.ReadByte();
            TypeFlags = br.ReadBytes(3);
            ParamKey = br.ReadUInt32();
            DefaultValue = br.ReadSingles(5);
            br.AssertPattern(0x08, 0x00);

            Name = br.GetUTF16(nameOffset);
        }
    }
}