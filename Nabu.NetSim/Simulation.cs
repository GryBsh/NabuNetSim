using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nabu.Adaptor;
using Nabu.Network;
using Nabu.Network.RetroNet;
using Python.Runtime;
using Nabu.Services;
using Nabu.Network.NHACP.V01;
using Nabu.Network.NHACP.V0;
using Nabu.JavaScript;
//using LiteDb.Extensions.Caching;

namespace Nabu;

public class Simulation : BackgroundService, ISimulation
{
    readonly IConsole Logger;
    readonly AdaptorSettings[] DefinedAdaptors;
    private readonly IServiceProvider ServiceProvider;
    Settings Settings { get; }
    readonly IEnumerable<IJob> Jobs;

    public Simulation(
        IConsole<Simulation> logger,
        Settings settings,
        IServiceProvider serviceProvider,
        IEnumerable<IJob> jobs
    )
    {
        Logger = logger;
        Settings = settings;
        DefinedAdaptors = NabuLib.Concat<AdaptorSettings>(
            settings.Adaptors.Serial,
            settings.Adaptors.TCP
        ).ToArray();
        ServiceProvider = serviceProvider;
        Jobs = jobs;
    }


    static CancellationTokenSource[] Cancel { get; set; } = Array.Empty<CancellationTokenSource>();
        
    public void ToggleAdaptor(AdaptorSettings settings)
    {

        if (settings.State is ServiceShould.Run)
            settings.State = ServiceShould.Stop;
        else
            settings.State = ServiceShould.Run;

    }


    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        if (Settings.EnablePython)
        {
            PythonProtocol.Startup(Logger);
        }

        foreach (var job in Jobs)
            job.Start();

        Cancel = NabuLib.SetLength(
            DefinedAdaptors.Length,
            Array.Empty<CancellationTokenSource>(),
            () => CancellationTokenSource.CreateLinkedTokenSource(stopping)
        ).ToArray();

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

            //int[] fails = new int[DefinedAdaptors.Length];
            //bool started = false;
            Logger.Write($"Defined Adaptors: {DefinedAdaptors.Length}");

            // Until the host tells us to stop
            while (stopping.IsCancellationRequested is false)
            {
                for (int index = 0; index < DefinedAdaptors.Length; index++)
                {
                    var settings = DefinedAdaptors[index];
                    
                    if (settings.State is ServiceShould.Stop)
                    {
                        if (services[index] != Task.CompletedTask)
                        {
                            Cancel[index].Cancel();
                            Cancel[index].Dispose();
                            Cancel[index] = CancellationTokenSource.CreateLinkedTokenSource(stopping);
                            services[index] = Task.CompletedTask;
                        }
                        continue;
                    }
                    
                    if (settings.Enabled == false) continue;

                    // Is this service stopped?
                    if (services[index].IsCompleted && settings.State is ServiceShould.Run && settings.Enabled)
                    {
                        // If so, restart it
                        var token = Cancel[index].Token;
                        services[index] = settings.Type switch
                        {
                            AdaptorType.Serial 
                                => Task.Run(() => SerialAdaptor.Start(ServiceProvider, (SerialAdaptorSettings)settings, token)),
                            AdaptorType.TCP when settings is TCPAdaptorSettings tcp && tcp.Client 
                                => Task.Run(() => TCPClientAdaptor.Start(ServiceProvider, settings, token)),
                            AdaptorType.TCP 
                                => Task.Run(() => TCPAdaptor.Start(ServiceProvider, settings, token)),
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
        services.AddSingleton<INabuNetwork, NabuNetwork>()
                .AddTransient<ClassicNabuProtocol>()
                .AddTransient(typeof(IConsole<>), typeof(MicrosoftExtensionsLoggingConsole<>))
                .AddSingleton<FileCache>();

        if (settings.EnablePython)
        {
            var pythonLib = string.Empty;
            if (OperatingSystem.IsWindows())
            {
                pythonLib = "python311.dll";


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
                        var pluginProtocols = settings.Protocols.Where(p => p.Type.ToLower() == ProtocolPluginTypes.Python.ToLower());
                        foreach (var proto in pluginProtocols)
                        {
                            services.AddTransient<IProtocol>(
                                sp => new PythonProtocol(sp.GetService<IConsole<PythonProtocol>>()!, proto)
                            );
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Cannot find Python, disabling.");
                    settings.EnablePython = false;
                }
            }
            else if (OperatingSystem.IsLinux()) pythonLib = "libpython3.11.so";
            else if (OperatingSystem.IsMacOS()) pythonLib = "libpython3.11.dylib";
        }

        if (settings.EnableJavaScript)
        {
            var pluginProtocols = settings.Protocols.Where(p => p.Type.ToLower() == ProtocolPluginTypes.JavaScript.ToLower());
            foreach (var proto in pluginProtocols)
            {
                services.AddTransient<IProtocol>(
                    sp => new JavaScriptProtocol(sp.GetService<IConsole<JavaScriptProtocol>>()!, proto)
                );
            }
        }

        services.AddTransient<IProtocol, NHACPProtocol>()
                .AddTransient<IProtocol, NHACPV01Protocol>()
                .AddTransient<IProtocol, RetroNetTelnetProtocol>()
                .AddTransient<IProtocol, RetroNetProtocol>()
                .AddSingleton<ISimulation, Simulation>()
                .AddSingleton<IJob, RefreshSourcesJob>()
                .AddHostedService<Simulation>();

        return services;
    }
}