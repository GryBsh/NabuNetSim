namespace Nabu;

public enum AdaptorType
{
    Unknown = 0,
    Serial,
    TCP,
    Server,
    Relay
}

public record RelaySettings
{
    public bool Enabled { get; set; } = false;
    public string? Hostname { get; set; } // The hostname of the server
    public AdaptorType Type { get; set; } = AdaptorType.Unknown;
    public string? SerialPort { get; set; } // The serial port to use
    public int TCPPort { get; set; } = 5816; // The local port to connect to
}
public record AdaptorSettings
{
    public AdaptorType Type { get; set; } = AdaptorType.Unknown;
    public string Port { get; set; } = string.Empty; // The port to connect to, COM port, tty path, TCP port number or host:port
    public bool Enabled { get; set; } = true; // Enabled by default
    public short AdapterChannel { get; set; } = 0x0001; //The Channel of the Adaptor Emulator, setting this to 0 will show the prompt
    public string? Source { get; set; } // The name of the defined source to use for program images
    public string? Channel { get; set; } // The current "Channel", the name of the program image or pak folder to load.
    public int BaudRate { get; set; } = 115200; // 111865 or 111900 - I've seen both used;
    
    public int SendDelay { get; set; } = 0; // The send delay for SlowerSend
    public int ReadTimeout { get; set; } = 1000; // The serial port timeout
    
    public string StoragePath { get; set; } = "./Files"; //This folder is shipped IN zip with binaries.

}