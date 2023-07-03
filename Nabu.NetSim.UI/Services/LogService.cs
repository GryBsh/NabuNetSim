//using Microsoft.EntityFrameworkCore;
using Nabu.Models;
using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.Services;

public class LogService : DisposableBase, ILogService
{
    //public ILog<LogService> Log { get; }
    public IRepository<LogEntry> Repository { get; }

    public List<LogEntry> Entries { get; private set; }

    public int Count
    {
        get
        {
            return Repository.Count();
        }
    }

    public LogService(
        IRepository<LogEntry> repository
    )
    {
        Repository = repository;
        Entries = new List<LogEntry>();
    }

    public RefreshMode RefreshMode { get; set; } = RefreshMode.None;

    private DateTime LastUpdate { get; set; } = DateTime.MinValue;

    public void Refresh(bool force = false)
    {
        if (RefreshMode is RefreshMode.None && !force)
            return;

        var now = DateTime.Now;
        var entries = Repository.Select(e => e.Timestamp > LastUpdate)
                                .OrderByDescending(e => e.Timestamp);
        Entries.InsertRange(0, entries);
        LastUpdate = now;
    }

    public IEnumerable<LogEntry> GetPage(int page, int pageSize)
    {
        var pageSkip = (page - 1) * pageSize;
        if (RefreshMode is RefreshMode.MemoryCache)
        {
            return Entries.Skip(pageSkip).Take(pageSize);
        }
        return Repository.SelectDescending(e => e.Timestamp, pageSkip, pageSize);
    }

    public void Insert(LogEntry entry)
    {
        Repository.Insert(entry);
    }

    public void BulkInsert(params LogEntry[] entries)
        => Repository.BulkInsert(entries);
}