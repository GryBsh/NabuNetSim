using Gry.Jobs;
using Lgc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;


namespace Gry.Adapters;

public abstract class AdapterServer<TOptions, TAdapter, TTCPAdapter, TSerialAdapter>(
    ILogger logger,
    TOptions options,
    IServiceScopeFactory scopes,
    AdapterManager adapters
    ) : BackgroundService, IService
    where TAdapter : AdapterDefinition
    where TOptions : AdapterServerOptions<TAdapter, TTCPAdapter, TSerialAdapter>
    where TTCPAdapter : TAdapter
    where TSerialAdapter : TAdapter
{

    protected TOptions Options = options;
    
    private static AdapterListener? Listener(IServiceProvider services, string type)
    {
        var handler = services.GetServices<IListener>().FirstOrDefault(
            a => a.Type.Equals(type, StringComparison.InvariantCultureIgnoreCase)
        );

        if (handler is not null) return handler.Listen;
        return null;
    }

    protected async override Task ExecuteAsync(CancellationToken stopping)
    {
        await Task.Run(() =>
        {
            using var scope = scopes.CreateScope();
            var services = scope.ServiceProvider;
            var jobs = services.GetServices<IJob>();

            foreach (var job in jobs)
                job.Schedule(stopping);



            logger.LogInformation("Defined Adapters: {}", Options.Adapters.Count());

            while (stopping.IsCancellationRequested is false)
            {
                try
                {
                    RefreshAdapters(services, stopping);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while managing adapters");
                }
                Thread.Sleep(100); 
            }

            foreach (var job in jobs)
                job.Cancel();

            adapters.Cancel();
        }, stopping);
    }

    private void RefreshAdapters(IServiceProvider services, CancellationToken stopping)
    {
        //adapters.Defined = [.. Options.Adapters];

        //Where we have options for an adapter, but not an adapter.
        var starting = Options.Adapters.Where(
            a => a.Enabled && 
                 !adapters.Running.Any(
                    t => t.Definition.Type == a.Type && 
                         t.Definition.Port == a.Port
                )
        );

        foreach (var adapter in starting)
            adapters.Add(
                adapter,
                new Adapter(
                    services.GetRequiredService<ILogger<Adapter>>(), 
                    adapter, 
                    Listener(services, adapter.Type), 
                    stopping
                )
            );

        var stopped = adapters.Running.Where(t => t.Definition.Enabled && t.State is AdapterState.Stopped);
        foreach (var start in stopped)
        {
            logger.LogInformation("Starting {}:{}", start.Definition.Type, start.Definition.Port);
            start.Start();
        }

        var shouldStop = adapters.Running.Where(
            t => !adapters.Defined.Any(a => a.Port == t.Definition.Port) ||             // Where we have a task for an emulator, but no adaptor definition, likely because it was removed.
                (t.Definition.Enabled is false && t.State is AdapterState.Running)     // or its disabled       
        );

        foreach (var stop in shouldStop)
        {
            var definition = stop.Definition;
            logger.LogInformation("Stopping {}:{}", definition.Type, definition.Port);
            stop.Dispose();
            adapters.Remove(stop.Definition);
            

            //logger.LogInformation("Stopped {}:{}", stop.Definition.Type, stop.Definition.Port);
        }
        
    }
}