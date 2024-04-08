using Lgc;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lgc.Hosting;

[Runtime.Invisible()]
public class NullHost : IHost
{
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IServiceProvider Services => throw new NotImplementedException();

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
