namespace Nabu;

public enum AdaptorType
{
    Unknown = 0,
    Serial,
    TCP,
    Relay
}


public class AdaptorCollection
{
    public List<SerialAdaptorSettings> Serial { get; } = new();
    public List<TCPAdaptorSettings> TCP { get; } = new();
}

public record SourceFolder
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public abstract record AdaptorSettings
{
    public abstract AdaptorType Type { get; }
    public string Port { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? Source { get; set; }
    public string? Image { get; set; }
    public string StoragePath { get; set; } = "./Files";
    public short AdapterChannel { get; set; } = 0x0001;
}

public record NullAdaptorSettings : AdaptorSettings
{
    public override AdaptorType Type => AdaptorType.Unknown;
}

public record TCPAdaptorSettings : AdaptorSettings
{
    public override AdaptorType Type => AdaptorType.TCP;
}

public record SerialAdaptorSettings : AdaptorSettings
{
    public override AdaptorType Type => AdaptorType.Serial;
    public int BaudRate { get; set; } = 115200; // 111865 or 111900 - I've seen both used;
    public int ReadTimeout { get; set; } = 1000;
}


