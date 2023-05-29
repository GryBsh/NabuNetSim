using System.Collections.Concurrent;

namespace Nabu.Network;

public class MemoryCache<T>
{
    ConcurrentDictionary<string, CacheItem<T>> Cached { get; } = new();

    public CacheItem<T>? Cache(string key, T value)
    {
        var emptyValue = value?.Equals(default) is true;
        var cached = Cached.TryGetValue(key, out var old);
        if (cached && old is not null && old.Value?.Equals(value) is true)
            return old;
        else if (emptyValue) 
            return null;
        return Cached[key] = new CacheItem<T>(DateTime.Now, value);
    }

    public CacheItem<T>? UnCache(string key)
    {
        Cached.TryRemove(key, out var old);        
        return old;
    }

    public DateTime LastCached(string key)
    {
        if (!Cached.TryGetValue(key, out var value)) 
            return DateTime.MinValue;
        return value!.Timestamp;
    }

    public async Task<T?> CacheOrUpdate(string key, Func<DateTime, T?, Task<T>> memory)
    {
        //memory ??= (timestamp, old) => Task.FromResult(old ?? T.Empty);
        if (Cached.TryGetValue(key, out var item))
        {
            var (timestamp, value) = item;
            value = await memory.Invoke(timestamp, value);
            item = Cache(key, value);
            return item is not null ? item.Value : default;
        }

        var content = await memory.Invoke(DateTime.MinValue, default);
        item = Cache(key, content);
        return item is not null ? item.Value : default;
    }   
}
