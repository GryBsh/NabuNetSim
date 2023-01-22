using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Network;

public class HttpProgramRetriever : ProgramRetriever
{
    public ILogger<ProgramRetriever> Logger { get; }
    public HttpClient Http { get; }

    public HttpProgramRetriever(ILogger<ProgramRetriever> logger, HttpClient http)
    {
        Logger = logger;
        Http = http;
    }
    
    public static bool IsWebSource(string path) => path.StartsWith("http");

    #region HTTP

    static string PakUrl(string url, int pak, bool encrypted)
    {
        var suffix = encrypted ? "npak" : "pak";
        var filename = NabuLib.PakName(pak);
        return $"{url}/{filename}.{suffix}";
    }

    static string NabuUrl(string url, int pak, string? image = null)
    {
        var filename = image switch
        {
            null => NabuLib.FormatTriple(pak),
            _ => image
        };
        return $"{url}/{filename}.nabu";
    }

    public async Task<byte[]> GetPakBytes(string url, int pak, bool encrypted = true)
    {
        if (!IsPak(url)) url = PakUrl(url, pak, encrypted);
        var npak = await Http.GetByteArrayAsync(url);

        Logger.LogTrace($"NPAK Length: {npak.Length}");
        npak = NabuLib.Unpak(npak);
        Logger.LogTrace($"Segment Length: {npak.Length}");
        return npak;
    }
    public async Task<(bool, string)> FoundPak(string url, int pak)
    {
        if (!IsPak(url)) {
            var _url = PakUrl(url, pak, true);
            var found = await GetHead(_url);
            if (found) return (true, _url);
            
            _url = PakUrl(url, pak, false);
            found = await GetHead(_url);
            if (found) return (true, _url);

            return (false, string.Empty);
        }
        return (await GetHead(url), url);
    }

    public async Task<byte[]> GetRawBytes(string url, int pak, string? image = null)
    {
        if (!IsNabu(url)) url = NabuUrl(url, pak, image);
        var buffer = await Http.GetByteArrayAsync(url);
        return buffer;
    }
    public async Task<bool> FoundRaw(string url, int pak, string? image = null)
    {
        if (!IsNabu(url)) url = NabuUrl(url, pak, image);
        return await GetHead(url);
    }

    async Task<bool> GetHead(string url)
    {
        var response = await Http.SendAsync(new(HttpMethod.Head, url));
        return response.IsSuccessStatusCode;
    }

    #endregion

}
