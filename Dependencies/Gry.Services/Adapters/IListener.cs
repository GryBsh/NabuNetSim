using Lgc;


namespace Gry.Adapters;

public interface IListener : IDependency
{
    string Type { get; }
    Task Listen(Adapter adapter, CancellationToken token);
}
