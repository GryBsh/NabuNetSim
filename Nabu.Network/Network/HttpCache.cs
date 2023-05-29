using Nabu.Services;
using System.Net;

namespace Nabu.Network;

public class CachingHttpClient : IHttpCache
{
    HttpClient Http { get; }
    ILog Logger { get; }
    AdaptorSettings Settings { get; }
    protected string CacheFolder { get; }
    protected IFileCache Cache { get; }
    //readonly Settings Settings;

    public CachingHttpClient(HttpClient http, ILog<CachingHttpClient> logger, IFileCache cache)
        : this(http, logger, cache, null) { }
        
    public CachingHttpClient(HttpClient http, ILog logger, IFileCache cache, AdaptorSettings? settings = null)
    {
        Http = http;
        Logger = logger;
        Cache = cache;
        Settings = settings ?? new NullAdaptorSettings();
        CacheFolder = Settings is TCPAdaptorSettings ?
                Path.Join(AppContext.BaseDirectory, "cache", $"{Settings.Port.Split(":")[0]}") :
                Path.Combine(AppContext.BaseDirectory, "cache");
        Task.Run(() => NabuLib.EnsureFolder(CacheFolder));
    }

    public async Task<HttpResponseMessage> GetHead(string uri)
    {
        try
        {
            return await Http.SendAsync(new(HttpMethod.Head, uri));
        }
        catch (Exception ex)
        {
            Logger.WriteError(string.Empty, ex);
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }
    }

    public async Task<UriStatus> GetUriStatus(string uri, string? path = null)
    {
        var head = await GetHead(uri);
        path ??= Path.Join(CacheFolder, NabuLib.SafeFileName(uri));
        var pathExists = File.Exists(path);
        
        if (!pathExists)
        {
            if (!head.IsSuccessStatusCode)
            {
                return new (false, false, false, DateTime.Now); //No download, not found, no local
            }
            return new (true, true, false, DateTime.MinValue); //Download, Found, None
        }

        var modified = head.Content.Headers.LastModified;
        var lastCached = Cache.LastChange(path);

        if (modified > lastCached)
        {
            return new (true, true, true, DateTime.MinValue);
        }
        return new (false, true, true, lastCached);
    }

    public string CachePath(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        return Path.Join(CacheFolder, safeName);
    }

    public async Task<Memory<byte>> GetBytes(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);
        var name = Path.GetFileName(uri);

        var (shouldDownload, found, local, last) = await GetUriStatus(uri, path);

        if (!shouldDownload && !found && !local)
            return Array.Empty<byte>();

        if (shouldDownload && found)
        {
            Logger.Write($"Downloading {uri}");
            var bytes = await Http.GetByteArrayAsync(uri);

            Logger.Write($"Writing {bytes.Length} bytes to {name}");
            try
            {
                //await File.WriteAllBytesAsync(path, bytes);
                Cache.CacheFile(path, bytes, true);
            }
            catch
            {
                Logger.WriteWarning("Caching failed, please try again later");
            }
            return bytes;
        }

        Logger.Write($"Reading {name} from cache");
        return await Cache.GetFile(path);
    }

    

    public async Task<string> GetString(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);
        var name = Path.GetFileName(uri);
        var (shouldDownload, found, local, _) = await GetUriStatus(uri, path);

        if (!shouldDownload && !found && !local) return string.Empty;

        if (shouldDownload && found)
        {
            Logger.Write($"Downloading {uri}");
            var str = await Http.GetStringAsync(uri);

            Logger.Write($"Writing {str.Length} characters to {name} in cache");
            try
            {
                //await File.WriteAllTextAsync(path, str);
                Cache.CacheString(path, str, true);
            }
            catch
            {
                Logger.WriteWarning("Caching failed, please try again later");
            }
            
            return str;
        }
        
        Logger.Write($"Reading {name} from cache");
        return await Cache.GetString(path);

    }

}
