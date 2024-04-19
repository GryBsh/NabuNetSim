using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Lgc.Options;
using Microsoft.Extensions.Options;
using System.Xml.Linq;
using System.Reflection;
using Lgc.Extensions;
using System.Collections.Concurrent;

namespace Lgc;

public record ConfigurationValue();

public static partial class Runtime
{
    public static List<Type> Ignored { get; } = [];

    public static List<Type> Scoped { get; } = [
        typeof(IScopedDependency)
    ];

    public static IEnumerable<Type> ServiceTypes
        => EnumerableExtensions.Concat(
            Singleton,
            Scoped,
            Transient
        );

    public static List<Type> Singleton { get; } = [
        typeof(ISingletonDependency)
    ];

    public static List<Type> Transient { get; } = [
        typeof(IDependency)
    ];

    public static List<Type> Services { get; } = [];

    public static RuntimeOptions? Options { get; private set; }

    public static ConcurrentDictionary<string, object?> Settings { get; } = new();    public static IEnumerable<T?> ActivateAll<T>(params object[] args)         => from i in GetImplementationsOf<T>()           select Activate<T>(i, args);    public static IEnumerable<T?> ActivateAll<T>(Type type, params object[] args)         => from i in GetImplementationsOf(type)           select Activate<T>(i, args);

    public static IEnumerable<Y?> ActivateAll<T, Y>(params object[] args)         => from i in GetImplementationsOf<T>()           select Activate<Y>(i, args);

    /// <summary>
    /// Adds automatic registration / registrar types to the provided container
    /// </summary>
    /// <param name="services">Discovered services will be added to this collection</param>
    /// <param name="config">Configuration</param>
    /// <returns></returns>
    public static IServiceCollection Register(IServiceCollection services, IConfiguration config, RuntimeOptions? options = default)
    {
        Options = options ??= new();

        BindAndRegister(services, config, nameof(RuntimeOptions), options);

        IEnumerable<Type?> ignored = options.IgnoredTypes.Select(Type.GetType);

        foreach (var type in ignored)
        {
            if (type is null) continue;
            Ignored.Add(type);
        }
        
        var serviceTypes =
            options.Strict switch
            {
                true when !options.DisableDiscovery 
                    => Modules.Values.SelectMany(m => m.ServiceTypes),
                false when !options.DisableDiscovery 
                    => ServiceTypes
                        .SelectMany(GetImplementationsOf)
                        .Distinct(),
                _ => ServiceTypes.SelectMany(t => GetImplementationsOf(t, Services))
            };

        InvokeRegistrars(services, config);
        RegisterServices(services, serviceTypes);
        RegisterDependencyServices(services, options);
        BindOptions(services, config);

        return services;
    }

    public static IHost Build(Action<HostBuilderContext, IServiceCollection> register)
    {
        var builder = Build(Host.CreateDefaultBuilder());
        builder.ConfigureServices(register);
        return builder.Build();
    }

    public static IHostBuilder Build(IHostBuilder hostBuilder)
    {
        var builders = ActivateAll<IBuildHost>();

        foreach (var builder in builders)
        {
            builder?.Build(hostBuilder);
        }

        hostBuilder.ConfigureServices(
            (context, services) =>
                Register(services, context.Configuration)
        );

        return hostBuilder;
    }    public static T Build<T>(T appBuilder) where T : IHostApplicationBuilder
    {
        var builders = ActivateAll<IBuildApp<T>>();

        foreach (var builder in builders)
        {
            builder?.Build(appBuilder);
        }        Register(appBuilder.Services, appBuilder.Configuration);

        return appBuilder;
    }

    public static IHost Build<T>(IServiceProviderFactory<T> factory, string[]? args = null)
        where T : notnull
    {
        var hostBuilder = Build(
            Host.CreateDefaultBuilder(args)
        );
        hostBuilder.UseServiceProviderFactory(factory);
        
        return hostBuilder.Build();
    }

    public static IHost Build(string[]? args = null)
    {
        args ??= [];
        var hostBuilder = Build(
            Host.CreateDefaultBuilder(args)
        );
        return hostBuilder.Build();
    }

    public static void RegisterScoped<T>() => Scoped.Add(typeof(T));

    public static void RegisterSingleton<T>() => Singleton.Add(typeof(T));

    public static void RegisterTransient<T>() => Transient.Add(typeof(T));

    private static void BindAndRegister<T>(IServiceCollection services, IConfiguration config, string section, T instance)
        where T : class
        //=> BindAndRegister(services, config, section, typeof(T), instance);
    {
        section = Normalize.Name(section);
        config.Bind(section, instance);
        services.AddSingleton(typeof(T), instance);
    }

    private static void BindOptions(IServiceCollection services, IConfiguration config)
    {
        //var types = from type in GetImplementationsOf<IBoundConfiguration>()
        //            select (type, Activate(type));

        var types = GetImplementationsOf<IDependencyOptions>();
        services.AddOptions();

        var extType = typeof(OptionsConfigurationServiceCollectionExtensions);
        var extMethod = extType.GetMethod(
            nameof(OptionsConfigurationServiceCollectionExtensions.Configure), 
            [typeof(IServiceCollection), typeof(IConfiguration)]
        );
        foreach (Type type in types)
        {
            var nameAttribute =
                type
                .GetCustomAttributes(typeof(SectionNameAttribute), false)
                .Cast<SectionNameAttribute>()
                .FirstOrDefault();

            var sectionName = Normalize.Name(nameAttribute switch
            {
                not null => nameAttribute.Name,
                _ => type.Name
            });

            var section = config.GetSection(sectionName);

            var gMethod = extMethod?.MakeGenericMethod(type);
            gMethod?.Invoke(null, [services, section]);

        }
    }

    static List<Type> DisabledRegistrars { get; } = [];

    public static void DisableRegistrar<T>() => DisabledRegistrars.Add(typeof(T));

    private static void InvokeRegistrars(IServiceCollection services, IConfiguration config)
    {
        var registrars =
            from registrar in GetImplementationsOf<IRegister>()
            select Activate<IRegister>(registrar);

        foreach (var r in registrars.Where(r => !DisabledRegistrars.Contains(r.GetType())).ToArray())
            r.Register(services, config);
    }

    private static void RegisterDependencyServices(IServiceCollection services, RuntimeOptions options)
    {
        void register<T>()
        {
            foreach (var service in GetImplementationsOf<T>())
            {
                services.TryAddEnumerable(
                    ServiceDescriptor.Singleton(
                        typeof(T),
                        service
                    )
                );
            }
        }
        register<IHostedService>();
    }

    private static void RegisterServices(IServiceCollection services, IEnumerable<Type> types)
    {
        var constructable =
            from type in types
            let constructors = type.GetConstructors()
            where constructors.Any(c => c.IsPublic)
            select type;

        foreach (var type in constructable.ToArray())
        {
            
            static IEnumerable<Type> ServiceTypesOf(Type type)
            {
                var lineage = GetLineage(type);
                return (
                    from serviceType in ServiceTypes
                    let classes = GetImplementationsOf(serviceType, lineage)
                    let interfaces = GetInterfacesImplementing(serviceType, type)
                    from service in classes.Concat(interfaces)
                    where ServiceTypes.Contains(service) is false
                    select service
                ).Distinct();
            }

            ServiceLifetime lifetime = type switch
            {
                _ when AssignableToAny(type, Singleton) => ServiceLifetime.Singleton,
                _ when AssignableToAny(type, Scoped) => ServiceLifetime.Scoped,
                _ => ServiceLifetime.Transient,
            };

            var serviceTypes = ServiceTypesOf(type);
            foreach (var serviceType in serviceTypes)
            {
                services.Add(
                    ServiceDescriptor.Describe(serviceType, type, lifetime)
                );
            }
        }
    }
}