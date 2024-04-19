using Microsoft.Extensions.DependencyInjection;
using Nabu.Network;
using Nabu.Settings;
using Gry.Settings;

namespace Nabu;public static class ServiceCollectionExtensions{    public static GlobalSettings? GetSettings(this IServiceCollection services)    {        return services.FirstOrDefault(t => t.ServiceType == typeof(GlobalSettings))?.ImplementationInstance as GlobalSettings;    }    public static ILocationService GetLocator(this IServiceCollection services)    {        return new LocationService(settings: services.GetSettings());    }}