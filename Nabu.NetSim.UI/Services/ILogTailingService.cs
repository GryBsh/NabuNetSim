//using Microsoft.EntityFrameworkCore;
using Lgc;

namespace Nabu.NetSim.UI.Services;

public interface ILogTailingService : ISingletonDependency
{
    // Tail a log file in string lines from newest to oldest
    void Tail(string path, CancellationToken cancellation);
    int Count();
    int PageCount(int pageSize);
    // Get a page of log entries
    IEnumerable<string> GetPage(int page, int pageSize);
    void DropLast(int entries);
    void DropBefore(DateTime cutoff);
    //(DateTime Timestamp, string Text)[] Entries { get; }
}
