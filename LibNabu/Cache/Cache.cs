using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Cache;

public class HttpCache
{
    HttpClient Http { get; }
   
    
    public string CacheFolder => Path.Join(AppContext.BaseDirectory, "cache");

    public HttpCache(HttpClient http)
    {
        
        Http = http;
        Task.Run(EnsureCacheFolder);
    }

    void EnsureCacheFolder()
    {
        if (Directory.Exists(CacheFolder) is false)
            Directory.CreateDirectory(CacheFolder);
    }


    public async Task<HttpResponseMessage> GetHead(string uri) 
        => await Http.SendAsync(new(HttpMethod.Head, uri));

    async Task<(bool, bool, bool)> ShouldDownload(string uri, string path)
    {

        var pathExists = File.Exists(path);

        var head = await GetHead(uri);
        if (!head.IsSuccessStatusCode && !pathExists) return (false, false, false); //No download, not found, no local
        if (!pathExists) return (true, true, false); //Download, Found, None

        var modified = head.Content.Headers.LastModified;
        var localModified = File.GetLastWriteTimeUtc(path);
        
        if (modified > localModified) return (true, true, true);
        return (false, true, true);
    }

    public async Task<byte[]> GetBytes(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);

        var (shouldDownload, found, local) = await ShouldDownload(uri, path);

        if (!shouldDownload && !found && !local) return Array.Empty<byte>();

        if (shouldDownload && found)
        {
            var bytes = await Http.GetByteArrayAsync(uri);
            await File.WriteAllBytesAsync(path, bytes);
            return bytes;
        }
        
        return await File.ReadAllBytesAsync(path);
        
    }

    public async Task<string> GetString(string uri)
    {
        var safeName = NabuLib.SafeFileName(uri);
        var path = Path.Join(CacheFolder, safeName);

        var (shouldDownload, found, local) = await ShouldDownload(uri, path);

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
