using Nabu.Binary;

namespace Nabu.Adaptor;

public enum AdaptorType {
    Unknown = 0,
    Serial,
    TCP
}


public record AdaptorSettings()
{
    public AdaptorType Type { get; set; } = AdaptorType.Unknown;
    public string Port {get; set;} = string.Empty;
    public string LocalPort { get; set;} = string.Empty;
    public bool Enabled {get; set;} = false;
    public bool ChannelPrompt { get; set; } = false;
    public short AdapterChannel { get; set; } = 0x0000;
    public string? Source { get; set; }
    public string? Channel { get; set; }
    public int BaudRate { get; set; } = 111865;

}


