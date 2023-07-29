namespace Nabu;

/// <summary>
///     Version Numbers and adaptor Id
///     Used in various places
/// </summary>
public static class Emulator
{
    public const string Branch = "beta";
    public const int Build = 9;
    public const int Major = 0;
    public const int Minor = 9;
    public const int Release = 6;
    public static readonly string Id = $"NABU NetSim v{Version}";
    public static readonly string Version = $"{Major}.{Minor}.{Build}-{Branch}{Release}";
}