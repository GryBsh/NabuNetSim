using Nabu.NetSim.UI.Models;
using Nabu.Services;
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
    public class FolderListViewModel : ReactiveObject, IActivatableViewModel
    {
        public FolderListViewModel(FilesViewModel files, Settings settings) {
            Files = files;
            Settings = settings;

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

        public FilesViewModel Files { get; }
        public Settings Settings { get; }

        public ViewModelActivator Activator { get; } = new();

        public IEnumerable<DirectoryModel> Folders { get; set; } = Array.Empty<DirectoryModel>();
        public void UpdateList()
        {
            Folders = Directory.GetDirectories(Settings.StoragePath)
                               .Select(d => new DirectoryModel { Name = Path.GetFileName(d), Path = d })
                               .Where(d => d.Name is not StorageNames.SourceFolder);
            this.RaisePropertyChanged(nameof(Folders));
        }

        public string FolderName(string port) => port.Split(Path.DirectorySeparatorChar)[^1].Split(':')[0];

        public void ShowStorage(string name)
        {
            name = FolderName(name);
            Files.SetRootDirectory(null, Path.Combine(Settings.StoragePath, name));
        }

        public void ShowPrograms()
        {
            Files.SetRootDirectory(null, Settings.LocalProgramPath);
        }
        public void ShowLogs()
        {
            Files.SetRootDirectory(null, Path.Join(AppDomain.CurrentDomain.BaseDirectory, "logs"));
        }
    }
}
