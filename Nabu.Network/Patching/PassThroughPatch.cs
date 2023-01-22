
using Microsoft.Extensions.Logging;
using Nabu.Network;

namespace Nabu.Patching;

public class PassThroughPatch : IProgramPatch
{
    private readonly ILogger Logger;
    public string Name => nameof(PassThroughPatch);

    public PassThroughPatch(ILogger logger)
    {
        Logger = logger;
    }

    public Task<byte[]> Patch(NabuProgram source, byte[] program)
    {
        Logger.LogInformation($"Source {source.DisplayName}: Pass-Through");
        return Task.FromResult(program);
    }
}

public class BootstrapPatch : IProgramPatch
{
    private readonly ILogger Logger;
    public string Name => nameof(BootstrapPatch);

    public BootstrapPatch(ILogger logger)
    {
        Logger = logger;
    }

    public Task<byte[]> Patch(NabuProgram source, byte[] program)
    {
        Logger.LogInformation($"Source {source.DisplayName}: Bootstrap");
        
        
        
        return Task.FromResult(program);
    }
}