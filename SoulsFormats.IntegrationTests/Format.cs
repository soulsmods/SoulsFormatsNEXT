namespace SoulsFormats.IntegrationTests;

public sealed record Format(string Name, Func<byte[], object> Read)
{
    public static Format Of<TFormat>() where TFormat : SoulsFile<TFormat>, new() =>
        new(typeof(TFormat).Name, bytes => SoulsFile<TFormat>.Read(bytes));
    
    public static readonly Format Binder = new("BND", bytes => BND3.Is(bytes) ? BND3.Read(bytes) : BND4.Read(bytes));
    public static readonly Dictionary<string, Format> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acb"] = Of<ACB>(),
        ["aip"] = Of<AIP>(),
        ["btab"] = Of<BTAB>(),
        ["btl"] = Of<BTL>(),
        ["btpb"] = Of<BTPB>(),
        ["clm2"] = Of<CLM2>(),
        ["emeld"] = Of<EMELD>(),
        ["emevd"] = Of<EMEVD>(),
        ["entryfilelist"] = Of<ENFL>(),
        ["esd"] = Of<ESD>(),
        ["evd"] = Of<EMEVD>(),
        ["expb"] = Of<FMB>(),
        ["flv"] = Of<FLVER2>(),
        ["flver"] = Of<FLVER2>(),
        ["flver2tri"] = Of<F2TR>(),
        ["fltparam"] = Of<GPARAM>(),
        ["fmg"] = Of<FMG>(),
        ["gparam"] = Of<GPARAM>(),
        ["grass"] = Of<GRASS>(),
        ["luagnl"] = Of<LUAGNL>(),
        ["luainfo"] = Of<LUAINFO>(),
        ["matbin"] = Of<MATBIN>(),
        ["mcg"] = Of<MCG>(),
        ["mcp"] = Of<MCP>(),
        ["metaparam"] = Of<METAPARAM>(),
        ["mqb"] = Of<MQB>(),
        ["mtd"] = Of<MTD>(),
        ["ngp"] = Of<NGP>(),
        ["nva"] = Of<NVA>(),
        ["nvm"] = Of<NVM>(),
        ["param"] = Of<PARAM>(),
        ["paramdef"] = Of<PARAMDEF>(),
        ["pmdcl"] = Of<PMDCL>(),
        ["rmb"] = Of<RMB>(),
        ["tae"] = Of<TAE>(),
        ["tpf"] = Of<TPF>(),
    };

    public static string ExtensionOf(string path)
    {
        string name = path[(path.LastIndexOfAny(['/', '\\']) + 1)..];
        foreach (string suffix in (string[])[".dcx", ".devpatch"])
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                name = name[..^suffix.Length];
        }

        int dot = name.LastIndexOf('.');
        return dot < 0 ? "" : name[(dot + 1)..];
    }

    public override string ToString() => Name;
}
