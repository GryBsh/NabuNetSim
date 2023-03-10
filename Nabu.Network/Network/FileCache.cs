using Nabu.Services;
using System.Text;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Nabu.Network;

public class FileCache 
{
    public static TimeSpan TTL { get; set; } = TimeSpan.FromMinutes(30);
    IConsole<FileCache> Logger { get; }
    
    //readonly IMultiLevelCache cache;
    //readonly Func<TimeSpan, MemoryCacheEntryOptions> memCacheOptions = ttl => new MemoryCacheEntryOptions() { SlidingExpiration = ttl };
    //readonly Func<TimeSpan, DistributedCacheEntryOptions> longCacheOptions = ttl => new DistributedCacheEntryOptions() { SlidingExpiration = ttl };
    readonly ConcurrentDictionary<string, DateTime> _cacheTime = new();
    readonly ConcurrentDictionary<string, byte[]> _cache = new();
    
    readonly Settings Settings;

    public FileCache(
        IConsole<FileCache> logger ,
        Settings settings
        /*IMultiLevelCache cache*/
    ) {
        Settings = settings;
        
        //this.cache = cache;
        Logger = logger;
        //Observable.Interval(TimeSpan.FromMinutes(1), ThreadPoolScheduler.Instance)
            //.Subscribe(_ => ExpireCache());
    }

    public async void CacheFile(string path, byte[] content, bool write = true)
    {        
        if (write) await File.WriteAllBytesAsync(path, content);
        //await cache.SetAsync(path, content, memCacheOptions(TTL), longCacheOptions(TTL * 2));
 
        _cache[path] = content;
        _cacheTime[path] = DateTime.Now;

    }

    /*
    void ExpireCache()
    {
        var now = DateTime.Now;
        foreach (var (path, time) in _cacheTime)
        {
            if (now - time > TTL)
            {
                _cacheTime.TryRemove(path, out _);
                _cache.TryRemove(path, out _);
            }
        }
    }
    */

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
            //await cache.SetAsync(path, content, memCacheOptions(TTL), longCacheOptions(TTL * 2));
            CacheFile(path, content, false);
            return content;
        }

        if (_cache.TryGetValue(path, out content))
            return content;
        
        content = await File.ReadAllBytesAsync(path);
        CacheFile(path, content, false);
        return content;
            /*await cache.GetOrSetAsync(
                path, 
                async cancel => {    
                    _cacheTime[path] = DateTime.Now;
                    return await File.ReadAllBytesAsync(path, cancel);
                }, 
                memCacheOptions(TTL), 
                longCacheOptions(TTL * 2)
            ) ?? Array.Empty<byte>();*/
    }

    public void CacheString(string path, string content, bool write = true)
    {
        Task.Run(async () =>
        {
            if (write) await File.WriteAllTextAsync(path, content);

            //await cache.SetAsync(path, content, memCacheOptions(TTL), longCacheOptions(TTL * 2));
            _cache[path] = Encoding.UTF8.GetBytes(content);
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
            //await cache.SetAsync(path, content, memCacheOptions(TTL), longCacheOptions(TTL * 2));
            CacheString(path, content, false);
            return content;
        }

        if (_cache.TryGetValue(path, out var bytes))
            return Encoding.UTF8.GetString(bytes);
            
        content = await File.ReadAllTextAsync(path);
        CacheString(path, content, false);
        return content;

            /*await cache.GetOrSetAsync(
                path,
                async cancel =>
                {
                    var text = await File.ReadAllTextAsync(path, cancel);
                    _cacheTime[path] = DateTime.Now;
                    return text;
                },
                memCacheOptions(TTL),
                longCacheOptions(TTL * 2)
            ) ?? string.Empty;*/
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
