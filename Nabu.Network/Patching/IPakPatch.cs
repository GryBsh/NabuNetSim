using Nabu.Network;

namespace Nabu.Patching;

public interface IPakPatch
{
    string Name { get; }
    Task<byte[]> Patch(NabuProgram source, byte[] program);
}