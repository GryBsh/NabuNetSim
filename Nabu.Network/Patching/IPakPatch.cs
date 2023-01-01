using Nabu.Network;

namespace Nabu.Patching;

public interface IPakPatch
{
    string Name { get; }
    Task<byte[]> Patch(ProgramImage source, byte[] program);
}