using Gry;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Napa;

public interface IPackageManager
{
    ObservableCollection<SourcePackage> Available { get; }
    ObservableCollection<InstalledPackage> Installed { get; }
    ConcurrentQueue<string> InstallQueue { get; }
    List<string> PreservedPackages { get; }
    List<PackageSource> Sources { get; }
    ConcurrentQueue<string> UninstallQueue { get; }

    Task<bool> Create(string path, string destination);

    Task<PackageInfo> Install(string path, bool force = false);

    Task<PackageInfo> Open(string folder, string? name = null);

    public Task Refresh(bool silent = false, bool localOnly = false);

    Task Uninstall(string id);

    bool Uninstall(InstalledPackage package);

    (bool, Package) Uninstall(InstalledPackage package, Package newPackage);
}