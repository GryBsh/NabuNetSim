using Nabu.Services;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Concurrent;
using LiteDb.Extensions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Channels;
using System.Net.Mime;

namespace Nabu.Network;

public class FileCache 
{
    public static TimeSpan TTL { get; set; } = TimeSpan.FromMinutes(30);
    IConsole<FileCache> Logger { get; }
    //readonly ConcurrentDictionary<string, CachedObject> _cache = new();

    readonly IMultiLevelCache cache;
    readonly Func<TimeSpan, MemoryCacheEntryOptions> memCacheOptions = ttl => new MemoryCacheEntryOptions() { SlidingExpiration = ttl };
    readonly Func<TimeSpan, DistributedCacheEntryOptions> longCacheOptions = ttl => new DistributedCacheEntryOptions() { SlidingExpiration = ttl };
    readonly ConcurrentDictionary<string, DateTime> _cacheTime = new();
    
    public FileCache(IConsole<FileCache> logger, IMultiLevelCache cache) {
        
        this.cache = cache;

        Logger = logger;
     
    }

    public async void CacheFile(string path, byte[] content)
    {
        await File.WriteAllBytesAsync(path, content);
        await cache.SetAsync(path, content, memCacheOptions(TTL), longCacheOptions(TTL * 2));
        _cacheTime[path] = DateTime.Now;
    }

    public async Task<byte[]> GetOrCacheFile(string path, byte[]? content = null)
    {
        if (content is not null)
        {
            CacheFile(path, content);
            return content;
        }
        
        if (File.Exists(path) is false) return Array.Empty<byte>();

        if (_cacheTime.TryGetValue(path, out var cacheTime) &&
            File.GetLastWriteTime(path) > cacheTime
        ) {
            content = await File.ReadAllBytesAsync(path);
            await cache.SetAsync(path, content, memCacheOptions(TTL), longCacheOptions(TTL * 2));
            return content;
        }

        return 
            await cache.GetOrSetAsync(
                path, 
                async cancel => {    
                    _cacheTime[path] = DateTime.Now;
                    return await File.ReadAllBytesAsync(path, cancel);
                }, 
                memCacheOptions(TTL), 
                longCacheOptions(TTL * 2)
            ) ?? Array.Empty<byte>();
    }

    public async void CacheString(string path, string content)
    {
        await File.WriteAllTextAsync(path, content);
        await cache.SetAsync(path, content, memCacheOptions(TTL), longCacheOptions(TTL * 2));
        _cacheTime[path] = DateTime.Now;
    }

    public async Task<string> GetOrCacheString(string path, string? content = null)
    {
        if (content is not null)
        {
            CacheString(path, content);
            return content;
        }
        if (File.Exists(path) is false) return string.Empty;

        if (_cacheTime.TryGetValue(path, out var cacheTime) &&
            File.GetLastWriteTime(path) > cacheTime
        )
        {
            content = await File.ReadAllTextAsync(path);
            await cache.SetAsync(path, content, memCacheOptions(TTL), longCacheOptions(TTL * 2));
        }
        return
            await cache.GetOrSetAsync(
                path,
                async cancel =>
                {
                    var text = await File.ReadAllTextAsync(path, cancel);
                    _cacheTime[path] = DateTime.Now;
                    return text;
                },
                memCacheOptions(TTL),
                longCacheOptions(TTL * 2)
            ) ?? string.Empty;
    }

    public DateTime LastChange(string path)
    {
        if (_cacheTime.TryGetValue(path, out var cacheTme)) return cacheTme;
        if (File.Exists(path)) return File.GetLastWriteTime(path);
        else return DateTime.MinValue;
    }

    
}
