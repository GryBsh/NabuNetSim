using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gry.Adapters;

public abstract class Listener(
    ILogger logger,
    IServiceScopeFactory scopes
) : HostService(logger, scopes), IListener
{
    public const string Unhandled = $"{nameof(Listener)}_{nameof(Unhandled)}";

    public abstract string Type { get; }

    public abstract Task Listen(Adapter adapter, CancellationToken token);
}
