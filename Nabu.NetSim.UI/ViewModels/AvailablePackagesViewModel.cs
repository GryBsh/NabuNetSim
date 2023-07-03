using DynamicData;
using Napa;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels
{
    public class AvailablePackagesViewModel : ReactiveObject, IActivatableViewModel
    {
        public AvailablePackagesViewModel(IPackageManager packages, Settings settings)
        {
            Packages = packages;
            Settings = settings;
            Activator = new();

            AvailablePackages.CollectionChanged += AvailablePackages_CollectionChanged;
            this.RaisePropertyChanged(nameof(AvailablePackages));
        }

        public ViewModelActivator Activator { get; }
        public ObservableCollection<SourcePackage> AvailablePackages => Packages.Available;
        public IPackageManager Packages { get; }
        public Settings Settings { get; }

        public async void StagePackage(string id)
        {
            Packages.InstallQueue.Enqueue(id);
            await Packages.Refresh(true);
            //this.RaisePropertyChanged(nameof(AvailablePackages));
        }

        private void AvailablePackages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(AvailablePackages));
        }
    }
}