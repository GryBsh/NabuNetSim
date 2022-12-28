namespace Nabu.Network;

public class NetworkState
{
    public ImageSourceDefinitions? SourceDefinitions { get; set; } = new();
    public string? Source { get; set; }
    public Dictionary<string, ProgramImage> Sources { get; private set;} = new();
    public bool HasChannels => Sources.Count > 0;
    public string? Channel { get; set; }
    public Dictionary<string, string> SourceCache { get; private set;} = new();
    public Dictionary<int, byte[]> PakCache { get; private set; } = new();

    public Dictionary<int, byte[]> ResponseCache { get; private set; } = new();

    public void ClearCache()
    {
        ResponseCache.Clear();
        ResponseCache = new();
        PakCache.Clear();
        PakCache = new();
        SourceCache.Clear();
        SourceCache = new();
        Sources.Clear();
        Sources = new();
    }
}
