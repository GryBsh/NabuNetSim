namespace Nabu;

public abstract record AdaptorSettings
{
    public abstract AdaptorType Type { get; }
    public string Port { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Source { get; set; } = string.Empty;
    public string? Program { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public short AdapterChannel { get; set; } = 0x0001;
    public bool Running { get; set; }
    public bool EnableCopyOnSymLinkWrite { get; set; } = false;
    public Dictionary<string, string> StorageRedirects { get; set; } = new();
    public ServiceShould State { get; set; }
}

public record NullAdaptorSettings : AdaptorSettings
{
    public override AdaptorType Type => AdaptorType.Unknown;
}

public record TCPAdaptorSettings : AdaptorSettings
{
    public override AdaptorType Type => AdaptorType.TCP;
    public bool Client { get; set; } = false;
    public int ReceiveBufferSize { get; set; } = 8;
    public int SendBufferSize { get; set; } = 8;
    public bool Connection { get; set; } = false;
    public CancellationTokenSource? ListenTask { get; set; }
}

public record SerialAdaptorSettings : AdaptorSettings
{
    public override AdaptorType Type => AdaptorType.Serial;
    public int BaudRate { get; set; } = 115200;
    public int ReadTimeout { get; set; } = 1000;
}