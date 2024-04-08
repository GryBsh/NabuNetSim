//using Microsoft.EntityFrameworkCore;
using Gry;
using Nabu.Settings;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.Services;

public class LogTailingService : DisposableBase, ILogTailingService
{
    readonly SemaphoreSlim _lock = new(1, 1);
    List<(DateTime Timestamp, string Text)> LogEntries { get; set; } = [];   
    GlobalSettings Settings { get; }
    public async void Tail(string path, CancellationToken cancellation)
    {
        LogEntries.Clear();
        var logs = new DirectoryInfo(path).GetFiles("*.log").OrderByDescending(f => f.CreationTime);

        var current = logs.FirstOrDefault();
        var last = logs.Skip(1).Reverse();

        if (current is null)
            return;
        foreach (var log in last)
            foreach (var line in File.ReadLines(log.FullName).Reverse())
                LogEntries.Insert(0, (DateTime.Now, line));
        
        using var fs = new FileStream(
            current.FullName, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.ReadWrite
        );        if (LogEntries.Count > Settings.MaxLogEntries)            LogEntries.RemoveRange(Settings.MaxLogEntries, LogEntries.Count - Settings.MaxLogEntries);
        using var reader = new StreamReader(fs);
        while (!cancellation.IsCancellationRequested) {
            try
            {
                var line = await reader.ReadLineAsync(cancellation);
                if (line is not null)
                {
                    await _lock.WaitAsync(cancellation);
                    LogEntries.Insert(0, (DateTime.Now, line));    
                    _lock.Release();
                }
                else
                {
                    try
                    {
                        await Task.Delay(10, cancellation);
                    }
                    catch (TaskCanceledException) { }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
        }
           
    }

    public LogTailingService(GlobalSettings settings)
    {
        Settings = settings;
        var cancellation = new CancellationDisposable();
        Tail(Path.Combine(AppContext.BaseDirectory, "logs"), cancellation.Token);
        Disposables.Add(cancellation);

    }

    public int Count() => LogEntries.Count;
    public int PageCount(int pageSize)
    {
        var count = Count();
        return count == pageSize ? 1 : (int)Math.Floor((count / pageSize) + 1.0);
    }

    public IEnumerable<string> GetPage(int page, int pageSize)
    {
        lock (_lock) 
            return LogEntries.Skip((page-1) * pageSize)
                             .Take(pageSize)
                             .Select(l => l.Text)
                             .ToArray();
    }

    public async void DropLast(int entries)
    {
        await _lock.WaitAsync();
        LogEntries.RemoveRange(LogEntries.Count - entries, entries);
        _lock.Release();
    }

    public async void DropBefore(DateTime timestamp)
    {
        await _lock.WaitAsync();
        LogEntries.RemoveAll(e => e.Timestamp < timestamp);
        _lock.Release();
    }   
}   
