//using Microsoft.EntityFrameworkCore;
using Blazorise;
using Gry;
using Microsoft.Extensions.Logging;
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
        LogEntries.Clear();
        var logs = new DirectoryInfo(path).GetFiles("*.log").OrderByDescending(f => f.CreationTime);

        var current = logs.FirstOrDefault();
        var last = logs.Skip(1).Reverse();

        if (current is null)
            return;
        foreach (var l in last)        {            var loaded = false;            while (!loaded)                try {                     foreach (var line in File.ReadLines(l.FullName))
                        LogEntries.Insert(0, line);                    loaded = true;                } catch {}        }        if (LogEntries.Count > Settings.MaxLogEntries)        {            DropLast(Settings.MaxLogEntries);        }        long position = await ReadAll(current.FullName, 0, cancellation);                while (!cancellation.IsCancellationRequested) {
            try
            {                position = await Read(current.FullName, position, cancellation);                await Task.Delay(1000, cancellation);            }
            catch { }            //(Exception ex) when (ex is ArgumentOutOfRangeException or IOException)             //{            //    position = 0;            //}   
        }
           
    } 

    public LogService(GlobalSettings settings)
    {
        Settings = settings;
        var cancellation = new CancellationDisposable();                Tail(Path.Combine(AppContext.BaseDirectory, "logs"), cancellation.Token);        /*        var path = Path.Combine(AppContext.BaseDirectory, "logs");        var logs = new DirectoryInfo(path).GetFiles("*.log").OrderByDescending(f => f.CreationTime);

        //var current = logs.FirstOrDefault();
        var last = logs.Skip(1).Reverse();

        foreach (var log in last)        {            var loaded = false;            while (!loaded)                try                {                    foreach (var line in File.ReadLines(log.FullName))
                        LogEntries.Insert(0, (DateTime.Now, line));                    loaded = true;                }                catch { }        }        if (LogEntries.Count > Settings.MaxLogEntries)        {            DropLast(Settings.MaxLogEntries);        } */
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
