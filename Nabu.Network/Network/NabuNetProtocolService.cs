using Microsoft.Extensions.Logging;
using Nabu.Patching;

namespace Nabu.Network;

/// <summary>
///    This is the backend service replicating functions
///    that would be performed by the Nabu Network.
/// </summary>
public class ProgramImageService : NabuService
{
    readonly HttpClient Http;
    //AdaptorSettings? Settings;
    readonly NetworkState State = new();

    public ProgramImageService(
        ILogger<ProgramImageService> logger,
        HttpClient http,
        List<SourceFolder> sources
    ) : base(logger, new NullAdaptorSettings())
    {
        Http = http;
        State.SourceDefinitions = sources;
        State.Sources = State.SourceDefinitions.ToDictionary(k => k.Name, v => v.Path);
    }

    #region PAK

    async Task<byte[]> HttpGetPakBytes(string url, int pak)
    {
        var filename = NabuLib.PakName(pak);
        url = $"{url}/{filename}.npak";
        var npak = await Http.GetByteArrayAsync(url);
        //Trace($"NPAK Length: {npak.Length}");
        npak = NabuLib.Unpak(npak);
        //Trace($"Segment Length: {npak.Length}");
        return npak;
    }
    async Task<byte[]> FileGetPakBytes(string path)
    {
        //var filename = NabuLib.PakName(pak);
        //path = Path.Join(path, $"{filename}.npak");
        var npak = await File.ReadAllBytesAsync(path);
        Trace($"NPAK Length: {npak.Length}");
        npak = NabuLib.Unpak(npak);
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
    IEnumerable<ProgramImage> GetPakSubFolders(string sourceName, string root)
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
    
    IEnumerable<ProgramImage> GetImageList(string sourceName, string path)
    {
        
        if (path is null) yield break;
        
        var files = Directory.GetFiles(path, "*.npak");
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            yield return new(
                name,
                name,
                sourceName,
                DefinitionType.Folder,
                file,
                SourceType.Local,
                ImageType.Pak,
                new[] { new PassThroughPatch(Logger) }
            );
        }
            
        files = Directory.GetFiles(path, "*.nabu");
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            yield return new(
                name,
                name,
                sourceName,
                DefinitionType.Folder,
                file,
                SourceType.Local,
                ImageType.Raw,
                new[] { new PassThroughPatch(Logger) }
            );
        }
        
        
    }
    /// <summary>
    /// Refreshes the list of image sources from the current definition.
    /// </summary>
    /// <returns></returns>
    Task<bool> RefreshSources()
    {
        return Task.Run(() =>
        {
            Log($"Refresh Channel List from Source: {State.Source}");
            State.Sources = State.SourceDefinitions.ToDictionary(k => k.Name, v => v.Path);
            
            if (State.FoundImages) return true;
            if (State.SourceDefinitions is null) return false;
            if (State.Source is null) return false;
            if (State.Sources.Keys.Contains(State.Source) is false) return false;
            
            var source = State.Sources[State.Source];
            if (source is null)
                return false;

            State.ProgramImages.Clear();
            foreach (var channel in GetImageList(State.Source, source))
            {
                Log($"Adding [{channel.Name}] {channel.DisplayName} from {channel.Source}");
                State.ProgramImages.Add(channel.Name, channel);
            }
            Log($"Refreshed ({State.ProgramImages.Count} Channels)");
            State.LastUpdatedSource = State.Source;
            return true;
        });
    }
    #endregion

    /// <summary>
    /// Sets the initial state of the Network Emulator
    /// </summary>
    /// <param name="settings"></param>
    public void SetState(AdaptorSettings settings)
    {
        Settings = settings;
        Log($"Source: {settings.Source}, Channel: {settings.Image}");
        State.Source = settings.Source;
        State.Image = settings.Image;
        State.ClearCache();
        Task.Run(RefreshSources);
        
    }

    public void UncachePak(int pak)
    {
        if (State.PakCache.ContainsKey(pak))
        {
            Log($"Removing pak {pak} from cache");
            State.PakCache.Remove(pak);
        }
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
            return (ImageType.None, ZeroBytes);
        }
        var image = State.Image ?? string.Empty;

        if (Empty(image))
        {
            var nabuName = FormatTriple(pak);
            var pakName = NabuLib.PakName(pak);
            if (State.ProgramImages.ContainsKey(nabuName))
            {
                Debug($"NTWRK: NABU Image Found: {pak}");
                image = nabuName;
            }
            else if (State.ProgramImages.ContainsKey(pakName))
            {
                Debug($"NTWRK: NABU Image Found: {pak}");
                image = pakName;
            }
            else
            {
                Error($"NTWRK: No Channel or NABU file for {pak}");
                return (ImageType.None, ZeroBytes);
            }
        }

        if (State.FoundImages is false || 
            State.LastUpdatedSource != Settings.Source)
        {
            State.ClearCache();
            if (!await RefreshSources())
            {
                Error("NTWRK: No Channel List");
                return (ImageType.None, ZeroBytes);
            }
        }

        var source = State.ProgramImages[image];

        if (State.PakCache.TryGetValue(pak, out var value))
        {
            return (source.ImageType, value);
        }

        byte[] bytes = ZeroBytes;
        try
        {
            bytes = (source.SourceType, source.ImageType) switch
            {
                (SourceType.Remote, ImageType.Raw) => await Http.GetByteArrayAsync(source.Path),
                (SourceType.Local, ImageType.Raw)  => await File.ReadAllBytesAsync(source.Path),
                //(SourceType.Remote, ImageType.Pak) => await HttpGetPakBytes(source.Path, pak),
                (SourceType.Local, ImageType.Pak)  => await FileGetPakBytes(source.Path),
                _ => ZeroBytes
            };
        }
        catch (Exception ex)
        {
            Warning(ex.Message);
            return (ImageType.None, ZeroBytes);
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