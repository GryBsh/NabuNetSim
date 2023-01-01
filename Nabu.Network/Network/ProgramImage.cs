using Nabu.Patching;

namespace Nabu.Network;

public enum SourceType
{
    Unknown = 0,
    Local,
    Remote
}

public enum ImageType
{
    None = 0,
    Raw,
    Pak
}

public record ProgramImage(
    string DisplayName,
    string Name,
    string Source,
    DefinitionType DefinitionType,
    string Path,
    SourceType SourceType,
    ImageType ImageType,
    IPakPatch[] Patches
);