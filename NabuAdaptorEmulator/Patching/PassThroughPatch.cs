
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
