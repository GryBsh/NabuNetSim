using Nabu.NetSim.UI.Services;
using Nabu.Services;

namespace Nabu.NetSim.UI;

public class LogCleanupJob : Job
{
    public ILogService LogService { get; set; }

    public LogCleanupJob(ILog<LogCleanupJob> logger, Settings settings, ILogService logService) : base(logger, settings)
    {
        LogService = logService;
    }

    private void Cleanup()
    {
        //using var scope = Scope.CreateScope();
        var repository = LogService.Repository;

        var cutoff = DateTime.Now.AddDays(-Settings.MaxLogEntryDatabaseAgeDays);
        //Repository.Collection<LogEntry>().EnsureIndex(e => e.Timestamp);
        var pendingDelete = repository.Count(e => e.Timestamp < cutoff);
        var hasPending = pendingDelete > 0;

        Logger.Write((hasPending ? "Removing" : "No") + $" entries before {cutoff}" + (hasPending ? $": {pendingDelete}" : string.Empty));
        if (hasPending)
        {
            repository.Delete(e => e.Timestamp < cutoff);
        }

        //GC.Collect();
    }

    private void Maintenance()
    {
        Logger.Write("Database Maintenance");
        LogService.Repository.RunMaintenance();
    }

    public override void Start()
    {
        Cleanup();

        Disposables.AddInterval(
            TimeSpan.FromMinutes(Settings.LogCleanupIntervalMinutes),
            _ => Cleanup()
        );
        Disposables.AddInterval(
            TimeSpan.FromHours(1),
            _ => Maintenance()
        );
    }
}