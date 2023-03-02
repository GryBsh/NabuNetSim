namespace Nabu.Network;

public record ProtocolSettings
{
    public string Path { get; set; } = string.Empty;
    public byte[] Commands { get; set; } = Array.Empty<byte>();
    public List<string> Modules { get; set; } = new();

}
