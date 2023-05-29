using Nabu.Services;
using System.Reactive.Disposables;
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
        Disposables.Add(
            Observable.Interval(TimeSpan.FromMinutes(1))
                .Subscribe(_ => Network.BackgroundRefresh(RefreshType.Local))
        );
        Disposables.Add(
            Observable.Interval(TimeSpan.FromMinutes(30))
                .Subscribe(_ => Network.BackgroundRefresh(RefreshType.Remote))
        );
    }
}
