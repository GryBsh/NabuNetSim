using Nabu.Network;

namespace Nabu.Patching;

public interface IProgramPatch
{
    string Name { get; }

    Task<byte[]> Patch(NabuProgram source, byte[] program);
}