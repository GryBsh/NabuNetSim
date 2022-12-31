
using Microsoft.Extensions.Logging;
using Nabu.Network;

namespace Nabu.Patching;

public class PassThroughPatch : IPakPatch
{
    private readonly ILogger Logger;
    public string Name => nameof(PassThroughPatch);

    public PassThroughPatch(ILogger logger)
    {
        Logger = logger;
    }

    public Task<byte[]> Patch(ProgramImage source, byte[] program)
    {
        Logger.LogInformation($"Source {source.DisplayName}: Pass-Through");
        return Task.FromResult(program);
    }
}