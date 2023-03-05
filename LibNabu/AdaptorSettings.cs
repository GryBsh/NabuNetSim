namespace Nabu;



public abstract record AdaptorSettings
{
    public abstract AdaptorType Type { get; }
    public string Port { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Source { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string StoragePath { get; set; } = "./Files";
    public short AdapterChannel { get; set; } = 0x0001;
    public bool Running { get; set; }
    public ServiceShould Next { get; set; }
    
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
}

public record SerialAdaptorSettings : AdaptorSettings
{
    public override AdaptorType Type => AdaptorType.Serial;
    public int BaudRate { get; set; } = 115200; // 111861 
    public int ReadTimeout { get; set; } = 1000;
}


