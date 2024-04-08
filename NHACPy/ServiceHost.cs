using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Gry;
using Gry.Caching;
using Gry.Protocols;
using Lgc;
using Lgc.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NHACP;
using NHACP.V01;
using System.Reactive.Subjects;

namespace NHACPy;

public class ProtocolHostInfo : IProtocolHostInfo
{
    public string Name { get; } = $"NHACPy";
    public string Description { get; } = "NHACPy Server";
    public string Version { get; } = "1";
}

public class ServiceHost(params Type[] protocols) : IHost
{

    IHost Host = new NullHost();

    IHost BuildHost()
    {
        Runtime.AddModule<GryModule>();
        Runtime.AddModule<NHACPModule>();

        return Runtime.Build(Register);
    }

    private void Register(HostBuilderContext context, IServiceCollection services)
    {
        /*services.AddLogging(l => l.AddSystemdConsole(
            c => {
                c.IncludeScopes = false;
                c.TimestampFormat = "yyyy/MM/dd HH:mm:ss ";
            }
        ));*/

        services.AddLogging(
            l => l.AddSimpleConsole(
                c =>
                {
                    c.SingleLine = true;
                    c.IncludeScopes = false;
                    c.TimestampFormat = "yyyy/MM/dd HH:mm:ss ";
                }    
            )
        );

        services.AddHttpClient();

        services.AddTransient<
            IProtocol<NHACPyAdapter>,
            NHACPV01Protocol<NHACPyAdapter>
        >();

        foreach (var protocol in protocols)
            services.AddTransient(typeof(IProtocol<NHACPyAdapter>), protocol);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Host is not NullHost) return;

        Host = BuildHost();
        await Host.StartAsync(cancellationToken);
    }

    public void WaitForShutdown() => Host.WaitForShutdown();

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Host is NullHost) return;
        await Host.StopAsync(cancellationToken);
        await Host.WaitForShutdownAsync(cancellationToken);
    }

    public IServiceProvider Services => Host.Services;

#pragma warning disable CA1816 // This is a wrapper around the host, so the host needs to be disposed
    void IDisposable.Dispose() => Host?.Dispose();
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
}
