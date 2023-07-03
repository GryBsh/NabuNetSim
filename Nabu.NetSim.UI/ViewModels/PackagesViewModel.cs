using Nabu.Packages;
using Napa;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels
{
    public class PackagesViewModel : ReactiveObject, IActivatableViewModel
    {
        public PackagesViewModel(IPackageManager packages)
        {
            Packages = packages;

            Activator = new();
            InstalledPackages.CollectionChanged += InstalledPackages_CollectionChanged;
            this.RaisePropertyChanged(nameof(InstalledPackages));
        }

        public ViewModelActivator Activator { get; }
        public ObservableCollection<InstalledPackage> InstalledPackages => Packages.Installed;
        public IPackageManager Packages { get; }

        public bool IsDisabled(string id) => Packages.PreservedPackages.Contains(id);

        public async void StageUninstallPackage(string id)
        {
            await Packages.Uninstall(id);
            await Packages.Refresh(true);
            this.RaisePropertyChanged(nameof(InstalledPackages));
        }

        private void InstalledPackages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(InstalledPackages));
        }
    }
}