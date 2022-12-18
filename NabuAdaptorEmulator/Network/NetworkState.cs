namespace Nabu.Network;

public class NetworkState
{
    public ChannelSources? Sources { get; set; } = new();
    public string? Source { get; set; }
    public Dictionary<string, Channel> Channels { get; } = new();
    public bool HasChannels => Channels.Count > 0;

    public Dictionary<int, byte[]> SegmentCache { get; private set; } = new();
    public void ClearCache()
    {
        SegmentCache.Clear();
        SegmentCache = new();
    }
}
