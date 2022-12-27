namespace Nabu.Network;

public class NetworkState
{
    public ChannelSources? Sources { get; set; } = new();
    public string? Source { get; set; }
    public Dictionary<string, Channel> Channels { get; private set;} = new();
    public bool HasChannels => Channels.Count > 0;
    public string? Channel { get; set; }
    public Dictionary<string, string> SourceCache { get; private set;} = new();
    public Dictionary<int, byte[]> PakCache { get; private set; } = new();
    public void ClearCache()
    {
        PakCache.Clear();
        PakCache = new();
        SourceCache.Clear();
        SourceCache = new();
        Channels.Clear();
        Channels = new();
    }
}
