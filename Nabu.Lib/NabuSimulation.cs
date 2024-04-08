using Gry.Adapters;
using Gry.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nabu.Settings;
using Nabu.Sources;


//using LiteDb.Extensions.Caching;

namespace Nabu
{
    public class NabuSimulation(
        ILogger<NabuSimulation> logger, 
        GlobalSettings options,
        IServiceScopeFactory scopes,
        AdapterManager adapters
    ) : AdapterServer<GlobalSettings, AdaptorSettings, TCPAdaptorSettings, SerialAdaptorSettings>(
        logger, 
        options,
        scopes,
        adapters
    ), ISimulation
    {
        public void ToggleAdaptor(AdaptorSettings settings)
        {
            settings.Enabled = !settings.Enabled;
            logger.LogInformation("{} toggled: Enabled: {}", settings.Name ?? $"{settings.Type}:{settings.Port}", settings.Enabled);
        }
    }
}