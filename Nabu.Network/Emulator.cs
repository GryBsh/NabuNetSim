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
    public const int Minor = 7;
    public const int Build = 8;
    public static readonly string Id = $"NabuNetSim v{Major}.{Minor}.{Build}";

    public static IServiceCollection Register(IServiceCollection services)
    {
        services.AddTransient<HttpProgramRetriever>();
        services.AddTransient<FileProgramRetriever>();
        services.AddTransient<NabuNetService>();
        services.AddTransient<ClassicNabuProtocol>();

        return services;
    }
}
