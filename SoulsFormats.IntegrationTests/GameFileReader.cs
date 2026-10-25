namespace SoulsFormats.IntegrationTests;

public sealed class GameFileReader(Game game)
{
    public List<string> Failures { get; } = [];

    public bool TryRead(string path, byte[] bytes)
    {
        Format? format = game.FormatOf(Format.ExtensionOf(path));
        if (format is null)
            return false;

        if (DCX.Is(bytes))
            bytes = DCX.Decompress(bytes);

        if (format.Read(bytes) is IBinder binder)
            ReadEntries(path, binder);

        return true;
    }

    public void ReadEntries(string path, IBinder binder)
    {
        foreach (BinderFile entry in binder.Files)
        {
            string entryPath = $"{path}/{entry.Name ?? entry.ID.ToString()}";

            try
            {
                TryRead(entryPath, entry.Bytes);
            }
            catch (Exception ex)
            {
                Failures.Add($"{entryPath}: {ex}");
            }
        }
    }
}
