using Nabu.Packages;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Napa;

public interface IPackageManager
{
    ObservableRange<SourcePackage> Available { get; }
    ObservableRange<InstalledPackage> Installed { get; }
    ConcurrentQueue<string> InstallQueue { get; }
    List<string> PreservedPackages { get; }
    List<PackageSource> Sources { get; }
    ConcurrentQueue<string> UninstallQueue { get; }

    Task<bool> Create(string path, string destination);

    Task<FoundPackage> Install(string path, bool force = false);

    Task<FoundPackage> Open(string folder, string? name = null);

    public Task Refresh(bool silent = false);

    Task Uninstall(string id);

    bool Uninstall(InstalledPackage package);

    (bool, Package) Uninstall(InstalledPackage package, Package newPackage);
}