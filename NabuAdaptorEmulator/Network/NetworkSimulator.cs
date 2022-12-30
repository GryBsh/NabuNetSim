using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Patching;
using Nabu.Services;

namespace Nabu.Network;

public class NetworkSimulator : NabuService
{
    readonly HttpClient Http;
    AdaptorSettings? Settings;
    readonly NetworkState State = new();

    public NetworkSimulator(
        ILogger<NetworkSimulator> logger,
        HttpClient http,
        Dictionary<string, ImageSourceDefinition> sources
    ) : base(logger)
    {
        Logger = logger;
        Http = http;
        State.SourceDefinitions = sources;
    }

    #region PAK

    async Task<byte[]> HttpGetPakBytes(string url, int pak)
    {
        var filename = NABU.PakName(pak);
        url = $"{url}/{filename}.npak";
        var npak = await Http.GetByteArrayAsync(url);
        //Trace($"NPAK Length: {npak.Length}");
        npak = NABU.Unpak(npak);
        //Trace($"Segment Length: {npak.Length}");
        return npak;
    }
    async Task<byte[]> FileGetPakBytes(string path, int pak)
    {
        var filename = NABU.PakName(pak);
        path = Path.Join(path, $"{filename}.npak");
        var npak = await File.ReadAllBytesAsync(path);
        Trace($"NPAK Length: {npak.Length}");
        npak = NABU.Unpak(npak);
        Trace($"Segment Length: {npak.Length}");
        return npak;
    }
    #endregion

    #region Source List

    /// <summary>
    /// Get the list of PAK sources from a given folder
    /// </summary>
    /// <param name="sourceName">The name of the source definition</param>
    /// <param name="root">A given folder</param>
    /// <returns></returns>
    IEnumerable<ProgramImage> GetLocalPakSources(string sourceName, string root)
    {
        var folders = Directory.GetDirectories(root);
        foreach (var folder in folders)
        {
            var files = Directory.GetFiles(folder, "*.npak");
            if (files.Length > 0)
            {
                var name = folder.Split(Path.DirectorySeparatorChar)[^1];
                yield return new(
                    name,
                    name,
                    sourceName,
                    DefinitionType.Folder,
                    folder,
                    SourceType.Local,
                    ImageType.Pak,
                    new[] { new PassThroughPatch(Logger) }
                );
            }
        }
    }

    IEnumerable<ProgramImage> GetSourceFromList(string sourceName, ImageSourceDefinition source, string list)
    {
        var parts =
                list.Split('\n')
                    .Select(l => l.Split(';')
                                    .Select(s => s.Trim())
                                    .ToArray()
                    ).Where(a => a.Length == 2);

        foreach (var line in parts)
        {
            var name = line[^1];
            var path = line[0];

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
            var type = isRemote ? SourceType.Remote : SourceType.Local;

            yield return new(
                name,
                path,
                sourceName,
                source.Type,
                realPath,
                type,
                isNabuFile ? ImageType.Raw : ImageType.Pak,
                new[] { new PassThroughPatch(Logger) }
            );
        }
    }

    async IAsyncEnumerable<ProgramImage> GetImageList(string sourceName, ImageSourceDefinition source)
    {
        if (source.Type is DefinitionType.Folder)
        {
            if (source.NabuRoot is null && source.PakRoot is null) yield break;
            if (source.PakRoot is not null)
            {
                foreach (var channel in GetLocalPakSources(sourceName, source.PakRoot))
                {
                    yield return channel;
                }
            }

            if (source.NabuRoot is not null)
            {
                var files = Directory.GetFiles(source.NabuRoot, "*.nabu");
                foreach (var file in files)
                {
                    var name = file.Split('.')[0].Split(Path.DirectorySeparatorChar)[^1];
                    yield return new(
                        name,
                        name,
                        sourceName,
                        source.Type,
                        file,
                        SourceType.Local,
                        ImageType.Raw,
                        new[] { new PassThroughPatch(Logger) }
                    );
                }
            }
        }
        else
        {
            if (source.ListUrl is null) yield break;
            string list = string.Empty;
            try
            {

                list = State.SourceCache.ContainsKey(sourceName) ?
                            State.SourceCache[sourceName] :
                            State.SourceCache[sourceName] = await Http.GetStringAsync(source.ListUrl);
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                yield break;
            }

            foreach (var channel in GetSourceFromList(sourceName, source, list))
            {
                yield return channel;
            }
        }
    }
    /// <summary>
    /// Refreshes the list of image sources from the current definition.
    /// </summary>
    /// <returns></returns>
    async Task<bool> RefreshSources()
    {
        Log($"Refresh Channel List from Source: {State.Source}");
        if (State.HasChannels) return true;
        if (State.SourceDefinitions is null) return false;
        if (State.Source is null) return false;
        if (State.SourceDefinitions.ContainsKey(State.Source) is false) return false;

        var source = State.SourceDefinitions[State.Source];
        if (source is null) return false;
        State.Sources.Clear();
        await foreach (var channel in GetImageList(State.Source, source))
        {
            Log($"Adding [{channel.Name}] {channel.DisplayName} from {channel.Source}");
            State.Sources.Add(channel.Name, channel);
        }
        Log($"Refreshed ({State.Sources.Count} Channels)");
        return true;
    }
    #endregion

    #region RetroNET

    public async Task<(bool, string)> StorageOpen(short index, string url)
    {
        try
        {
            if (Settings is null) return (false, "Network initialized improperly");

            var response = url.ToLower().StartsWith("http") switch
            {
                true => await Http.GetByteArrayAsync(url),
                false => await File.ReadAllBytesAsync(
                    Path.Combine(Settings.StoragePath, url)
                )
            };
            State.ACPStorage[index] = response;
        }
        catch (Exception ex)
        {
            Warning(ex.Message);
            return (false, ex.Message);
        }

        return (true, string.Empty);
    }

    public int GetResponseSize(short index)
        => State.ACPStorage[index].Length;

    public byte[] StorageGet(short index, int offset, short length)
    {
        var (_, data) = NABU.SliceArray(State.ACPStorage[index], offset, length);
        return data;
    }

    public (bool, string) StoragePut(short index, int offset, params byte[] bytes)
    {
        var data = State.ACPStorage[index];
        var size = offset + bytes.Length;
        if (size < data.Length) size = data.Length;
        var buffer = new byte[size];
        data.CopyTo(buffer, 0);
        bytes.CopyTo(buffer, offset);
        State.ACPStorage[index] = buffer;
        return (true, string.Empty);
    }

    #endregion

    /// <summary>
    /// Sets the initial state of the Network Emulator
    /// </summary>
    /// <param name="settings"></param>
    public void SetState(AdaptorSettings settings)
    {
        Log($"Source: {settings.Source}, Channel: {settings.Channel}");
        State.Source = settings.Source;
        State.Channel = settings.Channel;
        State.ClearCache();
        Task.Run(RefreshSources);
        Settings = settings;
    }

    public void UncachePak(int pak)
    {
        if (State.PakCache.ContainsKey(pak))
            State.PakCache.Remove(pak);
    }

    /// <summary>
    /// Requests a PAK from the Network Emulator
    /// </summary>
    /// <param name="pak">the number of the desired back, starting at 1</param>
    /// <returns></returns>
    public async Task<(ImageType, byte[])> Request(int pak)
    {
        if (Empty(State.Source))
        {
            Warning("NTWRK: No Source Defined");
            return (ImageType.None, ZeroBytes());
        }
        var channel = State.Channel ?? string.Empty;

        if (Empty(channel))
        {
            var nabuName = FormatTriple(pak);
            if (State.Sources.ContainsKey(nabuName))
            {
                Warning($"NTWRK: No channel defined, NABU Image Found: {pak}");
                channel = nabuName;
            }
            else
            {
                Warning($"NTWRK: No Channel or NABU file for {pak}");
                return (ImageType.None, ZeroBytes());
            }
        }

        if (State.HasChannels is false)
        {
            State.ClearCache();
            if (!await RefreshSources())
            {
                Warning("NTWRK: No Channel List");
                return (ImageType.None, ZeroBytes());
            }
        }

        var source = State.Sources[channel];

        if (State.PakCache.ContainsKey(pak))
        {
            return (source.ImageType, State.PakCache[pak]);
        }

        byte[] bytes = ZeroBytes();
        try
        {
            bytes = (source.SourceType, source.ImageType) switch
            {
                (SourceType.Remote, ImageType.Raw) => await Http.GetByteArrayAsync(source.Path),
                (SourceType.Local, ImageType.Raw)  => await File.ReadAllBytesAsync(source.Path),
                (SourceType.Remote, ImageType.Pak)  => await HttpGetPakBytes(source.Path, pak),
                (SourceType.Local, ImageType.Pak)   => await FileGetPakBytes(source.Path, pak),
                _ => ZeroBytes()
            };
        }
        catch (Exception ex)
        {
            Warning(ex.Message);
            return (ImageType.None, ZeroBytes());
        }

        foreach (var patch in source.Patches)
        {
            if (patch.Name is not nameof(PassThroughPatch))
                Log($"NTWRK: Applying Patch: {patch.Name}");

            bytes = await patch.Patch(source, bytes);
        }

        State.PakCache[pak] = bytes;

        return (source.ImageType, bytes);
    }

}