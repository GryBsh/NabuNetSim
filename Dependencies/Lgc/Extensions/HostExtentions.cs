using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Lgc;

namespace Microsoft.Extensions.Hosting;

public static class HostExtentions
{
    public static IHostBuilder AddLgc(this IHostBuilder hostBuilder)
        => Runtime.Build(hostBuilder);

    public static void Bind(this IHost host, string section, object instance)
        => host.Configuration()?
            .Bind(section, instance);

    public static IConfiguration? Configuration(this IHost host)
        => host?.Service<IConfiguration>();

    public static ILogger<T>? Logger<T>(this IHost host)
        => host.Service<ILogger<T>>();

    public static TService? Service<TService>(this IHost host)
                        => host.Services
            .GetService<TService>();

    public static IEnumerable<TService> Services<TService>(this IHost host)
        => host.Services
            .GetServices<TService>();
}