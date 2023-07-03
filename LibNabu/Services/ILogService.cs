//using Microsoft.EntityFrameworkCore;
using Nabu.Models;

namespace Nabu.Services
{
    public interface ILogService
    {
        int Count { get; }
        List<LogEntry> Entries { get; }
        RefreshMode RefreshMode { get; set; }
        IRepository<LogEntry> Repository { get; }

        void BulkInsert(params LogEntry[] entries);

        IEnumerable<LogEntry> GetPage(int page, int pageSize);

        void Insert(LogEntry entry);

        void Refresh(bool force = false);
    }
}