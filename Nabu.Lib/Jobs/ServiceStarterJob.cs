using Gry.Jobs;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Settings;
using Nabu.Sources;
using Napa;
using YamlDotNet.Serialization;

namespace Nabu.Jobs
{
    public class ServiceStarterJob(
        ILogger<ServiceStarterJob> logger, 
        PackageService packages,
        StorageService storage
    ) : Job
    {
        protected ILogger Logger { get; } = logger;
        protected PackageService Packages { get; } = packages;
        protected StorageService Storage { get; } = storage;

        protected override void OnSchedule() 
        {
            Logger.LogDebug("Service Starter Complete");
        }
    }
}