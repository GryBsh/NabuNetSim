using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nabu.Adaptor;
using Nabu.Services;

//using LiteDb.Extensions.Caching;

namespace Nabu;

public record SimulationTask
{
    public SimulationTask(CancellationToken stopping)
    {
        Cancellation = CancellationTokenSource.CreateLinkedTokenSource(stopping);
    }
    public AdaptorSettings Settings { get; set; } = new NullAdaptorSettings();
    public Task Task { get; set; } = Task.CompletedTask;
    public CancellationTokenSource Cancellation { get; set; }
}

public class Simulation : BackgroundService, ISimulation
{
    private readonly IEnumerable<IJob> Jobs;
    private readonly ILog Logger;
    private readonly IServiceProvider ServiceProvider;

    public Simulation(
        ILog<Simulation> logger,
        Settings settings,
        IServiceProvider serviceProvider,
        IEnumerable<IJob> jobs
    )
    {
        Logger = logger;
        Settings = settings;

        ServiceProvider = serviceProvider;
        Jobs = jobs;
    }

    private List<AdaptorSettings>? DefinedAdaptors { get; set; }
    private Settings Settings { get; }

    private List<SimulationTask> Tasks { get; set; } = new();

    public void ToggleAdaptor(AdaptorSettings settings)
    {
        if (settings.State is ServiceShould.Run)
            settings.State = ServiceShould.Stop;
        else
            settings.State = ServiceShould.Run;
    }

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        foreach (var job in Jobs)
            job.Start();

        if (DefinedAdaptors is null)
            RefreshAdaptors(stopping);

        await Task.Run(() =>
        {
            Logger.Write($"Defined Adaptors: {DefinedAdaptors?.Count ?? 0}");

            while (stopping.IsCancellationRequested is false)
            {
                RefreshAdaptors(stopping);

                foreach (var task in Tasks.Where(t => t.Settings.Enabled))
                {
                    if (task.Settings.State is ServiceShould.Stop or ServiceShould.Restart && !task.Task.IsCompleted)
                    {
                        task.Cancellation?.Cancel();
                        task.Cancellation?.Dispose();
                        task.Cancellation = CancellationTokenSource.CreateLinkedTokenSource(stopping);
                        task.Task = Task.CompletedTask;
                        task.Settings.Running = false;
                        if (task.Settings.State is not ServiceShould.Restart)
                            continue;
                        task.Settings.State = ServiceShould.Run;
                    }

                    if (task.Task.IsCompleted && task.Settings.State is ServiceShould.Run)
                    {
                        task.Task = task.Settings.Type switch
                        {
                            AdaptorType.Serial
                                => Task.Run(() => SerialAdaptor.Start(ServiceProvider.CreateScope().ServiceProvider, (SerialAdaptorSettings)task.Settings, task.Cancellation.Token)),
                            AdaptorType.TCP when task.Settings is TCPAdaptorSettings tcp && tcp.Client
                                => Task.Run(() => TCPClientAdaptor.Start(ServiceProvider.CreateScope().ServiceProvider, (TCPAdaptorSettings)task.Settings, task.Cancellation.Token)),
                            AdaptorType.TCP
                                => Task.Run(() => TCPAdaptor.Start(ServiceProvider.CreateScope().ServiceProvider, (TCPAdaptorSettings)task.Settings, task.Cancellation.Token)),
                            _ => throw new NotImplementedException()
                        };

                        task.Settings.Running = true;
                    }
                }
                Thread.Sleep(10); // Lazy Wait, we don't care how long it takes to resume
            }
        }, stopping);
    }

    private void RefreshAdaptors(CancellationToken stopping)
    {
        DefinedAdaptors =
            Enumerable.Concat<AdaptorSettings>(
                Settings.Adaptors.Serial,
                Settings.Adaptors.TCP
            ).ToList();

        var missing = DefinedAdaptors.Where(a => !Tasks.Any(t => t.Settings.Port == a.Port)); //Where we have an adaptor for the port, but not a task running the emulator.
        var orphans = Tasks.Where(t => !DefinedAdaptors.Any(a => a.Port == t.Settings.Port)); //Where we have a task for an emulator, but no adaptor definition, likely because it was removed.

        Tasks.AddRange(missing.Select(
            adaptor => new SimulationTask(stopping)
            {
                Settings = adaptor,
                Task = Task.CompletedTask
            }
        ));

        foreach (var orphan in orphans)
        {
            Logger.Write($"Shutting down adaptor {orphan.Settings.Port}");
            orphan.Cancellation?.Cancel();
            Tasks.Remove(orphan);
        }
    }
}