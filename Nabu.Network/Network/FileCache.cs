using Nabu.Services;
using System.Text;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ComponentModel;

namespace Nabu.Network;

public class FileCache 
{
    public static TimeSpan TTL { get; set; } = TimeSpan.FromMinutes(30);
    IConsole<FileCache> Logger { get; }
    
    readonly ConcurrentDictionary<string, DateTime> _cacheTime = new();
    readonly ConcurrentDictionary<string, byte[]> _cache = new();
    
    readonly Settings Settings;

    public FileCache(
        IConsole<FileCache> logger,
        Settings settings
    ) {
        Settings = settings;
        Logger = logger;
    }

    public void CacheFile(string path, byte[] content, bool write = true)
    {
        if (Settings.EnableLocalFileCache is false)
            return;
        Task.Run(async () =>
        {
            if (write) await File.WriteAllBytesAsync(path, content);
            _cache[path] = content;
            _cacheTime[path] = DateTime.Now;
        });
    }

    public void UnCache(string path)
    {
        if (Settings.EnableLocalFileCache is false)
            return;
        
        if (!_cache.ContainsKey(path)) return;

        Task.Run(() =>
        {
            _cache.Remove(path, out _);
            _cacheTime.Remove(path, out _);
        });
    }

    public async Task<byte[]> GetFile(string path)
    {
        if (Settings.EnableLocalFileCache is false)
            return await File.ReadAllBytesAsync(path);

        var content = Array.Empty<byte>();
        if (File.Exists(path) is false) return Array.Empty<byte>();

        if (_cacheTime.TryGetValue(path, out var cacheTime) &&
            File.GetLastWriteTime(path) > cacheTime
        ) {
            content = await File.ReadAllBytesAsync(path);
            CacheFile(path, content, false);
            return content;
        }

        if (_cache.TryGetValue(path, out content))
            return content;
        
        content = await File.ReadAllBytesAsync(path);
        CacheFile(path, content, false);
        return content;
    }

    public void CacheString(string path, string content, bool write = true, Encoding? encoding = null)
    {
        if (Settings.EnableLocalFileCache is false)
            return;
        encoding ??= Encoding.UTF8;
        Task.Run(async () =>
        {
            if (write) await File.WriteAllTextAsync(path, content);
            _cache[path] = encoding.GetBytes(content);
            _cacheTime[path] = DateTime.Now;
        });
    }

    public async Task<string> GetString(string path)
    {
        if (Settings.EnableLocalFileCache is false)
            return await File.ReadAllTextAsync(path);

        var content = string.Empty;

        if (File.Exists(path) is false) return string.Empty;

        if (_cacheTime.TryGetValue(path, out var cacheTime) &&
            File.GetLastWriteTime(path) > cacheTime
        ){
            content = await File.ReadAllTextAsync(path);
            CacheString(path, content, false);
            return content;
        }

        if (_cache.TryGetValue(path, out var bytes))
            return Encoding.UTF8.GetString(bytes);
            
        content = await File.ReadAllTextAsync(path);
        CacheString(path, content, false);
        return content;
    }

    public DateTime LastChange(string path)
    {
        
        var lastWrite = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;
        if (Settings.EnableLocalFileCache is false)
            return lastWrite;

        var cached = _cacheTime.TryGetValue(path, out var cacheTime);
        return cached switch
        {
            true when cacheTime > lastWrite => cacheTime,
            true => lastWrite,
            false when lastWrite > DateTime.MinValue => lastWrite,
            _ => DateTime.MinValue
        };
    }


}
