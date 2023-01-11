namespace Nabu.Network;

public class NetworkState
{
    public List<SourceFolder> SourceDefinitions { get; set; } = new();
    public Dictionary<string, string> Sources { get; set; } = new();
    public string? Source { get; set; }
    public Dictionary<string, ProgramImage> ProgramImages { get; private set; } = new();
    public bool FoundImages => ProgramImages.Count > 0;
    public string? Image { get; set; }

    public string LastUpdatedSource { get; set; }

    public Dictionary<string, string> SourceCache { get; private set; } = new();
    public Dictionary<int, byte[]> PakCache { get; private set; } = new();

    public Dictionary<int, byte[]> ACPStorage { get; private set; } = new();

    public void ClearCache()
    {
        ACPStorage.Clear();
        ACPStorage = new();
        PakCache.Clear();
        PakCache = new();
        SourceCache.Clear();
        SourceCache = new();
        ProgramImages.Clear();
        ProgramImages = new();
    }
}