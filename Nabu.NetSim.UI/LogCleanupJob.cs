using Microsoft.Extensions.DependencyInjection;
using Nabu.NetSim.UI.Models;
using Nabu.NetSim.UI.Services;
using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI;

public class LogCleanupJob : Job
{
    
    public LogService LogService { get; set; }

    public LogCleanupJob(IConsole<LogCleanupJob> logger, Settings settings, LogService logService) : base(logger, settings)
    {
        LogService = logService;
    }

    void Cleanup()
    {
        //using var scope = Scope.CreateScope();
        var repository = LogService.Repository;
        var cutoff = DateTime.Now.AddHours(-Settings.MaxLogEntryAgeHours);
        //Repository.Collection<LogEntry>().EnsureIndex(e => e.Timestamp);
        var pendingDelete = repository.Count(e => e.Timestamp < cutoff);
        if (pendingDelete > 0)
        {
            Logger.Write($"Log Sweep: < {cutoff}: {pendingDelete}");
            repository.Delete(e => e.Timestamp < cutoff);
        }
        else
        {
            Logger.Write($"Log Sweep: < {cutoff}: 0");
        }
        //GC.Collect();
    }

    public override void Start()
    {
        Cleanup();
        Observable.Interval(
            TimeSpan.FromMinutes(Settings.LogCleanupIntervalMinutes)
        ).Subscribe(_ =>
        {
            Cleanup();
        });
    }
}
