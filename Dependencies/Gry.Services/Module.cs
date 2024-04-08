using Gry.Caching;
using Gry.Conversion;
using Gry.Serialization;
using Lgc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gry;

public class GryModule : Module
{
}

public class ConverterService(ILogger<ConverterService> logger, IServiceScopeFactory scopeFactory) : ScopingService(logger, scopeFactory), ISingletonDependency
{
    public bool CanConvert<TIn,TOut>(TIn? input)
    {
        using var scope = Scope();
        var converters = scope.Services<IConvert<TIn, TOut>>();
        if (!converters.Any()) return false;

        foreach (var converter in converters)
            if (converter.CanConvert(input)) 
                return true;
        return false;
    }

    public TOut? Convert<TOut, TIn>(TIn? input)
    {
        using var scope = Scope();
        var converters = scope.Services<IConvert<TIn, TOut>>();
        if (!converters.Any()) return default;

        try
        {
            return converters.First().Convert(input);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Conversion Error");
        }
        return default;
    }
}

public class ModuleBuilder : IRegister
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ISerializeProvider, SerializerProvider>();
        services.AddTransient<ISerialize, YAMLSerializer>();
        services.AddTransient<ISerialize, JSONSerializer>();

        services.Configure<CacheOptions>(configuration.GetSection("Cache"));

        services.AddSingleton<IFileCache, FileCache>();
        services.AddSingleton<IHttpCache, HttpCache>();
    }
}
