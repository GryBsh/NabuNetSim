using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Services;
using System;

namespace Nabu.Network;

public class NetworkEmulator : NabuEmulator
{
    readonly HttpClient Http;
    //readonly AdaptorSettings Settings;
    readonly NetworkState State = new();

    public NetworkEmulator(
        ILogger<NetworkEmulator> logger,
        HttpClient http,
        ChannelSources sources
    ) : base(logger)
    {
        Logger = logger;
        Http = http;
        State.Sources = sources;
    }

    #region PAK
    async Task<byte[]> HttpGetPakBytes(string url, int segment)
    {
        var filename = Tools.PakName(segment);
        url = $"{url}/{filename}.npak";
        var npak = await Http.GetByteArrayAsync(url);
        Trace($"NPAK Length: {npak.Length}");
        npak = Tools.Unpack(npak);
        Trace($"Segment Length: {npak.Length}");
        return npak;
    }
    async Task<byte[]> FileGetPakBytes(string path, int segment)
    {
        var filename = Tools.PakName(segment);
        path = Path.Join(path, $"{filename}.npak");
        var npak = await File.ReadAllBytesAsync(path);
        Trace($"NPAK Length: {npak.Length}");
        npak = Tools.Unpack(npak);
        Trace($"Segment Length: {npak.Length}");
        return npak;
    }
    #endregion
    
    #region Channel List
    async IAsyncEnumerable<Channel> GetChannelList(string sourceName, ChannelSource source)
    {
        if (source.Type is ChannelSourceType.Folder) {
            if (source.NabuRoot is null && source.PakRoot is null) yield break;
            if (source.PakRoot is not null){
                var files = Directory.GetFiles(source.PakRoot, "*.npak");
                if (files.Length > 0) {
                    yield return new(
                        nameof(source.PakRoot), 
                        sourceName, 
                        source.Type, 
                        source.PakRoot, 
                        ChannelType.LocalPak
                    );                    
                } 
            }

            if (source.NabuRoot is not null) {
                var files = Directory.GetFiles(source.NabuRoot, "*.nabu");
                foreach (var file in files)
                {
                    var name = file.Split('.')[0].Split(Path.DirectorySeparatorChar)[^1];
                    yield return new(name, sourceName, source.Type, file, ChannelType.LocalNabu);
                }
            }
        }
        else
        {
            if (source.ListUrl is null) yield break;
            string list = string.Empty;
            try
            {

                list =  State.SourceCache.ContainsKey(sourceName) ? 
                            State.SourceCache[sourceName] : 
                            State.SourceCache[sourceName] = await Http.GetStringAsync(source.ListUrl);
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
    async Task<bool> ChangeChannelList()
    {
        Log($"Refresh Channel List from Source: {State.Source}");
        if (State.HasChannels) return true;
        if (State.Sources is null) return false;
        if (State.Source is null) return false;
        if (State.Sources.ContainsKey(State.Source) is false) return false;

        var source = State.Sources[State.Source];
        if (source is null) return false;
        State.Channels.Clear();
        await foreach (var channel in GetChannelList(State.Source, source))
        {
            Trace($"Adding {channel.Name} from {channel.Source}");
            State.Channels.Add(channel.Name, channel);
        }
        Log($"Refreshed ({State.Channels.Count} Channels)");
        return true;
    }

    public void SetState(AdaptorSettings settings) {
        Log($"Source: {settings.Source}, Channel: {settings.Channel}");
        State.Source = settings.Source;
        State.Channel = settings.Channel;
        State.ClearCache();
        Task.Run(ChangeChannelList);
    }

    #endregion
    
    public async Task<(ImageType, byte[])> Request(int segment)
    {
        if (State.Source is null || State.Channel is null)
        {
            Warning("No Channel or Source");
            return (ImageType.None, Array.Empty<byte>());
        }
        if (State.HasChannels is false) {
            if (!await ChangeChannelList())
            {
                Warning("No Channel List");
                return (ImageType.None, Array.Empty<byte>());
            }
        }
       
        var channel = State.Channels[State.Channel];
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
                ChannelType.RemotePak  => await HttpGetPakBytes(channel.Path, segment),
                ChannelType.LocalPak => await FileGetPakBytes(channel.Path, segment),
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
