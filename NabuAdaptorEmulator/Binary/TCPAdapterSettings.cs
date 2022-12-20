namespace Nabu.Binary;

public record TCPAdapterSettings()
{
    public int Port { get; set; } = 5816;
    public bool LocalOnly { get; set; } = true;
}


