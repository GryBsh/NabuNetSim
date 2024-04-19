//using Microsoft.EntityFrameworkCore;
using Blazorise;
using Gry;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Settings;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.Services;

public class LogService : DisposableBase, ILogTailingService
{
    readonly SemaphoreSlim _lock = new(1, 1);
    public ObservableCollection<string> LogEntries { get; set; } = [];   

    GlobalSettings Settings { get; }
    async Task<long> ReadAll(string path, long position, CancellationToken cancellation)    {        using var log = new FileStream(
            path,            FileMode.Open,            FileAccess.Read,            FileShare.ReadWrite
        );        using var rdr = new StreamReader(log);        rdr.BaseStream.Seek(position, SeekOrigin.Begin);        var text = (await rdr.ReadToEndAsync(cancellation)).Split('\n');        position = rdr.BaseStream.Position;        foreach (var line in text)        {            await _lock.WaitAsync(cancellation);            LogEntries.Insert(0, line);            _lock.Release();        }        return position;    }    async Task<long> Read(string path, long position, CancellationToken cancellation)    {        using var log = new FileStream(
            path,            FileMode.Open,            FileAccess.Read,            FileShare.ReadWrite
        );        using var rdr = new StreamReader(log);        string? text = null;        do        {            rdr.BaseStream.Seek(position, SeekOrigin.Begin);            text = await rdr.ReadLineAsync(cancellation);            position = rdr.BaseStream.Position;            if (text is not null)            {                await _lock.WaitAsync(cancellation);                LogEntries.Insert(0, text);                _lock.Release();            }        } while (text is not null);        return position;    }
    public async void Tail(string path, CancellationToken cancellation)
    {
        LogEntries.Clear();        var dir = new DirectoryInfo(path);        if (!dir.Exists)        {            dir.Create();        }
        var logs = dir.GetFiles("*.log").OrderByDescending(f => f.CreationTime);

        var current = logs.FirstOrDefault()?.FullName ?? Path.Combine(path, "current.log");
        var last = logs.Skip(1).Reverse();


        foreach (var l in last)        {            var loaded = false;            while (!loaded)                try {                     foreach (var line in File.ReadLines(l.FullName))
                        LogEntries.Insert(0, line);                    loaded = true;                } catch {}        }        if (LogEntries.Count > Settings.MaxLogEntries)        {            DropLast(Settings.MaxLogEntries);        }        long position = 0;                while (position == 0)            try            {                position = await ReadAll(current, 0, cancellation);                await Task.Delay(1000, cancellation);            } catch { }                while (!cancellation.IsCancellationRequested) {
            try
            {                position = await Read(current, position, cancellation);                await Task.Delay(1000, cancellation);            }
            catch { }
        }
           
    }    public LogService(GlobalSettings settings, LocationService location)
    {
        Settings = settings;
        var cancellation = new CancellationDisposable();                Tail(location.Logs, cancellation.Token);        
        Disposables.Add(cancellation);

    }

    public int Count() => LogEntries.Count();
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
                             .ToArray();
    }

    public async void DropLast(int entries)
    {
        await _lock.WaitAsync();
        for (int i = (LogEntries.Count - entries); i < LogEntries.Count(); i++)        {            LogEntries.RemoveAt(i);        }
        _lock.Release();
    }

    public async void DropBefore(DateTime timestamp)
    {
       
    }   
}   
