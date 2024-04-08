using Microsoft.Extensions.Logging;
using System.Text;

namespace Gry.Caching
{


    public class FileCache(
        ILogger<FileCache> logger
    ) : IFileCache
    {
        private MemoryCache<Memory<byte>> Cache { get; } = new();

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

            return await Cache.GetOrUpdate(
                path,
                (timestamp, old) =>
                {
                    if (lastWrite > timestamp)
                    {
                        return UpdateCheck<Memory<byte>>.Create(true, async () => await File.ReadAllBytesAsync(path));
                    }
                    return UpdateCheck<Memory<byte>>.Create(false, null);
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

        public void UnCache(string path)
        {
            var removed = Cache.Keys.FirstOrDefault(k => k.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            if (removed is not null)
                Cache.Remove(path);
        }

        public void UnCachePath(string path)
        {
            var removed = Cache.Keys.Where(k => k.StartsWith(path, StringComparison.InvariantCultureIgnoreCase));
            foreach (var key in removed)
                Cache.Remove(key);
        }
    }
}