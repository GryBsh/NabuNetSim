using Blazorise;
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
        public PackagesViewModel(IPackageManager packages, Settings settings)
        {
            Packages = packages;
            Settings = settings;
            Activator = new();
            InstalledPackages.CollectionChanged += InstalledPackages_CollectionChanged;
            this.RaisePropertyChanged(nameof(InstalledPackages));
        }

        public ViewModelActivator Activator { get; }
        public ObservableCollection<InstalledPackage> InstalledPackages => Packages.Installed;
        public IPackageManager Packages { get; }
        public Settings Settings { get; }
        public bool UninstallDisabled { get; set; } = false;

        public bool WarningVisible { get; set; } = false;

        private List<AdaptorSettings> NeedRestart { get; } = new();

        private string? PendingId { get; set; }

        public void EndStaging()
        {
            PendingId = null;
            UninstallDisabled = false;

            NeedRestart.ForEach((t) => t.State = ServiceShould.Run);
            NeedRestart.Clear();
            this.RaisePropertyChanged(nameof(UninstallDisabled));
            if (WarningVisible) HideWarning();
        }

        public bool IsDisabled(string id) => Packages.PreservedPackages.Contains(id);

        public Task OnClosing(ModalClosingEventArgs e)
        {
            EndStaging();
            return Task.CompletedTask;
        }

        public void StartStaging(string id)
        {
            PendingId = id;
            UninstallDisabled = true;
            WarningVisible = true;
            this.RaisePropertyChanged(nameof(UninstallDisabled));
            this.RaisePropertyChanged(nameof(WarningVisible));

        }

        public async void Uninstall()
        {
            if (PendingId is null) return;
            HideWarning();
            NeedRestart.AddRange(Settings.Adaptors.Serial.Where(s => s.Running));
            NeedRestart.AddRange(Settings.Adaptors.TCP.Where(t => t.Running));
            NeedRestart.ForEach(t => t.State = ServiceShould.Stop);

            await Packages.Uninstall(PendingId);
            await Packages.Refresh(true);
            //this.RaisePropertyChanged(nameof(AvailablePackages));
            EndStaging();
        }

        private void HideWarning()
        {
            WarningVisible = false;
            this.RaisePropertyChanged(nameof(WarningVisible));
        }

        private void InstalledPackages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(InstalledPackages));
        }
    }
}