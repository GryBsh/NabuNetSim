using System.Collections.Concurrent;

namespace Nabu.Network;

public class MemoryCache<T>
{
    public IEnumerable<string> Keys => Cached.Keys;
    private ConcurrentDictionary<string, CacheItem<T>> Cached { get; } = new();

    public T? this[string key] => Get(key);

    public CacheItem<T>? Cache(string key, T value, DateTime? timestamp = null)
    {
        timestamp ??= DateTime.Now;
        var emptyValue = value?.Equals(default) is true;
        var cached = Cached.TryGetValue(key, out var old);
        if (cached && old is not null && old.Value?.Equals(value) is true)
            return old;
        else if (emptyValue)
            return null;
        return Cached[key] = new CacheItem<T>(timestamp.Value, value);
    }

    public async Task<T?> CacheOrUpdate(string key, Func<DateTime, T?, Task<T>> memory, DateTime? cacheTime = null)
    {
        DateTime timestamp = DateTime.MinValue;
        T? value = default;

        if (Cached.TryGetValue(key, out var item))
            (timestamp, value) = item;

        var newValue = await memory.Invoke(timestamp, value);
        if (newValue?.Equals(value) is false)
        {
            var newItem = Cache(key, newValue, cacheTime);
            return newItem is not null ? newItem.Value : default;
        }
        return item is not null ? item.Value : default;
    }

    public T? Get(string key)
    {
        var found = Cached.TryGetValue(key, out var item);
        return found ? item!.Value : default;
    }

    public DateTime LastCached(string key)
    {
        if (!Cached.TryGetValue(key, out var value))
            return DateTime.MinValue;
        return value!.Timestamp;
    }

    public CacheItem<T>? UnCache(string key)
    {
        Cached.TryRemove(key, out var old);
        return old;
    }
}