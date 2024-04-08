using Gry;
using Gry.Jobs;

namespace Nabu.NetSim.UI;

public class GCJob : Job
{
    protected override void OnSchedule()
    {
        Disposables.AddInterval(
            TimeSpan.FromMinutes(5),
            _ => GC.Collect()
        );
    }
}