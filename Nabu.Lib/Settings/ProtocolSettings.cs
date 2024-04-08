namespace Nabu.Settings;

public record ProtocolSettings
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public byte[] Commands { get; set; } = [];
    public byte Version { get; set; } = 1;
}