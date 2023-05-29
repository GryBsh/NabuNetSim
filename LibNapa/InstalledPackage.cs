namespace Napa;

public record InstalledPackage : SourcePackage
{
    public InstalledPackage(Package package, string path) : base(package, "Installed", path)
    {

    }

}

