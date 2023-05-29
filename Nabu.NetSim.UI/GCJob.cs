using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI;

public class GCJob : Job
{
    public GCJob(ILog<GCJob> logger, Settings settings) : base(logger, settings)
    {

    }

    public override void Start()
    {
        Observable.Interval(
            TimeSpan.FromMinutes(5)
        ).Subscribe(_ => {
            GC.Collect();
        });
    }
}
