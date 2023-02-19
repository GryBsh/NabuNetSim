
using Microsoft.Extensions.Logging;
using Nabu.Network;

namespace Nabu.Patching;

public class PassThroughPatch : IProgramPatch
{
    private readonly IConsole Logger;
    public string Name => nameof(PassThroughPatch);

    public PassThroughPatch(IConsole logger)
    {
        Logger = logger;
    }

    public Task<byte[]> Patch(NabuProgram program, byte[] bytes)
    {
        Logger.Write($"Program: {program.DisplayName}: Pass-Through");
        return Task.FromResult(bytes);
    }
}

