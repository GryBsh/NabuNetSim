using Gry;
using Gry.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nabu.Network;
using Nabu.Settings;
using Nabu.Sources;
using YamlDotNet.Serialization;

namespace Nabu.Adaptors;

public class AdaptorEventReceiver(
    ILogger<AdaptorEventReceiver> logger,
    StorageService storage,
    PackageService packages
) : IReceiver<AdaptorSettings>
{
    public async Task ReceiveAsync(string @event, AdaptorSettings context, CancellationToken cancel)
    {
        switch (@event)
        {
            case Adapter.Startup:
                await Startup(context);
                break;
        }
    }

    /// <summary>
    /// Initializes services associated with the started Adapter
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    async Task Startup(AdaptorSettings settings)
    {
        logger.LogDebug($"Adapter Activated: {settings.Name}");
        await Task.Run(() =>
        {
            storage.UpdateStorageFromPackages(packages.Packages);
            
            if (!string.IsNullOrWhiteSpace(settings.Name)) 
                storage.AttachStorage(settings, settings.Name);

            
        });
    }
}
