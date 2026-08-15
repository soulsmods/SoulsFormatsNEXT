using System.Collections.Generic;

namespace SoulsFormats;

/// <summary>
/// A shader material parameter definition format used in ELDEN RING, Armored Core VI and related titles.
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

    /// <summary>Unknown. Always 0x0401 across all 1063 ELDEN RING metaparams.</summary>
    public int Unk30 { get; set; }

    /// <summary>
    /// Feature flags
    /// </summary>
    public int Flags { get; set; }

    /// <summary>
    /// Whether the shader declares <see cref="CaParams"/>
    /// </summary>
    public bool DeclaresCaParams => (Flags & 0x10) != 0;

    /// <summary>
    /// Whether the vertex stage reads the shader's own parameters
    /// </summary>
    public bool VertexReadsMtdParams => (Flags & 0x100) != 0;

    /// <summary>
    /// Whether the shader is tessellated
    /// </summary>
    public bool IsTessellated => (Flags & 0x10000) != 0;

    /// <summary>Unknown. Always 2 in ELDEN RING.</summary>
    public int Unk40 { get; set; }

    /// <summary>
    /// Key used to match against GXItem keys.
    /// </summary>
    public int GxmdKey { get; set; }

    /// <summary>
    /// Size in bytes of the CaParam block.
    /// </summary>
    public int CaParamBlockSize { get; set; }

    /// <summary>Unknown.</summary>
    public int Unk50 { get; set; }

    /// <summary>
    /// Number of parameters in CaParam. Counts parameters themselves, not components
    /// </summary>
    public int CaParamCount { get; set; }

    /// <summary>
    /// Texture sampler slot definitions.
    /// </summary>
    public IList<Texture> Textures { get; set; }

    /// <summary>
    /// Render pass sampler binding configuration.
    /// </summary>
    public RenderPassConfig RenderPasses { get; set; }

    /// <summary>
    /// Parameters read by shaders via <c>SAT_CAParam</c>.
    /// </summary>
    public IList<CaParam> CaParams { get; set; }

    /// <summary>
    /// Parameters read by shaders via <c>SAT_MtdParam</c>.
    /// </summary>
    public IList<MtdParam> MtdParams { get; set; }

    /// <summary>
    /// Creates an empty METAPARAM.
    /// </summary>
    public METAPARAM()
    {
        Textures = new List<Texture>();
        RenderPasses = new RenderPassConfig();
        CaParams = new List<CaParam>();
        MtdParams = new List<MtdParam>();
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

        // Always equal to caParamsOffset?
        var unkOffset = br.ReadInt64();
        var caParamsOffset = br.ReadInt64();
        var mtdParamsOffset = br.ReadInt64();

        var caParamCount = br.ReadInt32();
        var mtdParamCount = br.ReadInt32();
        Unk30 = br.ReadInt32();
        CBufferSize = br.ReadInt32();
        Flags = br.ReadInt32();
        br.AssertInt32(0); // unk3c
        Unk40 = br.ReadInt32();
        br.AssertInt32(0); // unk44
        GxmdKey = br.ReadInt32();
        CaParamBlockSize = br.ReadInt32();
        Unk50 = br.ReadInt32();
        CaParamCount = br.ReadInt32();
        ShaderId = br.ReadInt32();
        br.AssertPattern(0x3C, 0x00); // unk5c through unk94

        Textures = new List<Texture>(textureCount);
        for (int i = 0; i < textureCount; i++)
            Textures.Add(new Texture(br));

        RenderPasses = new RenderPassConfig(br);

        br.StepIn(caParamsOffset);
        CaParams = new List<CaParam>(caParamCount);
        for (int i = 0; i < caParamCount; i++)
            CaParams.Add(new CaParam(br));
        br.StepOut();

        br.StepIn(mtdParamsOffset);
        MtdParams = new List<MtdParam>(mtdParamCount);
        for (int i = 0; i < mtdParamCount; i++)
            MtdParams.Add(new MtdParam(br));
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

        /// <summary>Reflectance map.</summary>
        Reflectance = 0x03,

        /// <summary>Normal map.</summary>
        Normal = 0x04,

        /// <summary>Emissive map.</summary>
        Emissive = 0x06,

        /// <summary>Displacement map.</summary>
        Displacement = 0x07,

        /// <summary>Flow map, for scrolling or advected UVs.</summary>
        Flow = 0x0A,

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

        /// <summary>Three floats.</summary>
        Float3 = 0x03,

        /// <summary>Four floats.</summary>
        Float4 = 0x04,

        /// <summary>Five floats (color RGBA + intensity).</summary>
        Color = 0x0D,
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

        /// <summary>0 indexed texture slot</summary>
        public byte Slot { get; set; }

        /// <summary>Texture semantic category.</summary>
        public TextureType Type { get; set; }

        public int Unk0C { get; set; }

        /// <summary>
        /// Creates a default TextureSlot.
        /// </summary>
        public Texture()
        {
            Name = "";
            DefaultTexturePath = "";
            UvGroupName = "";
            Unk0C = -1;
        }

        internal Texture(BinaryReaderEx br)
        {
            var nameOffset = br.ReadInt64();
            br.ReadByte();
            Slot = br.ReadByte();
            br.AssertByte(0x00);
            Type = (TextureType)br.ReadByte();
            Unk0C = br.ReadInt32();
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
        /// <summary>
        /// Identifier for this pass. Unknown what each corresponds to.
        /// </summary>
        public int PassId { get; set; }

        /// <summary>
        /// Bitmask of samplers bound by pixel shaders.
        /// </summary>
        public int SamplerMask { get; set; }

        /// <summary>
        /// Bitmask of samplers bound by hull shaders.
        /// </summary>
        public int TessellationSamplerMask { get; set; }

        /// <summary>
        /// Bitmask of samplers bound by vertex shaders.
        /// </summary>
        public int VertexSamplerMask { get; set; }

        /// <summary>
        /// Creates a default RenderPassEntry.
        /// </summary>
        public RenderPassEntry()
        {
        }

        internal RenderPassEntry(BinaryReaderEx br)
        {
            PassId = br.ReadInt32();
            br.AssertInt32(0); // unk04
            SamplerMask = br.ReadInt32();
            TessellationSamplerMask = br.ReadInt32();
            VertexSamplerMask = br.ReadInt32();
            br.AssertInt32(0); // unk14
        }
    }

    /// <summary>
    /// Configuration of the sampler bound for each render pass.
    /// </summary>
    public class RenderPassConfig
    {
        /// <summary>
        /// Per-pass sampler binding entries.
        /// </summary>
        public IList<RenderPassEntry> Entries { get; set; }

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
            br.AssertInt32(0);
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

    public class CaParam
    {
        /// <summary>Parameter name (e.g. "_color_0_0").</summary>
        public string Name { get; set; }

        public int SequenceIndex { get; set; }

        /// <summary>
        /// Identifies the parameter this component belongs to.
        public int ParamId { get; set; }

        /// <summary>
        /// Channel within the parameter.
        /// </summary>
        public int ComponentIndex { get; set; }

        /// <summary>Default value for this component.</summary>
        public float DefaultValue { get; set; }

        public int Unk40 { get; set; }

        /// <summary>
        /// A name hash
        /// </summary>
        public int ParamKey { get; set; }

        /// <summary>
        /// Creates a default CaParam.
        /// </summary>
        public CaParam()
        {
            Name = "";
        }

        internal CaParam(BinaryReaderEx br)
        {
            long nameOffset = br.ReadInt64();
            br.AssertPattern(0x1C, 0x00);
            br.AssertInt32(0);
            SequenceIndex = br.ReadInt32();
            ParamId = br.ReadInt32();
            ComponentIndex = br.ReadInt32();
            DefaultValue = br.ReadSingle();
            Unk40 = br.ReadInt32();
            ParamKey = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertPattern(0x18, 0x00);

            Name = br.GetUTF16(nameOffset);
        }
    }

    /// <summary>
    /// A parameter declared as a whole value, storing its default inline as up to five floats.
    /// </summary>
    public class MtdParam
    {
        /// <summary>Parameter name (e.g. "_color_0", "group_0_CommonUV-UVParam").</summary>
        public string Name { get; set; }

        /// <summary>
        /// Offset of the first component in this parameter inside the constant buffer as 32 bit components
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// Sequential index in this shader's parameter list.
        /// </summary>
        public int ParamSeqIndex { get; set; }

        /// <summary>Value type of this parameter.</summary>
        public byte ValueType { get; set; }

        /// <summary>
        /// Is this param actually passed to a shader or just referenced by renderer code?
        /// </summary>
        public bool IsNotShaderInput { get; set; }

        /// <summary>
        /// If this param encodes color, is it in sRGB space?
        /// </summary>
        public bool IsSrgb { get; set; }

        /// <summary>
        /// CRC32 of the UTF-16-LE bytes of an original parameter name, such as "[Albedo]_1_[Tint]"
        /// </summary>
        public uint ParamKey { get; set; }

        /// <summary>
        /// Default value components.
        /// </summary>
        public float[] DefaultValue { get; set; }

        /// <summary>
        /// Creates a default MtdParam.
        /// </summary>
        public MtdParam()
        {
            Name = "";
            DefaultValue = new float[5];
            ParamSeqIndex = -1;
        }

        internal MtdParam(BinaryReaderEx br)
        {
            long nameOffset = br.ReadInt64();
            br.AssertPattern(0x1C, 0x00);
            StartOffset = br.ReadInt32();
            ParamSeqIndex = br.ReadInt32();
            ValueType = br.ReadByte();
            IsNotShaderInput = br.AssertByte(0, 2) == 2;
            IsSrgb = br.AssertByte(0, 1) == 1;
            br.AssertByte((byte)(ValueType == (byte)ParamValueType.Color ? 1 : 0));
            ParamKey = br.ReadUInt32();
            DefaultValue = br.ReadSingles(5);
            br.AssertPattern(0x08, 0x00);

            Name = br.GetUTF16(nameOffset);
        }
    }
}
