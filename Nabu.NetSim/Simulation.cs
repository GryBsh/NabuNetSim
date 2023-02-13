using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Network;
using Nabu.Network.NHACP;
using Nabu.Network.RetroNet;
using Nabu;
using System.Text;

namespace Nabu;

public class Simulation : BackgroundService, ISimulation
{
    readonly IConsole Logger;
    readonly AdaptorSettings[] DefinedAdaptors;
    private readonly IServiceProvider ServiceProvider;

    public Simulation(
        IConsole<Simulation> logger,
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

    ServiceShould[] Next { get; set; } = Array.Empty<ServiceShould>();

    public void StartAdaptor(AdaptorSettings settings)
    {
        int index = Array.IndexOf(DefinedAdaptors, settings);
        if (index >= 0)
            Next[index] = ServiceShould.Restart;
    }

    public void StopAdaptor(AdaptorSettings settings)
    {
        int index = Array.IndexOf(DefinedAdaptors, settings);
        if (index >= 0)
            Next[index] = ServiceShould.Stop;
    }

    public void ToggleAdaptor(AdaptorSettings settings)
    {
        int index = Array.IndexOf(DefinedAdaptors, settings);
        if (index >= 0)
            Next[index] = Next[index] == ServiceShould.Continue ? ServiceShould.Stop : ServiceShould.Continue;

    }

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {

        await Task.Run(() =>
        {
            // We are going to keep track of the services that were defined
            // so if they stop, we can restart them
            Span<Task> services =  NabuLib.SetLength(
                DefinedAdaptors.Length,
                Array.Empty<Task>(),
                Task.CompletedTask
            ).ToArray();

            Span<ServiceShould> next = Next = NabuLib.SetLength(
                DefinedAdaptors.Length,
                Array.Empty<ServiceShould>(),
                ServiceShould.Continue
            ).ToArray();

            Span<CancellationTokenSource> cancel = NabuLib.SetLength(
                DefinedAdaptors.Length,
                Array.Empty<CancellationTokenSource>(),
                () => CancellationTokenSource.CreateLinkedTokenSource(stopping)
            ).ToArray();


            //int[] fails = new int[DefinedAdaptors.Length];
            //bool started = false;
            Logger.Write($"Defined Adaptors: {DefinedAdaptors.Length}");

            // Until the host tells us to stop
            while (stopping.IsCancellationRequested is false)
            {
                for (int index = 0; index < DefinedAdaptors.Length; index++)
                {
                    var settings = DefinedAdaptors[index];

                    if (settings.Enabled is false)
                    {
                        settings.Next = ServiceShould.Stop;
                        continue;
                    }

                    if (settings.Next is ServiceShould.Restart or ServiceShould.Stop)
                    {
                        cancel[index].Cancel();
                        services[index] = Task.CompletedTask;
                        cancel[index] = CancellationTokenSource.CreateLinkedTokenSource(stopping);
                    }
                    if (settings.Next is ServiceShould.Restart)
                        settings.Next = ServiceShould.Continue;

                    // Is this service stopped?
                    if (services[index].IsCompleted && settings.Next is ServiceShould.Continue && settings.Enabled)
                    {
                        // If so, restart it
                        var token = cancel[index].Token;
                        services[index] = settings.Type switch
                        {
                            AdaptorType.Serial => Task.Run(() => SerialAdaptor.Start(ServiceProvider, (SerialAdaptorSettings)settings, token)),
                            AdaptorType.TCP => Task.Run(() => TCPAdaptor.Start(ServiceProvider, (TCPAdaptorSettings)settings, token)),
                            _ => throw new NotImplementedException()
                        };
                        settings.Running = true;
                    }

                }
                //started = true;
                Thread.Sleep(10); // Lazy Wait, we don't care how long it takes to resume
            }
        }, stopping);
    }

    public static IServiceCollection Register(IServiceCollection services)
    {
        services //.AddTransient<HttpProgramRetriever>()
                 //.AddTransient<FileProgramRetriever>()
                 //.AddTransient<ProgramSourceService>()
                .AddTransient<NabuNetwork>()
                .AddTransient<ClassicNabuProtocol>()
                .AddTransient(typeof(IConsole<>), typeof(MicrosoftExtensionsLoggingConsole<>))
                .AddTransient<IProtocol, NHACPProtocol>()
                .AddTransient<IProtocol, RetroNetTelnetProtocol>()
                .AddTransient<IProtocol, RetroNetProtocol>()
                .AddSingleton<ISimulation, Simulation>()
                .AddHostedService<Simulation>();
        return services;
    }
}