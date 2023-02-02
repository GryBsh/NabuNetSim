using Microsoft.Extensions.Logging;
using Nabu.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Network;

public enum ProgramListType
{
    Unknown = 0,
    NabuCa
}

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

    public async Task<(bool, NabuProgram[])> FoundNabuCaList(string source, string uri)
    {
        var type = uri switch
        {
            _ when uri.EndsWith(".txt") => ProgramListType.NabuCa,
            _ => ProgramListType.Unknown
        };

        if (type == ProgramListType.Unknown) { return (false, Array.Empty<NabuProgram>()); }
        var found = await GetHead(uri);
        if (found is false) { return (false, Array.Empty<NabuProgram>()); }
        
        var lines = (await Http.GetStringAsync(uri)).Split('\n');
        var progs = new List<NabuProgram>();
        foreach ( var line in lines)
        {
            if (line.StartsWith('!') || line.StartsWith(':')) continue;
            
            var parts = line.Split(';');
            var name = parts[0].Trim();
            var isNabu = name.EndsWith(".nabu");
            if (isNabu is false) continue;

            var displayName = parts[1].Trim();

            progs.Add(new(
                displayName,
                name,
                source,
                DefinitionType.NabuCaList,
                $"https://cloud.nabu.ca/HomeBrew/titles/{name}",
                SourceType.Remote,
                ImageType.Raw,
                new[] { new PassThroughPatch(Logger) }
            ));
        }
        return (true, progs.ToArray());
    }

    #region HTTP

    string PakUrl(string url, int pak, bool encrypted)
    {
        if (IsPak(url)) return url;

        var suffix = encrypted ? "npak" : "pak";
        var filename = NabuLib.PakName(pak);
        return $"{url}/{filename}.{suffix}";
    }

    string NabuUrl(string url, int pak, string? image = null)
    {

        if (IsNabu(url)) return url;

        var filename = image switch
        {
            null => NabuLib.FormatTriple(pak),
            _ => image
        };
        return $"{url}/{filename}.nabu";
    }

    public async Task<byte[]> GetPakBytes(string url, int pak, bool encrypted = true)
    {
        if (!IsPak(url))
        {
            var (found, uri) = await FoundPak(url, pak);
            if (found) url = uri;
        }

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
