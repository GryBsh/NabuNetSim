using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Nabu.NetSim.UI;

public static class AppLoggerExtensions
{
    public static ILoggingBuilder AddInMemoryLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, AppLogProvider>()
        );

        LoggerProviderOptions.RegisterProviderOptions
            <AppLogConfiguration, AppLogProvider>(builder.Services);

        return builder;
    }

    public static ILoggingBuilder AddInMemoryLogger(this ILoggingBuilder builder,
        Action<AppLogConfiguration> configure)
    {
        builder.AddInMemoryLogger();
        builder.Services.Configure(configure);

        return builder;
    }
}

