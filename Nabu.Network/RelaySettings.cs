namespace Nabu;

public record RelaySettings
{
    public bool Enabled { get; set; } = false;
    public string? Hostname { get; set; } // The hostname of the server
    public AdaptorType Type { get; set; } = AdaptorType.Unknown;
    public string? SerialPort { get; set; } // The serial port to use
    public int TCPPort { get; set; } = 5816; // The local port to connect to
}
