using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reactive.Linq;

namespace Gry.Caching;

public partial class HttpCache : DisposableBase, IHttpCache
{
    public static void EnsureFolder(string folder)
    {
        if (Directory.Exists(folder) is false)
            Directory.CreateDirectory(folder);
    }

    public static string SafeFileName(string name)
    {
        foreach (var bad in Path.GetInvalidFileNameChars())
            name = name.Replace(bad, '_');
        return name;
    }

    public HttpCache(
        HttpClient http, 
        ILogger<HttpCache> logger, 
        IFileCache cache, 
        IOptions<CacheOptions> settings
    ) : this(http, logger, cache, settings.Value, null)
    {
    }

    public HttpCache(
        HttpClient http, 
        ILogger logger, 
        IFileCache cache, 
        CacheOptions settings, 
        string? name = null
    )
    {
        Http = http;
        Logger = logger;
        Cache = cache;
        Settings = settings;
        CacheFolder = Path.Combine(
            AppContext.BaseDirectory, 
            settings.HttpCacheFolderName, 
            name ?? string.Empty
        );
                
        Task.Run(() => EnsureFolder(CacheFolder));
        Task.Run(() => CheckConnection());
        Disposables.AddInterval(
            TimeSpan.FromMinutes(1), 
            async _ => await CheckConnection()
        );    
    }

    public bool InternetAvailable { get; private set; }
    protected IFileCache Cache { get; }
    protected string CacheFolder { get; }
    protected CacheOptions Settings { get; }
    private HttpClient Http { get; }
    private ILogger Logger { get; }

   
    public async Task<Memory<byte>> GetBytes(string uri)
    {
        //var safeName = NabuLib.SafeFileName(uri);
        //var path = Path.Join(CacheFolder, safeName);
        //var name = Path.GetFileName(uri);

        var (path, name) = CacheFileNames(uri);

        var (shouldDownload, found, local, _, _, _, _) = await GetPathStatus(uri, path);

        if (!shouldDownload && !found && !local)
            return Array.Empty<byte>();

        if (shouldDownload && found)
        {
            return await DownloadAndCache(uri, path, name);
        }

        Logger.LogDebug("Reading {} from cache", name);
        return await Cache.GetBytes(path);
    }

    public async Task<string?> GetFile(string uri, bool bypassCache = false)
    {
        var (path, name) = CacheFileNames(uri);

        var (shouldDownload, found, local, _, _, _, _) = await GetPathStatus(uri, path);

        if (!bypassCache && !shouldDownload && !found && !local)
            return null;

        if (bypassCache || shouldDownload && found)
        {
            var bytes = await DownloadAndCache(uri, path, name);
            if (bytes.Length is 0)
                return null;
        }
        else
        {
            Logger.LogDebug("{} is cached", name);
        }
        return path;
    }

    

    public async Task<string> GetString(string uri)
    {
        var safeName = SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);
        var name = Path.GetFileName(uri);
        var (shouldDownload, found, local, _, _, _, _) = await GetPathStatus(uri, path);

        if (!shouldDownload && !found && !local) return string.Empty;

        if (shouldDownload && found)
        {
            Logger.LogDebug("Downloading {}", uri);

            try
            {
                var str = await Http.GetStringAsync(uri);
                Logger.LogDebug("Writing {} characters to {} in cache", str.Length, name);
                await Cache.CacheString(path, str, true);
                return str;
            }
            catch (Exception ex)
            {
                if (local)
                {
                    Logger.LogDebug("Error get remote file, using {} from cache", name);
                    return await Cache.GetString(path);
                }

                Logger.LogWarning(ex, "Caching failed, please try again later");
                return string.Empty;
            }
        }
        // found:
        Logger.LogDebug("Reading {} from cache", name);
        return await Cache.GetString(path);
    }

    

    

    public async Task<Memory<byte>> DownloadAndCache(string uri, string? path, string? name)
    {
        try
        {
            (path, name) = (path, name) switch
            {
                (null, null) => CacheFileNames(uri),
                (null, _) => (Path.Join(CacheFolder, name), name),
                (_, null) => (path, Path.GetFileName(uri)),
                _ => (path, name)
            }; 

            Logger.LogDebug($"Downloading {uri}");
            var bytes = await Http.GetByteArrayAsync(uri);
            Logger.LogDebug($"Writing {bytes.Length} bytes to {name}");
            await Cache.CacheFile(path!, bytes, true);
            return bytes;
        }
        catch 
        {
            Logger.LogWarning("Failed to download and cache {}", name);
            return Array.Empty<byte>();
        }
    }
}