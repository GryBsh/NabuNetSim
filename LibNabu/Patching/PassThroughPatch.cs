using Nabu.Network;
using Nabu.Services;

namespace Nabu.Patching;

public class PassThroughPatch : IProgramPatch
{
    private readonly ILog Logger;
    public string Name => nameof(PassThroughPatch);

    public PassThroughPatch(ILog logger)
    {
        Logger = logger;
    }

    public Task<byte[]> Patch(NabuProgram program, byte[] bytes)
    {
        Logger.Write($"Program: {program.DisplayName}: Pass-Through");
        return Task.FromResult(bytes);
    }
}