using Lgc;

namespace Gry;

public interface IReceiver<T> : IDependency
{
    Task ReceiveAsync(string @event, T context, CancellationToken cancel);
}
