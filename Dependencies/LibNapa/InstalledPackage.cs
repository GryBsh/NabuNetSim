namespace Napa;

public record InstalledPackage : SourcePackage
{
    public InstalledPackage(Package package, string path, string manifest) : base(package, "Installed", path)
    {
        ManifestPath = manifest;
    }

    public string ManifestPath { get; set; } = string.Empty;
}