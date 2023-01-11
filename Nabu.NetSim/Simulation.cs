using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using System.Text;

namespace Nabu;


public enum ServiceShould
{
    Stop = 0,
    Continue,
    Restart
}


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
        DefinedAdaptors = NabuLib.Concat<AdaptorSettings>(
            settings.Adaptors.Serial, 
            settings.Adaptors.TCP
        ).ToArray();
        ServiceProvider = serviceProvider;
    }

    public Task[] Services { get; private set; } 
        = Array.Empty<Task>();
    public ServiceShould[] Next { get; private set; } 
        = Array.Empty<ServiceShould>();
    public CancellationTokenSource[] Cancel { get; private set; } 
        = Array.Empty<CancellationTokenSource>();

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        
        await Task.Run(() =>
        {
            // We are going to keep track of the services that were defined
            // so if they stop, we can restart them
            Span<Task> services = Services = NabuLib.SetLength(
                DefinedAdaptors.Length,
                Array.Empty<Task>(),
                Task.CompletedTask
            ).ToArray();

            Span<ServiceShould> next = Next = NabuLib.SetLength(
                DefinedAdaptors.Length,
                Array.Empty<ServiceShould>(),
                ServiceShould.Continue
            ).ToArray();

            Span<CancellationTokenSource> cancel = Cancel = NabuLib.SetLength(
                DefinedAdaptors.Length,
                Array.Empty<CancellationTokenSource>(),
                () => CancellationTokenSource.CreateLinkedTokenSource(stopping)
            ).ToArray();

            
            //int[] fails = new int[DefinedAdaptors.Length];
            //bool started = false;
            Logger.LogInformation($"Defined Adaptors: {DefinedAdaptors.Length}");

            // Until the host tells us to stop
            while (stopping.IsCancellationRequested is false)
            {
                for (int index = 0; index < DefinedAdaptors.Length; index++)
                {
                    if (Next[index] is ServiceShould.Restart or ServiceShould.Stop)
                    {
                        cancel[index].Cancel();
                        services[index] = Task.CompletedTask;
                        cancel[index] = CancellationTokenSource.CreateLinkedTokenSource(stopping);
                    }
                    if (Next[index] is ServiceShould.Restart)
                        next[index] = ServiceShould.Continue;

                    // Is this service stopped?
                    if (services[index].IsCompleted && Next[index] is ServiceShould.Continue)
                    {
                        // If it was already started, increase the fails
                        //if (started) fails[index] += 1;

                        // If so, restart it
                        var settings = DefinedAdaptors[index];
                        if (settings.Enabled is false) //but not if it's disabled
                            continue;

                        services[index] = settings.Type switch
                        {
                            AdaptorType.Serial => Task.Run(() => SerialAdaptor.Start(ServiceProvider, (SerialAdaptorSettings)settings, stopping, index)),
                            AdaptorType.TCP    => Task.Run(() => TCPAdaptor.Start(ServiceProvider, (TCPAdaptorSettings)settings, stopping, index)),
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