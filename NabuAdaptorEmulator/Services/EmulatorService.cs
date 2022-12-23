using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Network;

namespace Nabu.Services;

public class EmulatorService : BackgroundService
{
    readonly ILogger Logger;
    readonly AdaptorSettings[] DefinedAdaptors;
    private readonly IServiceProvider ServiceProvider;

    public EmulatorService(
        ILogger<EmulatorService> logger,
        Settings settings,
        IServiceProvider serviceProvider
    )
    {
        Logger = logger;
        DefinedAdaptors = settings.Adaptors ?? Array.Empty<AdaptorSettings>();
        ServiceProvider = serviceProvider;
    }

    Task Serial(AdaptorSettings settings, CancellationToken stopping)
    {
        var adaptor = new SerialAdaptorEmulator(
            ServiceProvider.GetRequiredService<NetworkEmulator>(),
            ServiceProvider.GetRequiredService<ILogger<SerialAdaptorEmulator>>(),
            settings
        );
        return NabuService.From(
            adaptor.Emulate,
            settings,
            stopping,
            adaptor.Open,
            adaptor.Close
        );
    }

    Task TCP(AdaptorSettings settings, CancellationToken stopping)
    {
        var adaptor = new TCPAdaptorEmulator(
            ServiceProvider.GetRequiredService<NetworkEmulator>(),
            ServiceProvider.GetRequiredService<ILogger<TCPAdaptorEmulator>>(),
            settings
        );
        return NabuService.From(
            adaptor.Emulate,
            settings,
            stopping,
            adaptor.Open,
            adaptor.Close
        );
    }
       

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        await Task.Run(() => {

            // We are going to keep track of the services that were defined
            // so if they stop, we can restart them
            Task[] services = Tools.SetLength(
                DefinedAdaptors.Length, 
                Array.Empty<Task>(), 
                Task.CompletedTask
            );

            Logger.LogInformation($"Defined Adaptors: {DefinedAdaptors.Length}");
            foreach (var adaptor in DefinedAdaptors)
            {
                Logger.LogInformation($"Adaptor: {adaptor.Type}; On: {adaptor.Port}");
            }

            // Until the host tells us to stop
            while (stopping.IsCancellationRequested is false)
            {
                for (int index = 0; index < DefinedAdaptors.Length; index++)
                {
                    // Is this service stopped?
                    if (services[index].IsCompleted)
                    {
                        // If so, restart it
                        var settings = DefinedAdaptors[index];

                        services[index] = settings.Type switch
                        {
                            AdaptorType.Serial => Serial(settings, stopping),
                            AdaptorType.TCP => TCP(settings, stopping),
                            _ => throw new NotImplementedException()
                        }; // wrap the operations in a service         
                    }
                }
               
                Thread.Sleep(1000); // Lazy Wait, we don't care how long it takes to resume
            }
        }, stopping);
    }
}


