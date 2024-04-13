﻿using Microsoft.Extensions.Logging;using Microsoft.Extensions.Options;using System.Net;using System.Reactive.Linq;namespace Gry.Caching;public partial class HttpCache{    public string? CachePath(string uri)    {        var safeName = SafeFileName(uri);        var path = Path.Join(CacheFolder, safeName);        if (File.Exists(path))        {            return path;        }        return null;    }    public (string, string) CacheFileNames(string uri)    {        var safeName = SafeFileName(uri);        var path = Path.Join(CacheFolder, safeName);        var name = Path.GetFileName(uri);        return (path, name);    }    private async Task CheckConnection()    {        try        {            var entry = await Dns.GetHostEntryAsync("raw.githubusercontent.com");            InternetAvailable = entry.AddressList.Length > 0;        }        catch        {            Logger.LogDebug("No Internet Connection");            InternetAvailable = false;        }    }    public async Task<HttpResponseMessage?> Head(string uri)    {        try        {            return await Http.SendAsync(new(HttpMethod.Head, uri));        }        catch (Exception ex)        {            Logger.LogWarning("Error making HEAD request: {}", ex.Message);            return new HttpResponseMessage(HttpStatusCode.BadRequest);        }    }    public async Task<PathStatus> GetPathStatus(string uri, string? path = null)    {        path ??= Path.Join(CacheFolder, SafeFileName(uri));        var pathExists = File.Exists(path);        var lastCached = Cache.LastChange(path);        if (pathExists &&            lastCached.AddMinutes(Settings.MinimumCacheTimeMinutes) > DateTime.Now        )        {            return new(false, true, true, lastCached, HttpStatusCode.OK);        }        var head = await Head(uri);        if (head?.IsSuccessStatusCode is false && !pathExists)            return new(false, false, false, DateTime.MinValue, head?.StatusCode, head?.ReasonPhrase); //No download, not found, no local                var length = 0;        if (head?.Headers.TryGetValues("Content-Length", out var lengths) is true)            length = int.Parse(lengths.First());        if (!pathExists)            return new(true, true, false, DateTime.MinValue, head?.StatusCode, Length: length); //Download, Found, None                var modified = head?.Content.Headers.LastModified?.LocalDateTime;        modified ??= lastCached > DateTime.MinValue ?                        lastCached.AddMinutes(Settings.MinimumCacheTimeMinutes) :                        DateTime.MinValue;        if (modified > lastCached)            return new(true, true, true, modified.Value, head?.StatusCode ?? HttpStatusCode.Found, Length: length);                return new(false, true, true, lastCached, head?.StatusCode ?? HttpStatusCode.NotModified, Length: length);    }}