using Nabu.Patching;

namespace Nabu.Network;

public record NabuProgram
{
    public NabuProgram()
    {
    }

    public NabuProgram(
        string displayName,
        string name,
        string source,
        string path,
        SourceType sourceType,
        ImageType imageType,
        IProgramPatch[] patches,
        bool isPakMenu = false,
        bool enableClientIsolation = false
    ) {
        DisplayName = displayName;
        Name = name;
        Source = source;
        Path = path;
        SourceType = sourceType;
        ImageType = imageType;
        Patches = patches;
        IsPakMenu = isPakMenu;
        EnableClientIsolation = enableClientIsolation;
    }

    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public ImageType ImageType { get; set; }
    public IProgramPatch[] Patches { get; set; } = Array.Empty<IProgramPatch>();
    public bool IsPakMenu { get; set; }
    public bool EnableClientIsolation { get; set; }
}
