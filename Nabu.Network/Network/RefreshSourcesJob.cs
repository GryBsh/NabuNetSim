using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.Network;

public class RefreshSourcesJob : Job
{
    public RefreshSourcesJob(IConsole<RefreshSourcesJob> logger, Settings settings, INabuNetwork network) : base(logger, settings)
    {
        Network = network;
    }

    public INabuNetwork Network { get; }

    public override void Start()
    {
        Observable.Interval(TimeSpan.FromMinutes(1))
            .Subscribe(_ => Network.BackgroundRefresh(RefreshType.Local));

        Observable.Interval(TimeSpan.FromMinutes(30))
            .Subscribe(_ => Network.BackgroundRefresh(RefreshType.Remote));
    }
}
