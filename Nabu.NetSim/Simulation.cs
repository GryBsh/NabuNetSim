using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Network;
using Nabu.Network.NHACP;
using Nabu.Network.RetroNet;
using System.Text;
using Python.Runtime;
using Python.Included;
using Nabu.Services;
using NLog;

namespace Nabu;

public class Simulation : BackgroundService, ISimulation
{
    readonly IConsole Logger;
    readonly AdaptorSettings[] DefinedAdaptors;
    private readonly IServiceProvider ServiceProvider;
    Settings Settings { get; }
    public Simulation(
        IConsole<Simulation> logger,
        Settings settings,
        IServiceProvider serviceProvider
    )
    {
        Logger = logger;
        Settings = settings;
        DefinedAdaptors = NabuLib.Concat<AdaptorSettings>(
            settings.Adaptors.Serial,
            settings.Adaptors.TCP
        ).ToArray();
        ServiceProvider = serviceProvider;
    }

    ServiceShould[] Next { get; set; } = Array.Empty<ServiceShould>();

    CancellationTokenSource[] Cancel { get; set; } = Array.Empty<CancellationTokenSource>();

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
        if (Settings.Flags.Contains(Flags.EnablePython))
        {
            
            PythonProtocol.Startup(Logger);
        }


        //Logger.Write("GO SPEED RACER GO!");

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

            Span<CancellationTokenSource> Cancel = NabuLib.SetLength(
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
                        continue;
                    }
                                        
                    // Is this service stopped?
                    if (services[index].IsCompleted && settings.Next is ServiceShould.Continue && settings.Enabled)
                    {
                        // If so, restart it
                        var token = Cancel[index].Token;
                        services[index] = settings.Type switch
                        {
                            AdaptorType.Serial => Task.Run(() => SerialAdaptor.Start(ServiceProvider, (SerialAdaptorSettings)settings, token)),
                            AdaptorType.TCP when ((TCPAdaptorSettings)settings).Client => Task.Run(() => TCPClientAdaptor.Start(ServiceProvider, (TCPAdaptorSettings)settings, token)),
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

    

    

    public static IServiceCollection Register(IServiceCollection services, Settings settings)
    {
        services.AddTransient<NabuNetwork>()
                .AddTransient<ClassicNabuProtocol>()
                .AddTransient(typeof(IConsole<>), typeof(MicrosoftExtensionsLoggingConsole<>));

        if (settings.Flags.Contains(Flags.EnablePython))
        {
            var pythonLib = string.Empty;
            if (OperatingSystem.IsWindows()) pythonLib = "python311.dll";
            else if (OperatingSystem.IsLinux()) pythonLib = "libpython3.11.so";
            else if (OperatingSystem.IsMacOS()) pythonLib = "libpython3.11.dylib";

            var pythonPath = string.Empty;

            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
            var hits = new List<string>();
            foreach (var path in paths)
            {
                var searchPath = Path.Join(path, pythonLib);
                if (File.Exists(searchPath))
                    hits.Add(searchPath);
            }

            if (hits.Count > 0)
            {
                Runtime.PythonDLL = hits.OrderDescending().FirstOrDefault();
                if (Runtime.PythonDLL != null)
                {
                    foreach (var proto in settings.Protocols)
                    {
                        services.AddTransient<IProtocol>(
                            sp => new PythonProtocol(sp.GetService<IConsole<PythonProtocol>>()!, proto)
                        );
                    }
                }
            }
            else
            {
                settings.Flags.Remove(Flags.EnablePython);
            }
        }

        services.AddTransient<IProtocol, NHACPProtocol>()
                .AddTransient<IProtocol, RetroNetTelnetProtocol>()
                .AddTransient<IProtocol, RetroNetProtocol>()
                .AddSingleton<ISimulation, Simulation>()
                .AddHostedService<Simulation>();

        return services;
    }
}