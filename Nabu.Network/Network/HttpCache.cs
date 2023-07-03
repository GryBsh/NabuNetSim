using Nabu.Services;
using System.Net;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace Nabu.Network;

public class HttpCache : DisposableBase, IHttpCache
{
    public HttpCache(HttpClient http, ILog<HttpCache> logger, IFileCache cache, Settings settings)
        : this(http, logger, cache, settings, null)
    {
    }

    public HttpCache(HttpClient http, ILog logger, IFileCache cache, Settings settings, AdaptorSettings? adaptor = null)
    {
        Http = http;
        Logger = logger;
        Cache = cache;
        Settings = settings;
        AdaptorSettings = adaptor ?? new NullAdaptorSettings();
        CacheFolder = AdaptorSettings is TCPAdaptorSettings ?
                Path.Join(AppContext.BaseDirectory, "cache", $"{AdaptorSettings.Port.Split(":")[0]}") :
                Path.Combine(AppContext.BaseDirectory, "cache");
        Task.Run(() => NabuLib.EnsureFolder(CacheFolder));
        Disposables.Add(
            Observable.Interval(TimeSpan.FromMinutes(1))
                      .Subscribe(async _ => await CheckConnection())
        );
    }

    public bool InternetAvailable { get; private set; }
    protected IFileCache Cache { get; }
    protected string CacheFolder { get; }
    protected Settings Settings { get; }
    private AdaptorSettings AdaptorSettings { get; }
    private HttpClient Http { get; }
    private ILog Logger { get; }

    public string? CachePath(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);
        if (File.Exists(path))
        {
            return path;
        }
        return null;
    }

    public async Task<Memory<byte>> GetBytes(string uri)
    {
        //var safeName = NabuLib.SafeFileName(uri);
        //var path = Path.Join(CacheFolder, safeName);
        //var name = Path.GetFileName(uri);

        var (path, name) = CacheFileNames(uri);

        var (shouldDownload, found, local, _, _) = await GetUriStatus(uri, path);

        if (!shouldDownload && !found && !local)
            return Array.Empty<byte>();

        if (shouldDownload && found)
        {
            return await DownloadAndCache(uri, path, name);
        }

        Logger.WriteVerbose($"Reading {name} from cache");
        return await Cache.GetBytes(path);
    }

    public async Task<string?> GetFile(string uri, bool bypassCache = false)
    {
        var (path, name) = CacheFileNames(uri);

        var (shouldDownload, found, local, _, _) = await GetUriStatus(uri, path);

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
            Logger.WriteVerbose($"{name} is cached");
        }
        return path;
    }

    public async Task<HttpResponseMessage?> GetHead(string uri)
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

    public async Task<string> GetString(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);
        var name = Path.GetFileName(uri);
        var (shouldDownload, found, local, _, _) = await GetUriStatus(uri, path);

        if (!shouldDownload && !found && !local) return string.Empty;

        if (shouldDownload && found)
        {
            Logger.Write($"Downloading {uri}");
            var str = await Http.GetStringAsync(uri);

            Logger.WriteVerbose($"Writing {str.Length} characters to {name} in cache");
            try
            {
                //await File.WriteAllTextAsync(path, str);
                await Cache.CacheString(path, str, true);
            }
            catch
            {
                Logger.WriteWarning("Caching failed, please try again later");
            }

            return str;
        }

        Logger.WriteVerbose($"Reading {name} from cache");
        return await Cache.GetString(path);
    }

    public async Task<UriStatus> GetUriStatus(string uri, string? path = null)
    {
        path ??= Path.Join(CacheFolder, NabuLib.SafeFileName(uri));
        var pathExists = File.Exists(path);
        var lastCached = Cache.LastChange(path);

        if (pathExists &&
            lastCached.AddMinutes(Settings.MinimumCacheTimeMinutes) > DateTime.Now
        )
        {
            return new(false, true, true, lastCached);
        }

        var head = await GetHead(uri);
        if (!pathExists)
        {
            if (!head.IsSuccessStatusCode)
            {
                return new(false, false, false, DateTime.MinValue); //No download, not found, no local
            }
            var length = 0;
            if (head.Headers.TryGetValues("Content-Length", out var lengths))
                length = int.Parse(lengths.First());

            return new(true, true, false, DateTime.MinValue, length); //Download, Found, None
        }

        var modified = head.Content.Headers.LastModified?.LocalDateTime;
        modified ??= lastCached > DateTime.MinValue ?
                        lastCached.AddMinutes(Settings.MinimumCacheTimeMinutes) :
                        DateTime.MinValue;

        if (modified > lastCached)
        {
            return new(true, true, true, lastCached);
        }
        return new(false, true, true, lastCached);
    }

    private (string, string) CacheFileNames(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);
        var name = Path.GetFileName(uri);
        return (path, name);
    }

    private async Task CheckConnection()
    {
        try
        {
            var entry = await Dns.GetHostEntryAsync("raw.githubusercontent.com");
            InternetAvailable = entry.AddressList.Length > 0;
        }
        catch
        {
            Logger.WriteVerbose("No Internet Connection");
            InternetAvailable = false;
        }
    }

    private async Task<Memory<byte>> DownloadAndCache(string uri, string path, string name)
    {
        try
        {
            Logger.Write($"Downloading {uri}");
            var bytes = await Http.GetByteArrayAsync(uri);
            Logger.WriteVerbose($"Writing {bytes.Length} bytes to {name}");
            await Cache.CacheFile(path, bytes, true);
            return bytes;
        }
        catch (Exception ex)
        {
            Logger.WriteWarning(null, ex);
            return Array.Empty<byte>();
        }
    }
}