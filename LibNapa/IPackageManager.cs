using Nabu.Network;
using Nabu.Packages;

namespace Napa;

public interface IPackageManager
{
    public IList<PackageSource> Sources { get; }
    public IEnumerable<SourcePackage> Installed { get; }
    public IEnumerable<SourcePackage> Available { get; }
    public Task Refresh();
    public Task UpdateInstalled();
    public Task UpdateAvailable();
    Task<FoundPackage> Open(string folder, string? name = null);
    Task<bool> CreatePackage(string path, string destination);
    Task<Package?> InstallPackage(string path);
    bool UninstallPackage(Package package);
    (bool, Package) UninstallPackage(Package package, Package newPackage);
    public IEnumerable<SourcePackage> AvailablePackages(PackageSource source);
}

