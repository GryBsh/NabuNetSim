using Lgc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lgc.Hosting;/// <summary>/// Provides a null implementation of IHost./// </summary>
[Runtime.Invisible]
public class NullHost() : IHost
{
    public Task StartAsync(CancellationToken cancellationToken = default)
    {        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {        return Task.CompletedTask;
    }

    public IServiceProvider Services { get; } = new NullServiceProvider();

    public void Dispose()
    {
        GC.SuppressFinalize(this); 
    }
}
