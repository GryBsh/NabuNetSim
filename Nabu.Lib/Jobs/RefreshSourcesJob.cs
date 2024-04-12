using Nabu.Logs;
using Nabu.Network;
using Gry;
using Nabu.Settings;
using Gry.Jobs;
using Microsoft.Extensions.Logging;

namespace Nabu.Jobs
{
    public class RefreshSourcesJob(ILogger<RefreshSourcesJob> logger, GlobalSettings settings, INabuNetwork network) : Job
    {
        public INabuNetwork Network { get; } = network;
        protected ILogger Logger { get; } = logger;
        protected GlobalSettings Settings { get; } = settings;

        protected override void OnSchedule()
        {
            Network.RefreshSources(RefreshType.All);

            Disposables.AddInterval(
                TimeSpan.FromSeconds(10),
                _ =>
                {
                    Network.RefreshSources(RefreshType.Local);
                }
            );
            Disposables.AddInterval(
                TimeSpan.FromMinutes(Settings.RemoteSourceRefreshIntervalMinutes),
                _ =>
                {
                    Network.RefreshSources(RefreshType.Remote);
                }
            );

        }
    }
}