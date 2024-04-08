namespace Gry.Caching;

public record CacheItem<T>(DateTime Timestamp, T Value);