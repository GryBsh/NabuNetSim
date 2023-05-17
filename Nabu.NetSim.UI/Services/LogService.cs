//using Microsoft.EntityFrameworkCore;
using Blazorise;
using DynamicData;
using Nabu.Models;
using Nabu.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.Services
{
    public class LogService
    {
        public IRepository<LogEntry> Repository { get; }
        public List<LogEntry> Entries { get; private set; }
       
        public int Count => Repository.Count();
        public LogService(
            IRepository<LogEntry> repository
        )
        {
            Repository = repository;
            Entries = new List<LogEntry>();
            Refresh(true);
            Observable.Interval(TimeSpan.FromSeconds(10))
                      .Subscribe(_ => Refresh());

        }
        public bool Update { get; set; } = false;

        DateTime LastUpdate { get; set; } = DateTime.MinValue;
        public void Refresh(bool force = false)
        {
            LocalRefresh(force);
        }
        public void LocalRefresh(bool force = false)
        {
            if (!Update && !force) return;
            var now = DateTime.Now;
            var entries = Repository.Select(e => e.Timestamp > LastUpdate)
                                    .OrderByDescending(e => e.Timestamp);
            Entries.InsertRange(0, entries);
            LastUpdate = now;
        }

        public void Insert(LogEntry entry) => Repository.Insert(entry);
    }
}
