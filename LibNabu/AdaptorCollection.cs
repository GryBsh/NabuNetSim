namespace Nabu;

public class AdaptorCollection
{
    public List<SerialAdaptorSettings> Serial { get; } = new();
    public List<TCPAdaptorSettings> TCP { get; } = new();
}