using Microsoft.Extensions.Logging;
using Nabu.Patching;

namespace Nabu.Network;

/// <summary>
///    This is the backend service replicating functions
///    that would be performed by the Nabu Network.
/// </summary>
public class ProgramImageService : NabuService
{
    HttpClient Http { get; }
    Dictionary<(string, int), byte[]> PakCache { get; } = new();
    Dictionary<string, NabuProgram> Programs { get; } = new();
    List<SourceFolder> Sources { get; set; } = new();
    string LastUpdatedSource { get; set; } = string.Empty;
    bool IsWebSource(string path) => path.StartsWith("http");

    SourceFolder Source() => Sources.First(s => s.Name.ToLower() == Settings.Source?.ToLower());
    public string LastImage { get; private set; } = string.Empty;

    public ProgramImageService(
        ILogger<ProgramImageService> logger,
        HttpClient http,
        List<SourceFolder> sources
    ) : base(logger, new NullAdaptorSettings())
    {
        Http = http;
        Sources = sources;
        
    }

    #region File
    async Task<byte[]> FileGetPakBytes(string path, int pak)
    {
        var filename = NabuLib.PakName(pak);
        path = Path.Join(path, $"{filename}.npak");
        var npak = await File.ReadAllBytesAsync(path);
        //Trace($"NPAK Length: {npak.Length}");
        npak = NabuLib.Unpak(npak);
        //Trace($"Segment Length: {npak.Length}");
        return npak;
    }

    async Task<byte[]> FileGetRawBytes(string path, int pak)
    {
        var filename = FormatTriple(pak);
        path = Path.Join(path, $"{filename}.nabu");
        var buffer = await File.ReadAllBytesAsync(path);
        return buffer;
    }

    #endregion

    #region HTTP

    async Task<byte[]> HttpGetPakBytes(string url, int pak)
    {
        var filename = NabuLib.PakName(pak);
        url = $"{url}/{filename}.npak";
        var npak = await Http.GetByteArrayAsync(url);
        Trace($"NPAK Length: {npak.Length}");
        npak = NabuLib.Unpak(npak);
        Trace($"Segment Length: {npak.Length}");
        return npak;
    }
    

    async Task<byte[]> HttpGetRawBytes(string url, int pak)
    {
        var filename = NabuLib.FormatTriple(pak);
        url = $"{url}/{filename}.nabu";
        var buffer = await Http.GetByteArrayAsync(url);
        return buffer;
    }
    async Task<bool> HttpRawGetHead(string url, int pak)
    {
        var filename = NabuLib.FormatTriple(pak);
        url = $"{url}/{filename}.nabu";
        return await HttpGetHead(url);
    }

    async Task<bool> HttpPakGetHead(string url, int pak)
    {
        var filename = NabuLib.PakName(pak);
        url = $"{url}/{filename}.npak";
        return await HttpGetHead(url);
    }

    async Task<bool> HttpGetHead(string url)
    {
        var response = await Http.SendAsync(new(HttpMethod.Head, url));
        return response.IsSuccessStatusCode;
    }
    #endregion
    
    #region Source List
    
    IEnumerable<NabuProgram> GetImageList(string sourceName, string path)
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
            if (LastUpdatedSource == Settings.Source)
                return true;

            Log($"Refresh Channel List from Source: {Settings.Source}");

            if (Settings.Source is null)
                return false;
                        
            var path = Source().Path;
            if (path is null)
            {
                Error($"Source {Settings.Source} is not defined");
                return false;
            }

            if (IsWebSource(path) is false) { 
                foreach (var channel in GetImageList(Settings.Source, path))
                {
                    Log($"Adding [{channel.Name}] {channel.DisplayName} from {channel.Source}");
                    Programs.Add(channel.Name, channel);
                }
            }

            LastUpdatedSource = Settings.Source;
            return true;
        });
    }
    #endregion

    public void ClearCache()
    {
        PakCache.Clear();
        LastUpdatedSource = string.Empty;
        Programs.Clear();
    }

    /// <summary>
    /// Sets the initial state of the Network Emulator
    /// </summary>
    /// <param name="settings"></param>
    public void SetState(AdaptorSettings settings)
    {
        Settings = settings;
        Log($"Source: {settings.Source}, Channel: {settings.Image}");
        Task.Run(RefreshSources);
        
    }

    public void UncachePak(string image, int pak)
    {
        if (PakCache.ContainsKey((image, pak)))
        {
            Log($"Removing pak {pak} from cache");
            PakCache.Remove((image, pak));
        }
    }

 

    /// <summary>
    /// Requests a PAK from the Network Emulator
    /// </summary>
    /// <param name="pak">the number of the desired back, starting at 1</param>
    /// <returns></returns>
    public async Task<(ImageType, byte[])> Request(int pak)
    {
        if (Empty(Settings.Source))
        {
            Warning("NTWRK: No Source Defined");
            return (ImageType.None, ZeroBytes);
        }
        
        var source = Source().Path;
        var image = Settings.Image ?? string.Empty;
        
        if (Empty(image))
        {
            var nabuName = FormatTriple(pak);
            var pakName = NabuLib.PakName(pak);
                        
            if (Programs.ContainsKey(nabuName))
            {
                Debug($"NTWRK: NABU Image Found: {pak}");
                image = nabuName;
            }
            else if (Programs.ContainsKey(pakName))
            {
                Debug($"NTWRK: NABU Image Found: {pak}");
                image = pakName;
            }
            else if (IsWebSource(source))
            {
                if (await HttpRawGetHead(source, pak))
                {
                    Programs[nabuName] =
                        new NabuProgram(
                            image,
                            image,
                            Settings.Source!,
                            DefinitionType.Folder,
                            source,
                            SourceType.Remote,
                            ImageType.Raw,
                            new[] { new PassThroughPatch(Logger) }
                        );
                    image = nabuName;
                }
                else if (await HttpPakGetHead(source, pak))
                {
                    Programs[pakName] =
                        new NabuProgram(
                            image,
                            image,
                            Settings.Source!,
                            DefinitionType.Folder,
                            source,
                            SourceType.Remote,
                            ImageType.Pak,
                            new[] { new PassThroughPatch(Logger) }
                        );
                    image = pakName;
                }
            }

            if (Empty(image))
            {
                Error($"NTWRK: No Channel or NABU file for {pak}");
                return (ImageType.None, ZeroBytes);
            }
        }
        
                
        if (Programs.Count > 0)
        {
            if (!await RefreshSources())
            {
                Error("NTWRK: No Channel List");
                return (ImageType.None, ZeroBytes);
            }
        }
        
        var prg = Programs[image];

        if (PakCache.TryGetValue((image, pak), out var value))
        {
            return (prg.ImageType, value);
        }

        byte[] bytes = ZeroBytes;
        try
        {
            bytes = (prg.SourceType, prg.ImageType) switch
            {
                (SourceType.Remote, ImageType.Raw) => await HttpGetRawBytes(source, pak),
                (SourceType.Local, ImageType.Raw)  => await FileGetRawBytes(source, pak),
                (SourceType.Remote, ImageType.Pak) => await HttpGetPakBytes(source, pak),
                (SourceType.Local, ImageType.Pak)  => await FileGetPakBytes(source, pak),
                _ => ZeroBytes
            };
        }
        catch (Exception ex)
        {
            Warning(ex.Message);
            return (ImageType.None, ZeroBytes);
        }

        foreach (var patch in prg.Patches)
        {
            if (patch.Name is not nameof(PassThroughPatch))
                Log($"NTWRK: Applying Patch: {patch.Name}");

            bytes = await patch.Patch(prg, bytes);
        }

        PakCache[(image, pak)] = bytes;
        LastImage = image;
        return (prg.ImageType, bytes);
    }

}