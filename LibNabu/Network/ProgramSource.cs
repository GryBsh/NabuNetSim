namespace Nabu.Network;

public record ProgramSource
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public bool EnableRetroNet { get; set; }
    public bool EnableQuirkLoader { get; set; }
}


