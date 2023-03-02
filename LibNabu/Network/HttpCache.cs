using Nabu.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Nabu.Network;

public class CachingHttpClient : IHttpCache
{
    HttpClient Http { get; }
    IConsole Logger { get; }
    
    protected string CacheFolder => Path.Join(AppContext.BaseDirectory, "cache");
    protected FileCache MemoryCache { get; }
    public CachingHttpClient(HttpClient http, IConsole logger, FileCache cache)
    {
        Http = http;
        Logger = logger;
        MemoryCache = cache;
        Task.Run(EnsureCacheFolder);
    }

    void EnsureCacheFolder()
    {
        if (Directory.Exists(CacheFolder) is false)
            Directory.CreateDirectory(CacheFolder);
    }

    public async Task<HttpResponseMessage> GetHead(string uri)
    {
        try
        {
            return await Http.SendAsync(new(HttpMethod.Head, uri));
        }
        catch
        {
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        }
    }

    public async Task<(bool, bool, bool)> CanGet(string uri, string? path = null)
    {
        path ??= NabuLib.SafeFileName(uri);
        var pathExists = File.Exists(path);
        var head = await GetHead(uri);
        if (!pathExists)
        {
            if (!head.IsSuccessStatusCode)
            {
                return (false, false, false); //No download, not found, no local
            }
            return (true, true, false); //Download, Found, None
        }

        var modified = head.Content.Headers.LastModified;
        var localModified = await Task.Run(() => File.GetLastWriteTimeUtc(path));

        if (modified > localModified)
        {
            return (true, true, true);
        }
        return (false, true, true);
    }

    public async Task<byte[]> GetBytes(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);
        var name = Path.GetFileName(uri);

        var (shouldDownload, found, local) = await CanGet(uri, path);

        if (!shouldDownload && !found && !local)
            return Array.Empty<byte>();

        if (shouldDownload && found)
        {
            Logger.Write($"Downloading {uri}");
            var bytes = await Http.GetByteArrayAsync(uri);

            Logger.Write($"Writing {bytes.Length} bytes to {CacheFolder}\\{name}");
            try
            {
                await File.WriteAllBytesAsync(path, bytes);
                MemoryCache.Cache(path, bytes);
            }
            catch
            {
                Logger.WriteWarning("Caching failed, please try again later");
            }

            return bytes;
        }

        Logger.Write($"Reading {name} from cache");

        return await MemoryCache.CacheFile(Logger, path);
        
    }

    

    public async Task<string> GetString(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);
        var name = Path.GetFileName(uri);
        var (shouldDownload, found, local) = await CanGet(uri, path);

        if (!shouldDownload && !found && !local) return string.Empty;

        if (shouldDownload && found)
        {
            Logger.Write($"Downloading {uri}");
            var str = await Http.GetStringAsync(uri);

            Logger.Write($"Writing {str.Length} characters to {CacheFolder}\\{name}");
            try
            {
                await File.WriteAllTextAsync(path, str);
                MemoryCache.Cache(path, str);
            }
            catch
            {
                Logger.WriteWarning("Caching failed, please try again later");
            }
            
            return str;
        }

        return await MemoryCache.CacheFileText(Logger, path);

    }

}
