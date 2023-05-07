namespace Nabu.Packages;


public record PackageItem
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public record Package
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int[] Version { get; set; } = new []{ 0, 0, 0 };
    public string Author { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public PackageItem[] Programs { get; set; } = Array.Empty<PackageItem>();
    public Dictionary<string, bool> Features { get; set; } = new();
    public Dictionary<string, bool> Options { get; set; } = new();

}

public record PackageSource
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public record PackageSourceBuild 
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public record PackageSourceItem : Package
{
    public string Source { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}

public interface IPackageManager
{
    public IEnumerable<PackageSource> GetSources();
    public IEnumerable<Package> GetInstalledPackages();
    public IEnumerable<PackageSourceItem> GetAvailablePackages();
}

