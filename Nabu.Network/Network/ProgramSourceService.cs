using Nabu.Patching;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using static System.Net.WebRequestMethods;

namespace Nabu.Network;

public class ProgramSourceService : NabuBase
{

    HttpProgramRetriever Http { get; }
    FileProgramRetriever File { get; }
    public List<ProgramSource> Sources { get; }
    public Dictionary<ProgramSource, IEnumerable<NabuProgram>> SourceCache { get; } = new();
    Settings Settings { get; }

    Dictionary<(string?, int), byte[]> PakCache { get; } = new();

    public ProgramSourceService(
        Settings settings,
        List<ProgramSource> sources,
        IConsole<ProgramSourceService> console,
        HttpProgramRetriever http,
        FileProgramRetriever file) : base(console)
    {
        Settings = settings;
        Sources = sources;
        Http = http;
        File = file;
        Task.Run(() => RefreshSources());
        Observable.Interval(TimeSpan.FromMinutes(1))
                  .SubscribeOn(ThreadPoolScheduler.Instance)
                  .Subscribe(async _ => await RefreshSources(true));
    }

    public ProgramSource Source(AdaptorSettings settings)
        => Sources.First(s => s.Name.ToLower() == settings.Source?.ToLower());

    public string DiskFolder => Settings.StoragePath;

    public IEnumerable<string> Disks()
    {
        if (!Directory.Exists(DiskFolder))
            Directory.CreateDirectory(DiskFolder);
        
        return Directory.GetFiles(DiskFolder, "*.dsk,*.img");
    }

    public IEnumerable<NabuProgram> Programs(AdaptorSettings settings)
    {
        var source = Source(settings);
        if (SourceCache.TryGetValue(source, out IEnumerable<NabuProgram>? value))
            return value;
        return Array.Empty<NabuProgram>();
    }

    public IEnumerable<NabuProgram> RefreshLocal(ProgramSource source)
    {
        if (source.Path is null || source.SourceType is SourceType.Remote)
            return Array.Empty<NabuProgram>();

        return File.GetImageList(source.Name, source.Path);
    }

    public async Task<IEnumerable<NabuProgram>> RefreshRemote(int pak, string nabuName, string pakName, ProgramSource source)
    {
        if (source.Path is null || source.SourceType is SourceType.Local)
            return Array.Empty<NabuProgram>();

        var (isList, items) = await Http.FoundNabuCaList(source.Name, source.Path);
        if (isList) return items;

        var (isNabu, nabuUrl) = await Http.FoundRaw(source.Path, pak);
        if (isNabu)
        {
            return
                 new NabuProgram[] {
                    new(
                        nabuName,
                        pakName,
                        source.Name,
                        DefinitionType.Folder,
                        nabuUrl,
                        SourceType.Remote,
                        ImageType.Raw,
                        new[] { new PassThroughPatch(Logger) }
                    )
                 };
        }
        else
        {
            var (found, url) = await Http.FoundPak(source.Path, pak);
            if (found)
            {
                return
                    new NabuProgram[] {
                            new(
                                nabuName,
                                nabuName,
                                source.Name,
                                DefinitionType.Folder,
                                url,
                                SourceType.Remote,
                                ImageType.Pak,
                                new[] { new PassThroughPatch(Logger) }
                            )
                    };
            }
        }
        return Array.Empty<NabuProgram>();
    }

    public async Task RefreshSources(bool skipRemote = false)
    {
        foreach (var source in Sources)
        {
            var pak = 1;
            var nabuName = FormatTriple(pak);
            var pakName = NabuLib.PakName(pak);
            var isRemote = HttpProgramRetriever.IsWebSource(source.Path);
            if (!skipRemote && (source.SourceType is SourceType.Remote || isRemote))
            {
                source.SourceType = SourceType.Remote;
                SourceCache[source] = await RefreshRemote(pak, nabuName, pakName, source);
            }
            else if (source.SourceType is SourceType.Local || !isRemote)
            {
                source.SourceType = SourceType.Local;
                SourceCache[source] = RefreshLocal(source);
            }
        }
    }

    public async Task<(ImageType, byte[])> Request(AdaptorSettings settings, int pak)
    {
        if (Empty(settings.Source))
        {
            Warning("NTWRK: No Source Defined");
            return (ImageType.None, ZeroBytes);
        }

        var source = Source(settings);
        var path = source.Path;
        var image = pak switch
        {

            > 1 => FormatTriple(pak),
            _ when Empty(settings.Image) => NabuLib.FormatTriple(1),
            _ => settings.Image!
        };

        if (SourceCache.ContainsKey(source) is false) return (ImageType.None, ZeroBytes);

        var prg = SourceCache[source].FirstOrDefault(p => p.Name == image) ?? SourceCache[source].FirstOrDefault();
        if (prg is null) return (ImageType.None, ZeroBytes);

        if (PakCache.TryGetValue((source.Name.ToLower(), pak), out var value))
        {
            return (prg.ImageType, value);
        }

        Http.Attach(settings);
        File.Attach(settings);

        byte[] bytes = ZeroBytes;
        try
        {
            bytes = (prg.SourceType, prg.ImageType) switch
            {
                (SourceType.Remote, ImageType.Raw) => await Http.GetRawBytes(prg.Path, pak),
                (SourceType.Local, ImageType.Raw) => await File.GetRawBytes(prg.Path, pak, image),
                (SourceType.Remote, ImageType.Pak) => await Http.GetPakBytes(prg.Path, pak),
                (SourceType.Local, ImageType.Pak) => await File.GetPakBytes(prg.Path, pak),
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

        Http.Detach();
        File.Detach();

        PakCache[(source.Name.ToLower(), pak)] = bytes;
        return (prg.ImageType, bytes);
    }

    public void UncachePak(string source, int pak)
    {
        source = source.ToLower();
        if (PakCache.ContainsKey((source, pak)))
        {
            Log($"Removing pak {pak} from cache");
            PakCache.Remove((source, pak));
        }
    }
}
