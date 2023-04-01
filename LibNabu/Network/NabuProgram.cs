using Nabu.Patching;

namespace Nabu.Network;

public record NabuProgram
{
    public NabuProgram()
    {
    }

    public NabuProgram(
        string DisplayName,
        string Name,
        string Source,
        string Path,
        SourceType SourceType,
        ImageType ImageType,
        IProgramPatch[] Patches,
        bool IsPakMenu = false
    ) {
        this.DisplayName = DisplayName;
        this.Name = Name;
        this.Source = Source;
        this.Path = Path;
        this.SourceType = SourceType;
        this.ImageType = ImageType;
        this.Patches = Patches;
        this.IsPakMenu = IsPakMenu;
    }

    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public ImageType ImageType { get; set; }
    public IProgramPatch[] Patches { get; set; } = Array.Empty<IProgramPatch>();
    public bool IsPakMenu { get; set; }
}
