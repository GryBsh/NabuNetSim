using Microsoft.Extensions.Logging;
using Nabu.Sources;

namespace Nabu.Network
{
    public class PassThroughPatch : IProgramPatch
    {
        private readonly ILogger Logger;
        public string Name => nameof(PassThroughPatch);

        public PassThroughPatch(ILogger logger)
        {
            Logger = logger;
        }

        public Task<byte[]> Patch(NabuProgram program, byte[] bytes)
        {
            Logger.LogInformation($"Program: {program.DisplayName}: Pass-Through");
            return Task.FromResult(bytes);
        }
    }
}