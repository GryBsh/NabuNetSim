using Nabu.Services;
using System.Text;

namespace Nabu.Network;

public class FileCache : Dictionary<string, (DateTime, byte[])>
{
    public TimeSpan TTL { get; set; } = TimeSpan.FromMinutes(30);
    SemaphoreSlim Lock { get; } = new(1);

    (bool, byte[]?) CacheHit(string path)
    {
        if (TryGetValue(path, out var cached) &&
            cached is var (cachedTime, content) &&
            (DateTime.Now - cachedTime) < TTL
        )   return (true, content);

        return (false, Array.Empty<byte>());
    }

    public async Task<byte[]> CacheFile(IConsole logger, string path)
    {
        
        var name = Path.GetFileName(path);
        var (cached, content) = CacheHit(path);
        if (cached)
        {
            logger.WriteVerbose($"Memory Cache Hit: {name}");
            return content!;
        }
        
        logger.WriteVerbose($"Caching content from: {name}");

        var bytes = await File.ReadAllBytesAsync(path);
        return Cache(path, bytes);
    }

    public async Task<string> CacheFileText(IConsole logger, string path)
    {

        var name = Path.GetFileName(path);
        var (cached, content) = CacheHit(path);
        if (cached)
        {
            logger.WriteVerbose($"Memory Cache Hit: {name}");
            return Encoding.UTF8.GetString(content!);
        }

        logger.WriteVerbose($"Caching content from: {name}");
        var bytes = await File.ReadAllTextAsync(path);
        return Cache(path, bytes);
    }

    public byte[] Cache(string path, byte[] bytes)
    {
        lock (Lock)
        {
            this[path] = (DateTime.Now, bytes);
        }

        return bytes;
    }

    public string Cache(string path, string data)
    {
        lock (Lock)
        {
            this[path] = (DateTime.Now, Encoding.UTF8.GetBytes(data));
        }
        return data;
    }
}
