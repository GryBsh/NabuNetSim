//using Microsoft.EntityFrameworkCore;
using Blazorise;
using DynamicData;
using Microsoft.Extensions.Logging;
using Nabu.Models;
using Nabu.NetSim.UI.ViewModels;
using Nabu.Network;
using Nabu.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.Services;

public class LogService : DisposableBase
{
    //public ILog<LogService> Log { get; }
    public IRepository<LogEntry> Repository { get; }
    public List<LogEntry> Entries { get; private set; }
    public int Count {
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
        //Disposables.AddInterval(
        //    TimeSpan.FromSeconds(10), 
        //    _ => { if (RefreshMode is RefreshMode.MemoryCache) Refresh(); }
        //);
    }
    public RefreshMode RefreshMode { get; set; } = RefreshMode.Database;

    DateTime LastUpdate { get; set; } = DateTime.MinValue;
    public void Refresh(bool force = false)
    {
        if (RefreshMode is not RefreshMode.MemoryCache && !force) 
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
