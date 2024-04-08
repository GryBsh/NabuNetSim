using Nabu.Settings;
using Nabu.Sources;

namespace Nabu.Network
{
    public interface INabuNetwork
    {
        void RefreshSources(RefreshType refresh);

        IEnumerable<NabuProgram> Programs(AdaptorSettings settings);

        IEnumerable<NabuProgram> Programs(string? name);

        IEnumerable<NabuProgram> Programs(ProgramSource? source);

        Task<(ImageType, Memory<byte>)> Request(AdaptorSettings settings, int pak);

        ProgramSource? Source(AdaptorSettings settings);

        void UnCachePak(AdaptorSettings settings, int pak);
    }
}