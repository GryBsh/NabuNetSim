using Nabu.Patching;
using Nabu.Services;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Nabu.Network;


public partial class NabuNetwork : NabuBase
{
    HttpCache Http { get; }
    FileCache FileCache { get; set; }
    Settings Settings { get; }
    List<ProgramSource> Sources { get; }
    Dictionary<ProgramSource, IEnumerable<NabuProgram>> SourceCache { get; } = new();
    Dictionary<(AdaptorSettings, ProgramSource, int), byte[]> PakCache { get; } = new();
    public NabuNetwork(
        IConsole<NabuNetwork> logger,
        Settings settings, 
        HttpClient http,
        FileCache cache
    ) : base(logger)
    {
        Settings = settings;
        Sources = settings.Sources; 
        Http = new (http, logger, cache);
        FileCache = cache;

        BackgroundRefresh(RefreshType.All);

        Observable.Interval(TimeSpan.FromMinutes(1))
            .Subscribe(_ => BackgroundRefresh(RefreshType.Local));

        Observable.Interval(TimeSpan.FromMinutes(30))
            .Subscribe(_ => BackgroundRefresh(RefreshType.Remote));
    }

    

    public ProgramSource Source(AdaptorSettings settings)
        => Sources.First(s => s.Name.ToLower() == settings.Source?.ToLower());

    public IEnumerable<NabuProgram> Programs(AdaptorSettings settings)
    {
        var source = Source(settings);
        if (SourceCache.TryGetValue(source, out IEnumerable<NabuProgram>? value))
            return value;
        return Array.Empty<NabuProgram>();
    }

    (bool, ImageType) IsSupportedType(string file)
    {
        var ext = Path.GetExtension(file);
        return ext switch
        {
            _ when IsRawPak(file) => (true, ImageType.Raw),
            Constants.PakExtension => (true, ImageType.Pak),
            Constants.EncryptedPakExtension => (true, ImageType.EncryptedPak),
            _ => (false, ImageType.None)
        };
    }

    (bool, string, ImageType) ContainsPak(string[] files)
    {
        var encryptedMenuPakName = NabuLib.PakName(Constants.CycleMenuNumber);


        foreach (var f in files)
        {
            var filename = Path.GetFileNameWithoutExtension(f);

            if (filename is Constants.CycleMenuPak)
            {
                var (supported, type) = IsSupportedType(f);
                if (!supported) continue;
                return (supported, f, type);
            }
           
            if (filename == encryptedMenuPakName)
            {
                var (supported, type) = IsSupportedType(f);
                if (!supported) continue;
                return (supported, f, type);
            }
        }
        return (false, string.Empty, ImageType.None);
    }


    async void BackgroundRefresh(RefreshType refresh)
    {
        await RefreshSources(refresh);
        GC.Collect();
    }

    public async Task RefreshSources(RefreshType refresh = RefreshType.All)
    {
        if (refresh.HasFlag(RefreshType.Remote))
            Logger.Write($"Refreshing remote sources");

        foreach (var source in Sources)
        {
            
            var isRemote = IsWebSource(source.Path);
            
            var checkRemote = refresh.HasFlag(RefreshType.Remote);
            var checkLocal = refresh.HasFlag(RefreshType.Local);

            if (isRemote) source.SourceType = SourceType.Remote;
            else source.SourceType = SourceType.Local;

            if (checkRemote && source.SourceType is SourceType.Remote)
            {
                source.SourceType = SourceType.Remote;
                var (isList, items) = await IsNabuCaList(source.Name, source.Path);
                if (isList)
                {
                    SourceCache[source] = items;
                    continue;
                }
                var (isPak, pakUrl, type) = await IsPak(source.Path, 1);
                if (isPak)
                {
                    SourceCache[source] = new NabuProgram[] { new(
                        "Cycle Menu",
                        Constants.CycleMenuPak,
                        source.Name,
                        pakUrl,
                        source.SourceType,
                        type,
                        new[] { new PassThroughPatch(Logger) },
                        true
                    )};
                    continue;
                }
                if (IsNabu(source.Path))
                {
                    var name = Path.GetFileName(pakUrl);
                    SourceCache[source] = new NabuProgram[] { new(
                        source.Name,
                        name,
                        source.Name,
                        source.Path,
                        source.SourceType,
                        ImageType.Raw,
                        new[] { new PassThroughPatch(Logger) }
                    )};
                }
            }
            else if (checkLocal && source.SourceType is SourceType.Local)
            {
                source.SourceType = SourceType.Local;
                if (Directory.Exists(source.Path) is false) continue;

                var files = Directory.GetFiles(source.Path);
                var programs = new List<NabuProgram>();
                var (supported, menuPak, type) = ContainsPak(files);
                
                if (supported && menuPak is not null)
                {   
                    programs.Add(new (
                        "Cycle Menu",
                        Constants.CycleMenuPak,
                        source.Name,
                        menuPak,
                        source.SourceType,
                        type,
                        new[] { new PassThroughPatch(Logger) },
                        true
                    ));
                    files = files.Except(new[] { menuPak }).ToArray();
                }
                
                files = files.Where(
                    f => Path.GetExtension(f) is Constants.NabuExtension
                ).ToArray();

                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    programs.Add(new(
                        name,
                        name,
                        source.Name,
                        file,
                        source.SourceType,
                        ImageType.Raw,
                        new[] { new PassThroughPatch(Logger) }
                    ));
                }

                SourceCache[source] = programs.ToArray();
            }
        }
    }

    public async Task<(ImageType, byte[])> Request(AdaptorSettings settings, int pak)
    {
        if (Empty(settings.Source))
        {
            Warning("No Source Defined");
            return (ImageType.None, ZeroBytes);
        }

        var source = Source(settings);
        if (SourceCache.ContainsKey(source) is false) 
            return (ImageType.None, ZeroBytes);

        if (PakCache.TryGetValue((settings, source, pak), out var value))
        {
            return (ImageType.Raw, value);
        }

        var path = source.Path;
        var image = pak switch
        {

            > 1 => FormatTriple(pak),
            1 when Empty(settings.Image) => NabuLib.FormatTriple(1),
            1 => settings.Image!,
            _ => null
        };

        if (image == null) 
            return (ImageType.None, ZeroBytes);

        var prg = SourceCache[source].FirstOrDefault(p => p.Name == image) ?? 
                  SourceCache[source].FirstOrDefault();
        
        if (prg is null) 
            return (ImageType.None, ZeroBytes);

        if (prg.IsPakMenu && pak > Constants.CycleMenuNumber)
        {
            var ext = prg.ImageType switch
            {
                ImageType.Raw           => Constants.NabuExtension,
                ImageType.Pak           => Constants.PakExtension,
                ImageType.EncryptedPak  => Constants.EncryptedPakExtension,
            };
            var name = prg.ImageType switch
            {
                ImageType.EncryptedPak => NabuLib.PakName(pak),
                _ => FormatTriple(pak)
            };
            path = $"{path}/{name}.{ext}";
        }
        else path = prg.Path;


        byte[] bytes = ZeroBytes;
        try
        {
            bytes = prg.SourceType switch
            {
                SourceType.Remote => await Http.GetBytes(path),
                SourceType.Local => await FileCache.CacheFile(Logger, path),
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

        Log($"Type: {prg.ImageType} Size: {bytes.Length} Path: {path}");

        PakCache[(settings, source, pak)] = bytes;
        return (prg.ImageType, bytes);
    }

    public void UncachePak(AdaptorSettings settings, int pak)
    {
        var source = Source(settings);
        if (PakCache.ContainsKey((settings, source, pak)))
        {
            Log($"Removing pak {pak} from cache");
            PakCache.Remove((settings, source, pak));
        }
    }

    #region URLs
    protected static bool IsWebSource(string path) => path.Contains("://");
    protected static bool IsNabu(string path) => path.EndsWith(Constants.NabuExtension);
    protected static bool IsRawPak(string path) => PakFile().IsMatch(path);
    protected static bool IsPak(string path) => path.EndsWith(Constants.PakExtension);
    protected static bool IsEncryptedPak(string path) => path.EndsWith(Constants.EncryptedPakExtension);                                          

    

    #endregion

    #region Http Location
    protected async Task<(bool, string, ImageType)> IsPak(string url, int pak)
    {
        url = url.TrimEnd('/');
       
        var type = url switch
        {
            _ when IsRawPak(url) => ImageType.Raw,
            _ when IsPak(url) => ImageType.Pak,
            _ when IsEncryptedPak(url) => ImageType.EncryptedPak,
            _ => ImageType.None
        };
        bool found, cached = false;

        if (type is ImageType.None)
        {
           
            (_, found, cached) = await Http.CanGet($"{url}/{FormatTriple(pak)}{Constants.NabuExtension}");
            if (found || cached)
                return (true, url, ImageType.Raw);

            (_, found, cached) = await Http.CanGet($"{url}/{FormatTriple(pak)}{Constants.PakExtension}");
            if (found || cached)
                return (true, url, ImageType.Pak);

            (_, found, cached) = await Http.CanGet($"{url}/{NabuLib.PakName(pak)}{Constants.EncryptedPakExtension}");
            if (found || cached)
                return (false, url, ImageType.EncryptedPak); //Encrypted pak support is disabled.

            return (false, url, ImageType.None);
        }

        (_, found, cached) = await Http.CanGet(url);
        return (found || cached, url, type);
    }

    protected async Task<(bool, NabuProgram[])> IsNabuCaList(string source, string uri)
    {

        if (!uri.EndsWith(".txt")) { return (false, Array.Empty<NabuProgram>()); }

        var (shouldDownload, found, cached) = await Http.CanGet(uri);
        if (!found && !cached) { return (false, Array.Empty<NabuProgram>()); }
        
        var lines = (await Http.GetString(uri)).Split('\n');
        var progs = new List<NabuProgram>();
        foreach (var line in lines)
        {
            if (line.StartsWith('!') || line.StartsWith(':')) continue;

            var parts = line.Split(';');
            var name = parts[0].Trim();
            var isNabu = name.EndsWith(".nabu");
            var url = $"https://cloud.nabu.ca/HomeBrew/titles/{name}";
            if (isNabu is false)
                continue;

            var displayName = parts[1].Trim();

            progs.Add(new(
                displayName,
                name,
                source,
                url,
                SourceType.Remote,
                ImageType.Raw,
                new[] { new PassThroughPatch(Logger) }
            ));
        }
        return (true, progs.ToArray());
    }

    [GeneratedRegex(".*/d{6}.nabu")]
    private static partial Regex PakFile();
    #endregion

}
