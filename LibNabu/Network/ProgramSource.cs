namespace Nabu.Network;

public record ProgramSource
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public ImageType ImageType { get; set; }
    public bool EnableRetroNet { get; set; }
}


