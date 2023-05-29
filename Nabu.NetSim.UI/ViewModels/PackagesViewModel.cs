using Napa;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels
{
    public class PackagesViewModel : ReactiveObject, IActivatableViewModel
    {
        public IPackageManager Packages { get; }
        public HomeViewModel Home { get; }
        public ViewModelActivator Activator { get; }

        public ICollection<SourcePackage> InstalledPackages => Packages.Installed.ToList();

        public PackagesViewModel(IPackageManager packages, HomeViewModel home) {
            Packages = packages;
            Home = home;
            Activator = new();

            this.WhenActivated(
                disposables =>
                {
                    Observable.Interval(TimeSpan.FromMinutes(1), RxApp.TaskpoolScheduler)
                      .Subscribe(_ => this.RaisePropertyChanged(nameof(InstalledPackages)))
                      .DisposeWith(disposables);
                }
            );
            
        }
                
    }
}
