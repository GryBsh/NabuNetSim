using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.Network;

public class RefreshSourcesJob : Job
{
    public RefreshSourcesJob(ILog<RefreshSourcesJob> logger, Settings settings, INabuNetwork network) : base(logger, settings)
    {
        Network = network;
    }

    public INabuNetwork Network { get; }

    public override void Start()
    {
        Disposables.AddInterval(
            TimeSpan.FromMinutes(1),
            _ => {
                Network.BackgroundRefresh(RefreshType.Local);
            }
        );
        Disposables.AddInterval(
            TimeSpan.FromMinutes(Settings.RemoteSourceRefreshIntervalMinutes),
            _ => {
                Network.BackgroundRefresh(RefreshType.Remote);
            }
        );
        
    }
}