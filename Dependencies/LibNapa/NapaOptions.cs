namespace Napa
{
    public record NapaOptions
    {
        public List<PackageSource> Sources { get; private set; } = new();
        public List<string> UninstallPackageIds {  get; private set; } = new();
        public List<string> UninstallPackages { get; private set; } = new();
        public List<string> InstallPackages { get; private set; } = new();
        public string PackagePath { get; set; }
    }
}
