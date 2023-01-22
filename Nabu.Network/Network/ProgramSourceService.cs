using Nabu.Patching;

namespace Nabu.Network;

public class ProgramSourceService
{
    public List<ProgramSource> Sources { get; }
    public Dictionary<ProgramSource, IEnumerable<NabuProgram>> SourceCache { get; } = new();
    public ProgramSourceService(List<ProgramSource> sources)
    {
        Sources = sources;
    }

    
}
