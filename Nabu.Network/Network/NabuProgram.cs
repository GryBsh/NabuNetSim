using Nabu.Patching;

namespace Nabu.Network;



public record NabuProgram(
    string DisplayName,
    string Name,
    string Source,
    DefinitionType DefinitionType,
    string Path,
    SourceType SourceType,
    ImageType ImageType,
    IProgramPatch[] Patches
);