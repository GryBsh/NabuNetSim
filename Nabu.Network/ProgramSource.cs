namespace Nabu;

public record ProgramSource
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public SourceType SourceType { get; internal set; }
    public ImageType ImageType { get; set; }
    public bool EnableRetroNet { get; set; }
}


