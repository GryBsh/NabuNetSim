using Microsoft.Extensions.Logging;
using Nabu.Services;
using System;

namespace Nabu.Network;

public class NetworkEmulator : NabuEmulator
{
    readonly HttpClient Http;
    readonly NetworkSettings Settings;
    readonly NetworkState State = new();

    public NetworkEmulator(
        ILogger<NetworkEmulator> logger,
        HttpClient http,
        ChannelSources sources,
        NetworkSettings settings
    ) : base(logger)
    {
        Logger = logger;
        Http = http;
        Settings = settings;
        State.Sources = sources;

        if (settings.Source is not null &&
            State.Sources.ContainsKey(settings.Source)
        ) State.Source = settings.Source;

    }

    async Task<byte[]> HttpGetPakBytes(string url, int segment, ChannelType type)
    {
        if (type is not ChannelType.RemotePak)
        {
            Error($"Channel is not remote pak");
            return Array.Empty<byte>();
        }
        var filename = Tools.PakName(segment);
        
        url = $"{url}/{filename}.npak";
        var npak = await Http.GetByteArrayAsync(url);
        Trace($"NPAK Length: {npak.Length}");
        npak = Tools.Unpack(npak);
        Trace($"Segment Length: {npak.Length}");
        return npak;
    }

    async IAsyncEnumerable<Channel> GetChannelList(string sourceName, ChannelSource source)
    {
        if (source.Type is ChannelSourceType.Folder) {
            if (source.NabuRoot is null) yield break;
            var files = Directory.GetFiles(source.NabuRoot, "*.nabu");
            foreach (var file in files)
            {
                var name = file.Split('.')[0].Split(Path.DirectorySeparatorChar)[^1];
                yield return new(name, sourceName, source.Type, file, ChannelType.LocalNabu);
            }
        }
        else
        {
            if (source.ListUrl is null) yield break;
            string list = string.Empty;
            try
            {
                list = await Http.GetStringAsync(source.ListUrl);
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                yield break;
            }

            var parts =
                list.Split('\n')
                    .Select(l => l.Split(';')
                                    .Select(s => s.Trim())
                                    .ToArray()
                    );

            foreach (var line in parts)
            {
                var name = line[^1];
                var path = line[0];
                var lowerPath = path.ToLowerInvariant();

                if (name is null || path is null)
                {
                    Warning($"Invalid from Source {sourceName}: Channel: {name}, Path: {path}");
                    continue;
                }
                var isNabuFile = path.ToLower().EndsWith(".nabu");
                var root = isNabuFile ? source.NabuRoot : source.PakRoot;
                var isRemote = (root ?? string.Empty).ToLower().StartsWith("http");
                var pathSeperator = isRemote ? '/' : Path.DirectorySeparatorChar;
                var realPath = $"{root}{pathSeperator}{path}";
                var type = isRemote && isNabuFile ?
                                ChannelType.RemoteNabu :
                            isRemote ?
                                ChannelType.RemotePak :
                            isNabuFile ?
                                ChannelType.LocalNabu :
                                ChannelType.LocalPak;

                yield return new(name, sourceName, source.Type, realPath, type);
            }
        }
    }
    async Task<bool> RefreshChannelList()
    {
        Log($"Refresh Channel List from Source: {State.Source}");
        if (State.HasChannels) return true;
        if (State.Sources is null) return false;

        State.Source ??= State.Sources.Keys.First();
        var source = State.Sources[State.Source];
        if (source is null) return false;
        State.Channels.Clear();
        await foreach (var channel in GetChannelList(State.Source, source))
        {
            Log($"Adding {channel.Name} from {channel.Source}");
            State.Channels.Add(channel.Name, channel);
        }
        return true;
    }

    bool Loaded = false;
    public async Task PreLoad()
    {
        if (Loaded)
        {
            Logger.LogWarning("Channel List Already Loaded");
            return;
        }
        if (!State.HasChannels)
        {
            if (!await RefreshChannelList())
            {
                Debug("No Channel List");
                return;
            }
            State.ClearCache();
        }
        Loaded = true;
    }

    /// <summary>
    /// Requests Segment data from the current channel
    /// </summary>
    /// <param name="segment"></param>
    /// <returns></returns>
    public async Task<(ImageType, byte[])> Request(int segment)
    {
        if (!State.HasChannels)
        {
            if (!await RefreshChannelList())
            {
                Debug("No Channel List");
                return (ImageType.None, Array.Empty<byte>());
            }
        }
        Settings.Channel ??= State.Channels.Keys.First();
        var channel = State.Channels[Settings.Channel];
        var imageType = channel.Type switch
        {
            ChannelType.LocalPak or ChannelType.RemotePak => ImageType.Pak,
            _ => ImageType.Nabu
        };
        if (State.SegmentCache.ContainsKey(segment))
        {
            return (imageType, State.SegmentCache[segment]);
        }

        byte[] bytes = Array.Empty<byte>();
        try
        {
            bytes = channel.Type switch
            {
                ChannelType.RemoteNabu => await Http.GetByteArrayAsync(channel.Path),
                ChannelType.LocalNabu => await File.ReadAllBytesAsync(channel.Path),
                ChannelType.RemotePak  => await HttpGetPakBytes(channel.Path, segment, channel.Type),
                _ => Array.Empty<byte>()
            };
        }
        catch (Exception ex)
        {
            Error(ex.Message);
            return (ImageType.None, Array.Empty<byte>());
        }
        State.SegmentCache[segment] = bytes;
        
        return (imageType, bytes);
    }
}
