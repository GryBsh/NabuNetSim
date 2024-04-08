namespace Napa;

public static class PackageFeatures
{
    public static string Storage { get; } = nameof(Storage).ToLowerInvariant();
    public static string Programs { get; } = nameof(Programs).ToLowerInvariant();
    public static string PAKs { get; } = nameof(PAKs).ToLowerInvariant();
    public static string Sources { get; } = nameof(Sources).ToLowerInvariant();
    public static string Cache { get; } = nameof(Cache).ToLowerInvariant();
    public static string Protocols { get; } = nameof(Protocols).ToLowerInvariant();
}