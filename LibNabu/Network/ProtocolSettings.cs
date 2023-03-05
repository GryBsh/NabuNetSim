namespace Nabu.Network;

public record ProtocolSettings
{
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public byte[] Commands { get; set; } = Array.Empty<byte>();

}
