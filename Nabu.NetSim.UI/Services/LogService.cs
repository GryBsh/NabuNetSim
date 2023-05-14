//using Microsoft.EntityFrameworkCore;
using Nabu.NetSim.UI.Models;
using Nabu.Services;

namespace Nabu.NetSim.UI.Services
{
    public class LogService
    {
        public IRepository<LogEntry> Repository { get; }
        public List<LogEntry> Entries { get; } = new List<LogEntry>();
        public int Count => Repository.Count();
        public LogService(
            IRepository<LogEntry> repository
        )
        {
            Repository = repository;
            
            Refresh(true);

        }
        public bool Update { get; set; } = false;

        DateTime LastUpdate { get; set; } = DateTime.MinValue;
        public void Refresh(bool force = false)
        {
            if (!Update && !force) return;

            Entries.AddRange(Repository.Select(e => e.Timestamp > LastUpdate));
            LastUpdate = DateTime.Now;

        }

        public void Insert(LogEntry entry) => Repository.Insert(entry);
    }
}
