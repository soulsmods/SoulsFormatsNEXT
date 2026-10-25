namespace SoulsFormats.IntegrationTests;

public sealed record Game(string Name, int AppId, Dictionary<string, Format> Formats)
{
    public static readonly Game DarkSouls = new("DARK SOULS: Prepare to Die Edition", 211420, new()
    {
        ["msb"] = Format.Of<MSB1>(),
    });

    public static readonly Game DarkSouls2 = new("DARK SOULS II: Scholar of the First Sin", 335300, new()
    {
        ["msb"] = Format.Of<MSB2>(),
        ["ffx"] = Format.Of<FFXDLSE>(),
    });

    public static readonly Game DarkSouls3 = new("DARK SOULS III", 374320, new()
    {
        ["msb"] = Format.Of<MSB3>(),
        ["fxr"] = Format.Of<FXR3>(),
    });

    public static readonly Game Sekiro = new("Sekiro: Shadows Die Twice", 814380, new()
    {
        ["msb"] = Format.Of<MSBS>(),
        ["fxr"] = Format.Of<FXR3>(),
    });

    public static readonly Game EldenRing = new("ELDEN RING", 1245620, new()
    {
        ["msb"] = Format.Of<MSBE>(),
        ["fxr"] = Format.Of<FXR3>(),
    });

    public static readonly Game Nightreign = new("ELDEN RING NIGHTREIGN", 2622380, new()
    {
        ["msb"] = Format.Of<MSBE>(),
        ["fxr"] = Format.Of<FXR3>(),
    });

    public static readonly Game[] All = [DarkSouls, DarkSouls2, DarkSouls3, Sekiro, EldenRing, Nightreign];

    public const string RootVariable = "SOULSFORMATS_GAME_DATA";

    public static string? Root => Environment.GetEnvironmentVariable(RootVariable) is { Length: > 0 } root ? root : null;

    public string Directory => Path.Combine(Root ?? throw new InvalidOperationException($"{RootVariable} isn't set."), AppId.ToString());

    public Format? FormatOf(string extension)
    {
        if (Formats.TryGetValue(extension, out Format? format) || Format.Common.TryGetValue(extension, out format))
            return format;

        return extension.EndsWith("bnd", StringComparison.OrdinalIgnoreCase) ? Format.Binder : null;
    }

    public IEnumerable<string> Files() => System.IO.Directory
        .EnumerateFiles(Directory, "*", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(Directory, path))
        .Order(StringComparer.Ordinal);

    public override string ToString() => Name;
}
