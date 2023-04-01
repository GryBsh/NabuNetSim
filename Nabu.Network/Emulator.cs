using Microsoft.Extensions.DependencyInjection;
using Nabu.Adaptor;
using Nabu.Network;

namespace Nabu;

/// <summary>
///     Version Numbers and adaptor Id
///     Used in various places
/// </summary>
public static class Emulator
{
    public const int Major = 0;
    public const int Minor = 9;
    public const int Build = 0;
    public static readonly string Id = $"NABU NetSim v{Major}.{Minor}.{Build}";
}
