using Nabu.Packages;

namespace Nabu.Configuration
{
    public interface IPackageSettings
    {
        List<string> InstallPackages { get; set; }
        List<PackageSource> Sources { get; set; }
        List<string> UninstallPackages { get; set; }
    }
}