
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

    public Task<byte[]> Patch(NabuProgram source, byte[] program)
    {
        Logger.Write($"Source {source.DisplayName}: Pass-Through");
        return Task.FromResult(program);
    }
}

public class BootstrapPatch : IProgramPatch
{
    private readonly IConsole Logger;
    public string Name => nameof(BootstrapPatch);

    public BootstrapPatch(IConsole logger)
    {
        Logger = logger;
    }

    public Task<byte[]> Patch(NabuProgram source, byte[] program)
    {
        Logger.Write($"Source {source.DisplayName}: Bootstrap");
        
        return Task.FromResult(program);
    }
}