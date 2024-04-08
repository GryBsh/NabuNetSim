using Nabu.Sources;

namespace Nabu.Network
{
    public interface IProgramPatch
    {
        string Name { get; }

        Task<byte[]> Patch(NabuProgram source, byte[] program);
    }
}