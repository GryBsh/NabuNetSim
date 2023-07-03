using Nabu.Services;
using System.Text;

namespace Nabu.Network;

public class FileCache : IFileCache
{
    public FileCache(
        ILog<FileCache> logger
    )
    {
        Logger = logger;
    }

    private MemoryCache<Memory<byte>> Cache { get; } = new();
    private ILog<FileCache> Logger { get; }

    public async Task CacheFile(string path, Memory<byte> content, bool write = true)
    {
        Cache.Cache(path, content);
        if (write) await File.WriteAllBytesAsync(path, content.ToArray());
    }

    public async Task CacheString(string path, string content, bool write = true, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        Cache.Cache(path, encoding.GetBytes(content));
        if (write) await File.WriteAllTextAsync(path, content);
    }

    public async Task<Memory<byte>> GetBytes(string path)
    {
        if (File.Exists(path) is false)
            return Array.Empty<byte>(); ;

        var lastWrite = File.GetLastWriteTime(path);

        return await Cache.CacheOrUpdate(
            path,
            async (timestamp, old) =>
            {
                if (lastWrite > timestamp)
                {
                    return await File.ReadAllBytesAsync(path);
                }
                return old;
            },
            lastWrite
        );
    }

    public async Task<string> GetString(string path)
    {
        if (File.Exists(path) is false)
            return string.Empty;

        return Encoding.UTF8.GetString(
            (await GetBytes(path)).ToArray()
        );
    }

    public DateTime LastChange(string path)
    {
        var lastWrite = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;

        var cacheTime = Cache.LastCached(path);
        var cached = cacheTime > DateTime.MinValue;
        return cached switch
        {
            true when cacheTime > lastWrite => cacheTime,
            true => lastWrite,
            false when lastWrite > DateTime.MinValue => lastWrite,
            _ => DateTime.MinValue
        };
    }

    public void Uncache(string path)
    {
        Cache.UnCache(path);
    }

    public void UncachePath(string path)
    {
        var removed = Cache.Keys.Where(k => k.StartsWith(path));
        foreach (var key in removed)
            Cache.UnCache(key);
    }
}