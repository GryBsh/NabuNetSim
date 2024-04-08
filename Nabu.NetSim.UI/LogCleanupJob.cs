using Nabu.Logs;
using Nabu.NetSim.UI.Services;
using Nabu.Settings;
using Gry;
using Gry.Jobs;

namespace Nabu.NetSim.UI;

public class LogTailCleanupJob(ILogger<LogTailCleanupJob> logger, GlobalSettings settings, ILogTailingService logService) : Job
{
    public ILogTailingService LogService { get; set; } = logService;
    protected ILog Logger { get; } = logger;
    protected GlobalSettings Settings { get; } = settings;

    
    private void Cleanup()
    {
    
        //var cutoff = DateTime.Now.AddDays(-Settings.MaxLogEntryDatabaseAgeDays);
        
        //LogService.DropBefore(cutoff);
        //Logger.Write($"Entries before {cutoff} removed");
        
        var bad = LogService.Count() - Settings.MaxLogEntries;
        if (bad > 0)
        {
            LogService.DropLast(bad);
            Logger.Write($"{bad} entries over limit of {Settings.MaxLogEntries} removed");
        }
        
    }
    

    

    protected override void OnSchedule()
    {
        Cleanup();

        Disposables.AddInterval(
            TimeSpan.FromMinutes(Settings.LogCleanupIntervalMinutes),
            _ => Cleanup()
        );
    }
}

/*
public class LogCleanupJob(ILogger<LogCleanupJob> logger, GlobalSettings settings, ILogService logService) : Job
{
    public ILogService LogService { get; set; } = logService;
    protected ILog Logger { get; } = logger;
    protected GlobalSettings Settings { get; } = settings;

    private void Cleanup()
    {
        var repository = LogService.Repository;
        var cutoff = DateTime.Now.AddDays(-Settings.MaxLogEntryDatabaseAgeDays);
        var pendingDelete = repository.Count(e => e.Timestamp < cutoff);
        var hasPending = pendingDelete > 0;

        
        if (hasPending)
        {
            repository.Delete(e => e.Timestamp < cutoff);
        }
        if (hasPending)
            Logger.Write($"{pendingDelete} entries from before {cutoff} removed");

        var good = repository.SelectDescending(e => e.Timestamp, 0, Settings.MaxLogEntries).Select(e => e.Id);
        if (good.Count() >= Settings.MaxLogEntries)
        {
            var bad = repository.Select(e => !good.Contains(e.Id)).Select(e => e.Id);

            repository.Delete(e => bad.Contains(e.Id));
            if (bad.Any())
                Logger.Write($"{bad.Count()} entries over limit of {Settings.MaxLogEntries} removed");
        }

        //GC.Collect();
    }

    private void Maintenance()
    {
        Logger.Write("Database Maintenance");
        LogService.Repository.RunMaintenance();
    }

    protected override void OnSchedule()
    {
        //Cleanup();

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
*/