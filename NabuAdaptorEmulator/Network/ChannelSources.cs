namespace Nabu.Network;

public enum ChannelSourceType
{
    Unknown = 0,
    NabuRetroNet,
    Folder
}

public record ChannelSource
{
    public ChannelSourceType Type { get; set; } = ChannelSourceType.NabuRetroNet;
    public string? ListUrl { get; set; }
    public string? NabuRoot { get; set; }
    public string? PakRoot { get; set; }
}

public class ChannelSources : Dictionary<string, ChannelSource>
{

}
