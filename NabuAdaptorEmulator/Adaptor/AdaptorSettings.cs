
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
    public bool Enabled {get; set;} = true;
    public short AdapterChannel { get; set; } = 0x0001;
    public string? Source { get; set; }
    public string? Channel { get; set; }
    public int BaudRate { get; set; } = 111865;
    public string? TelnetHost { get; set; }
    public int? SendDelay { get; set; }
    public int ReadTimeout { get; set; } = 1000;

}


