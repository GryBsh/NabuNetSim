namespace Nabu.Network;

public record ProgramSource
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public bool EnableRetroNet { get; set; }
    public bool EnableRetroNetTCPServer { get; set; }
    public bool EnableExploitLoader { get; set; }
    public int? TCPServerPort { get; set; }
    public bool HeadlessMenu { get; set; } = false;
    public string? SourcePackage { get; set; }
}