using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;

namespace Nabu;

public class Simulation : BackgroundService
{
    readonly ILogger Logger;
    readonly AdaptorSettings[] DefinedAdaptors;
    private readonly IServiceProvider ServiceProvider;

    public Simulation(
        ILogger<Simulation> logger,
        Settings settings,
        IServiceProvider serviceProvider
    )
    {
        Logger = logger;
        DefinedAdaptors = settings.Adaptors ?? Array.Empty<AdaptorSettings>();
        ServiceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        await Task.Run(() =>
        {

            // We are going to keep track of the services that were defined
            // so if they stop, we can restart them
            Task[] services = NabuLib.SetLength(
                DefinedAdaptors.Length,
                Array.Empty<Task>(),
                Task.CompletedTask
            );

            //int[] fails = new int[DefinedAdaptors.Length];
            //bool started = false;
            Logger.LogInformation($"Defined Adaptors: {DefinedAdaptors.Length}");


            // Until the host tells us to stop
            while (stopping.IsCancellationRequested is false)
            {
                for (int index = 0; index < DefinedAdaptors.Length; index++)
                {
                    // Is this service stopped?
                    if (services[index].IsCompleted)
                    {
                        // If it was already started, increase the fails
                        //if (started) fails[index] += 1;

                        // If so, restart it
                        var settings = DefinedAdaptors[index];
                        if (settings.Enabled is false) //but not if it's disabled
                            continue;

                        services[index] = settings.Type switch
                        {
                            AdaptorType.Serial => Task.Run(() => SerialAdaptor.Start(ServiceProvider, settings, stopping)),
                            AdaptorType.TCP => Task.Run(() => TCPAdaptor.Start(ServiceProvider, settings, stopping)),
                            _ => throw new NotImplementedException()
                        };
                    }
                    
                }
                //started = true;
                Thread.Sleep(100); // Lazy Wait, we don't care how long it takes to resume
            }
        }, stopping);
    }
}