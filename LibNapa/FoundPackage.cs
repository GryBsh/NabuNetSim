namespace Napa;

public enum PackageType
{
    None,
    Napa,
    Folder
}

public record FoundPackage(string Path, Package? Package = null, PackageType Type = PackageType.None)
{
    public bool Found => Package is not null;
}