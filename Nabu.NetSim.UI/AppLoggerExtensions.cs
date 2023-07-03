using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Nabu.NetSim.UI;

public static class AppLoggerExtensions
{
    public static ILoggingBuilder AddDBLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.AddSingleton<ILoggerProvider, AppLogProvider>();

        return builder;
    }
}