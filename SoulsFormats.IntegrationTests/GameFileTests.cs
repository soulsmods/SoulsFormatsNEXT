namespace SoulsFormats.IntegrationTests;

[Parallelizable(ParallelScope.All)]
public class GameFileTests
{
    public static IEnumerable<TestCaseData> Files() => Game.All.SelectMany(game =>
        Game.Root is null ? [Missing(game, $"{Game.RootVariable} isn't set.")]
        : !Directory.Exists(game.Directory) ? [Missing(game, $"{game} isn't at {game.Directory}.")]
        : game.Files().Select(path => new TestCaseData(game, path).SetArgDisplayNames(game.Name, path).SetCategory(game.Name)));

    private static TestCaseData Missing(Game game, string reason) =>
        new TestCaseData(game, "").SetArgDisplayNames(game.Name).Ignore(reason);

    [TestCaseSource(nameof(Files))]
    public void Reads(Game game, string path)
    {
        string fullPath = Path.Combine(game.Directory, path);
        string extension = Format.ExtensionOf(path);

        if (extension.EndsWith("bdt", StringComparison.OrdinalIgnoreCase))
            Assert.Ignore();

        var reader = new GameFileReader(game);

        if (extension.EndsWith("bhd", StringComparison.OrdinalIgnoreCase))
            reader.ReadEntries(path, ReadSplitBinder(fullPath));
        else if (!reader.TryRead(path, File.ReadAllBytes(fullPath)))
            Assert.Ignore($"No format for .{extension}.");

        if (reader.Failures.Count > 0)
            Assert.Fail(string.Join("\n\n", reader.Failures));
    }

    private static IBinder ReadSplitBinder(string headerPath)
    {
        byte[] header = File.ReadAllBytes(headerPath);
        string name = Path.GetFileName(headerPath);
        int bhd = name.LastIndexOf("bhd", StringComparison.OrdinalIgnoreCase);
        string dataPath = Path.Combine(Path.GetDirectoryName(headerPath)!, name[..bhd] + "bdt" + name[(bhd + 3)..]);

        return BXF3.IsHeader(header) ? BXF3.Read(header, dataPath) : BXF4.Read(header, dataPath);
    }
}
