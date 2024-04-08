using Lgc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gry;

public abstract class HostService(ILogger logger, IServiceScopeFactory scopes) : ScopingService(logger, scopes)
{
    protected async Task SendAsync<T>(string @event, T context, CancellationToken? cancel = null)
    {
        await EachServiceAsync<IReceiver<T>>(
            async (r, cancel) =>
            {
                await r.ReceiveAsync(@event, context, cancel);
            },
            cancel ?? CancellationTokenSource.Token
        );
    }

    protected async void Send<T>(string @event, T context, CancellationToken? cancel = null)
    {
        await SendAsync(@event, context, cancel);
    }
}


