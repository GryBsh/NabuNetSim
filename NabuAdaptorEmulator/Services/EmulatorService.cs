using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Network;

namespace Nabu.Services;

public class EmulatorService : BackgroundService
{
    readonly ILogger Logger;
    readonly AdaptorSettings[] DefinedAdapters;
    private readonly IServiceProvider ServiceProvider;

    public EmulatorService(
        ILogger<EmulatorService> logger,
        Settings settings,
        IServiceProvider serviceProvider
    )
    {
        Logger = logger;
        DefinedAdapters = settings.Adaptors ?? Array.Empty<AdaptorSettings>();
        ServiceProvider = serviceProvider;
    }

    
    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        await Task.Run(() => {
            Task[] services = Tools.SetLength(
                DefinedAdapters.Length, 
                Array.Empty<Task>(), 
                Task.CompletedTask
            );

            while (stopping.IsCancellationRequested is false)
            {
                for (int index = 0; index < DefinedAdapters.Length; index++)
                {
                    if (services[index].IsCompleted)
                    {
                        var settings = DefinedAdapters[index];
                        AdaptorEmulator adaptor = settings.Type switch {
                            AdaptorType.Serial => new SerialAdaptorEmulator(
                                ServiceProvider.GetRequiredService<NetworkEmulator>(),
                                ServiceProvider.GetRequiredService<ILogger<SerialAdaptorEmulator>>(),
                                settings
                            ),
                            AdaptorType.TCP => new TCPAdaptorEmulator(
                                ServiceProvider.GetRequiredService<NetworkEmulator>(),
                                ServiceProvider.GetRequiredService<ILogger<TCPAdaptorEmulator>>(),
                                settings
                            ),
                            _ => throw new NotImplementedException()
                        };
                        
                        services[index] = NabuService.From(
                            adaptor.Emulate,
                            settings,
                            stopping,
                            adaptor.Open,
                            adaptor.Close
                        );         
                    }
                }
               
                Thread.Sleep(1000);
            }
        }, stopping);
    }
}


