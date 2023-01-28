using Microsoft.Extensions.Logging;
using Nabu.Patching;
using static System.Net.Mime.MediaTypeNames;

namespace Nabu.Network;


public class NabuNetService : NabuService
{
    HttpProgramRetriever Http { get; }
    FileProgramRetriever File { get; }
    Settings SimulationSettings {get;}
    List<ProgramSource> Sources => SimulationSettings.Sources;
    public Dictionary<ProgramSource,IEnumerable<NabuProgram>> SourceCache { get; } = new();
    Dictionary<int, Memory<byte>> ProgramCache { get; } = new();
    Dictionary<(string?, int), byte[]> PakCache { get; } = new();

    public NabuNetService(
        ILogger<NabuNetService> logger,
        HttpProgramRetriever http,
        FileProgramRetriever file,
        Settings settings
    ) : base(logger, new NullAdaptorSettings())
    {
        Http = http;
        File = file;
        SimulationSettings = settings;
        
        Task.Run(() => RefreshSources());
    }

    bool IsNabu(string path) => path.EndsWith(".nabu");
    bool IsPak(string path) => path.EndsWith(".pak") || IsEncryptedPak(path);
    bool IsEncryptedPak(string path) => path.EndsWith(".npak");

    public void Attach(AdaptorSettings settings)
    {
        Settings = settings;
        Log($"Source: {settings.Source}");
        if (settings.Image is not null)
            Log($"Program: {settings.Image}");
    }

    public void ClearCache()
    {
        SourceCache.Clear();
        ProgramCache.Clear();
    }

    /// <summary>
    /// Sets the initial state of the Network Emulator
    /// </summary>
    /// <param name="settings"></param>
    public void UncachePak(string source, int pak)
    {
        source = source.ToLower();
        if (PakCache.ContainsKey((source, pak)))
        {
            Log($"Removing pak {pak} from cache");
            PakCache.Remove((source, pak));
        }
    }

    public ProgramSource Source(AdaptorSettings settings)
        => Sources.First(s => s.Name.ToLower() == settings.Source?.ToLower());

    public IEnumerable<NabuProgram> Programs(AdaptorSettings settings)
        => SourceCache[Source(settings)];

    /// <summary>
    /// Refreshes the list of image sources from the current definition.
    /// </summary>
    /// <returns></returns>
    public async Task RefreshSources()
    {
        ClearCache();
        foreach (var source in Sources)
        {
            var pak = 1;
            var nabuName = FormatTriple(pak);
            var pakName = NabuLib.PakName(pak);
            if (HttpProgramRetriever.IsWebSource(source.Path))
            {
                if (await Http.FoundRaw(source.Path, pak))
                {
                    SourceCache.Add(
                        source,
                        new NabuProgram[] {
                            new(
                                nabuName,
                                pakName,
                                Settings.Source!,
                                DefinitionType.Folder,
                                source.Path,
                                SourceType.Remote,
                                ImageType.Raw,
                                new[] { new PassThroughPatch(Logger) }
                            )
                        }
                    );
                }
                else
                {
                    var (found, url) = await Http.FoundPak(source.Path, pak);
                    if (found)
                    {
                        SourceCache.Add(
                            source,
                            new NabuProgram[] {
                            new(
                                nabuName,
                                nabuName,
                                Settings.Source!,
                                DefinitionType.Folder,
                                url,
                                SourceType.Remote,
                                ImageType.Pak,
                                new[] { new PassThroughPatch(Logger) }
                            )
                            }
                        );
                    }
                }
            }
            else SourceCache.Add(source, new FileSystemObserver(Logger, source));
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
        var source = Source(Settings);
        var path = source.Path;
        var image = pak switch {

            > 1 => FormatTriple(pak),
            _ => Empty(Settings.Image) ? NabuLib.FormatTriple(1) : Settings.Image!
        };

        if (SourceCache.ContainsKey(source) is false) return (ImageType.None, ZeroBytes); 

        var prg = SourceCache[source].FirstOrDefault(p => p.Name == image) ?? SourceCache[source].FirstOrDefault();
        if (prg is null) return (ImageType.None, ZeroBytes);

        if (PakCache.TryGetValue((source.Name.ToLower(), pak), out var value))
        {
            return (prg.ImageType, value);
        }

        byte[] bytes = ZeroBytes;
        try
        {
            bytes = (prg.SourceType, prg.ImageType) switch
            {
                (SourceType.Remote, ImageType.Raw) => await Http.GetRawBytes(path, pak),
                (SourceType.Local, ImageType.Raw)  => await File.GetRawBytes(path, pak, image),
                (SourceType.Remote, ImageType.Pak) => await Http.GetPakBytes(path, pak),
                (SourceType.Local, ImageType.Pak)  => await File.GetPakBytes(path, pak),
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

        PakCache[(source.Name.ToLower(), pak)] = bytes;
        return (prg.ImageType, bytes);
    }

}