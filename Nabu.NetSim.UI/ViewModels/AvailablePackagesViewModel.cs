using Blazorise;
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
        public bool InstallDisabled { get; set; } = false;
        public IPackageManager Packages { get; }
        public Settings Settings { get; }
        public bool WarningVisible { get; set; } = false;

        private List<AdaptorSettings> NeedRestart { get; } = new();
        private string? PendingId { get; set; }

        public void EndStaging()
        {
            PendingId = null;
            InstallDisabled = false;

            NeedRestart.ForEach((t) => t.State = ServiceShould.Run);
            NeedRestart.Clear();
            this.RaisePropertyChanged(nameof(InstallDisabled));
            if (WarningVisible) HideWarning();
        }

        public async void Install()
        {
            if (PendingId is null) return;
            HideWarning();

            Packages.InstallQueue.Enqueue(PendingId);
            await Packages.Refresh(true);
            //this.RaisePropertyChanged(nameof(AvailablePackages));
            EndStaging();
        }

        public Task OnClosing(ModalClosingEventArgs e)
        {
            EndStaging();
            return Task.CompletedTask;
        }

        public void StartStaging(string id)
        {
            PendingId = id;
            InstallDisabled = true;
            WarningVisible = true;
            this.RaisePropertyChanged(nameof(InstallDisabled));
            this.RaisePropertyChanged(nameof(WarningVisible));
            NeedRestart.AddRange(Settings.Adaptors.Serial.Where(s => s.Running));
            NeedRestart.AddRange(Settings.Adaptors.TCP.Where(t => t.Running));
            NeedRestart.ForEach(t => t.State = ServiceShould.Stop);
        }

        private void AvailablePackages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(AvailablePackages));
        }

        private void HideWarning()
        {
            WarningVisible = false;
            this.RaisePropertyChanged(nameof(WarningVisible));
        }
    }
}