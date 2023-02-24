using Nabu.Services;

namespace Nabu.Network;

public class HttpCache : IHttpCache
{
    HttpClient Http { get; }
    IConsole Logger { get; }

    public string CacheFolder => Path.Join(AppContext.BaseDirectory, "cache");

    public HttpCache(HttpClient http, IConsole logger)
    {
        Http = http;
        //Http.Timeout = TimeSpan.FromSeconds(180);
        Logger = logger;
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
        var localModified = File.GetLastWriteTimeUtc(path);

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

        var (shouldDownload, found, local) = await CanGet(uri, path);

        if (!shouldDownload && !found && !local)
            return Array.Empty<byte>();

        if (shouldDownload && found)
        {
            Logger.Write($"Downloading {uri}");
            var bytes = await Http.GetByteArrayAsync(uri);
            
            Logger.Write($"Writing {bytes.Length} bytes to {path}");
            await File.WriteAllBytesAsync(path, bytes);

            return bytes;
        }

        Logger.Write($"Reading {path} from cache");
        return await File.ReadAllBytesAsync(path);

    }

    public async Task<string> GetString(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);

        var (shouldDownload, found, local) = await CanGet(uri, path);

        if (!shouldDownload && !found && !local) return string.Empty;

        if (shouldDownload && found)
        {

            var str = await Http.GetStringAsync(uri);
            await File.WriteAllTextAsync(path, str);
            return str;
        }

        return await File.ReadAllTextAsync(path);
    }

}
