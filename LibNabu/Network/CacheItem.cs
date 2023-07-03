namespace Nabu.Network;

public record CacheItem<T>(DateTime Timestamp, T Value);