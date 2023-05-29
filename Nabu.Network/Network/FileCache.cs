using Nabu.Services;
using System.Text;
using System.Reactive;

namespace Nabu.Network;



public class FileCache : IFileCache
{
    public static TimeSpan TTL { get; set; } = TimeSpan.FromMinutes(30);
    ILog<FileCache> Logger { get; }

    //readonly ConcurrentDictionary<string, DateTime> _cacheTime = new();
    //readonly ConcurrentDictionary<string, Memory<byte>> _cache = new();

    MemoryCache<Memory<byte>> Cache { get; } = new();

    readonly Settings Settings;

    public FileCache(
        ILog<FileCache> logger,
        Settings settings
    )
    {
        Settings = settings;
        Logger = logger;
    }

    public void CacheFile(string path, Memory<byte> content, bool write = true)
    {
        if (Settings.EnableLocalFileCache is false)
            return;
        Task.Run(async () =>
        {
            Cache.Cache(path, content);
            if (write) await File.WriteAllBytesAsync(path, content.ToArray());
        });
    }

    public void UnCache(string path)
    {
        if (Settings.EnableLocalFileCache is false)
            return;

        //if (!_cache.ContainsKey(path)) 
        //    return;

        Task.Run(() =>
        {
            Cache.UnCache(path);
        });
    }

    public async Task<Memory<byte>> GetFile(string path)
    {
        if (Settings.EnableLocalFileCache is false)
            return await File.ReadAllBytesAsync(path);

        if (File.Exists(path) is false)
            return Array.Empty<byte>(); ;

        return await Cache.CacheOrUpdate(
            path,
            async (timestamp, old) =>
            {
                if (File.GetLastWriteTime(path) > timestamp)
                {
                    return await File.ReadAllBytesAsync(path);
                }
                return old;
            }
        );
    }

    public void CacheString(string path, string content, bool write = true, Encoding? encoding = null)
    {
        if (Settings.EnableLocalFileCache is false)
            return;

        encoding ??= Encoding.UTF8;
        Task.Run(async () =>
        {
            Cache.Cache(path, encoding.GetBytes(content));
            if (write) await File.WriteAllTextAsync(path, content);
        });
    }

    public async Task<string> GetString(string path)
    {
        if (Settings.EnableLocalFileCache is false)
            return await File.ReadAllTextAsync(path);

        if (File.Exists(path) is false)
            return string.Empty;

        return Encoding.UTF8.GetString(
            (await GetFile(path)).ToArray()
        );
    }

    public DateTime LastChange(string path)
    {
        var lastWrite = File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.MinValue;
        if (Settings.EnableLocalFileCache is false)
            return lastWrite;

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


}
