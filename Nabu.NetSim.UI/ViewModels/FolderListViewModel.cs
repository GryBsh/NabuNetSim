using Nabu.NetSim.UI.Models;
using Nabu.Network;
using Nabu.Settings;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels
{
    public class FolderListViewModel : ReactiveObject, IActivatableViewModel
    {
        public FolderListViewModel(FilesViewModel files, GlobalSettings settings, StorageService storage)
        {
            Files = files;
            Settings = settings;
            Storage = storage;
            this.WhenActivated(
                disposables =>
                {
                    Observable.Interval(TimeSpan.FromSeconds(5))
                          .Subscribe(
                            _ =>
                            {
                                UpdateList();
                            }
                          ).DisposeWith(disposables);
                });
            UpdateList();
        }

        public ViewModelActivator Activator { get; } = new();
        public FilesViewModel Files { get; }
        public IEnumerable<DirectoryModel> Folders { get; set; } = Array.Empty<DirectoryModel>();
        public GlobalSettings Settings { get; }
        public StorageService Storage { get; }

        public string FolderName(string port) => port.Split(Path.DirectorySeparatorChar)[^1].Split(':')[0];

        public void ShowLogs()
        {
            Files.SetRootDirectory(null, Path.Join(AppDomain.CurrentDomain.BaseDirectory, "logs"));
        }

        public void ShowPrograms()
        {
            Files.SetRootDirectory(null, Settings.LocalProgramPath);
        }

        public void ShowStorage(string name)
        {
            name = FolderName(name);
            Files.SetRootDirectory(null, Path.Combine(Storage.StorageRoot, name));
        }

        public void UpdateList()
        {
            if (!Path.Exists(Storage.StorageRoot))
                Directory.CreateDirectory(Storage.StorageRoot);

            Folders = Storage.ListDirectories(".")
                             .Select(d => new DirectoryModel { Name = Path.GetFileName(d), Path = d })
                             .Where(d => d.Name is not StorageNames.SourceFolder);

            this.RaisePropertyChanged(nameof(Folders));
        }
    }
}