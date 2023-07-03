using Nabu.Services;
using Napa;
using System.Reactive.Linq;

namespace Nabu.Packages;

public class RefreshPackagesJob : Job
{
    private IPackageManager Packages { get; }

    public RefreshPackagesJob(ILog<RefreshPackagesJob> logger, Settings settings, IPackageManager packages)
        : base(logger, settings)
    {
        Packages = packages;
    }

    public override void Start()
    {
        //Observable.Interval(TimeSpan.FromMinutes(5))
         //         .Subscribe(async _ =>
          //        {
                      //Logger.Write("Updating Available Packages");
           //           await Packages.Refresh();
            //      });
    }
}