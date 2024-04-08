using System.Collections.Concurrent;

namespace Gry.Caching;

public delegate Task<T> DeferredValue<T>();

public record UpdateCheck<T>(bool Result, DeferredValue<T>? Value)
{
    public static Task<UpdateCheck<T>> Create(bool update, DeferredValue<T>? value)
    {
        return Task.FromResult<UpdateCheck<T>>(new(update, value));
    }
}

public class MemoryCache<T>
{
    public IEnumerable<string> Keys => Cached.Keys;
    private ConcurrentDictionary<string, CacheItem<T>> Cached { get; } = new();

    public T? this[string key] => Get(key);

    public CacheItem<T>? Cache(string key, T value, DateTime? timestamp = null)
    {
        timestamp ??= DateTime.Now;
        var emptyValue = value?.Equals(default) is true;
        //var cached = Cached.TryGetValue(key, out var old);
        if (emptyValue)
            return null;
        return Cached[key] = new CacheItem<T>(timestamp.Value, value);
    }

    public async Task<T?> GetOrUpdate(string key, Func<DateTime, T?, Task<UpdateCheck<T>>> memory, DateTime? cacheTime = null)
    {
        var timestamp = DateTime.MinValue;
        T? value = default;

        if (Cached.TryGetValue(key, out var item))
            (timestamp, value) = item;

        var (result, newValue) = await memory.Invoke(timestamp, value);
        if (result is true && newValue is not null)
        {
            var newItem = Cache(key, await newValue.Invoke(), cacheTime);
            return newItem is not null ? newItem.Value : value;
        }
        return value;
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

    public CacheItem<T>? Remove(string key)
    {
        Cached.TryRemove(key, out var old);
        return old;
    }
}