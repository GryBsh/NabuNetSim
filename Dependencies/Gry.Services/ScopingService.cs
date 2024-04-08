using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lgc;

namespace Gry;




public abstract class ScopingService(ILogger logger, IServiceScopeFactory scopeFactory)
{
    protected ILogger Logger { get; } = logger;
    protected IServiceScopeFactory ScopeFactory { get; } = scopeFactory;
    protected CancellationTokenSource CancellationTokenSource { get; } = new();

    protected async Task EachServiceAsync<T>(Func<T, CancellationToken, Task> action, CancellationToken cancel)
    {
        await WithServicesAsync<T>(async (items) => { foreach (var i in items) await action(i, cancel); });
    }

    protected IServiceScope Scope() => ScopeFactory.CreateScope();

    protected void WithService<T>(Action<T> action) where T : class
    {
        using var scope = Scope();
        var service = scope?.ServiceProvider.GetService<T>();
        if (service is not null) action(service);
    }

    protected void WithServices<T>(Action<IEnumerable<T>> action)
    {
        using var scope = Scope();
        var services = scope?.ServiceProvider.GetServices<T>();
        if (services is not null) action(services);
    }

    protected TY? WithServices<T, TY>(Func<IEnumerable<T>, TY?> func, TY? defaultValue)
    {
        using var scope = Scope();
        var services = scope?.ServiceProvider.GetServices<T>();
        if (services is not null)
            return func(services) ?? defaultValue!;
        return defaultValue!;
    }

    protected Task<TY?> WithServicesAsync<T, TY>(Func<IEnumerable<T>, Task<TY?>> func, TY? defaultValue = default)
    {
        using var scope = Scope();
        var services = scope?.ServiceProvider.GetServices<T>();
        if (services is not null)
            return func(services) ?? Task.FromResult(defaultValue);
        return Task.FromResult(defaultValue);
    }

    protected async Task WithServicesAsync<T>(Func<IEnumerable<T>, Task> action)
    {
        using var scope = Scope();
        var services = scope.ServiceProvider.GetServices<T>();
        if (services is not null) await action(services);
    }
}
