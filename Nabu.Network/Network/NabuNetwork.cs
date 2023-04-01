using Nabu.Patching;
using Nabu.Services;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Nabu.Network;

public partial class NabuNetwork : NabuBase, INabuNetwork
{
    CachingHttpClient Http { get; }
    FileCache FileCache { get; set; }
    Settings Settings { get; }
    List<ProgramSource> Sources { get; }
    ConcurrentDictionary<ProgramSource, IEnumerable<NabuProgram>> SourceCache { get; } = new();
    ConcurrentDictionary<(AdaptorSettings, ProgramSource, int), byte[]> PakCache { get; } = new();

    //readonly IRepository Database;

    public NabuNetwork(
        IConsole<NabuNetwork> logger,
        Settings settings,
        HttpClient http,
        FileCache cache
    //IRepository database
    ) : base(logger)
    {
        Settings = settings;
        Sources = settings.Sources;
        Http = new(http, logger, cache);
        FileCache = cache;
        //Database = database;

        //Database.Collection<NabuProgram>().EnsureIndex(p => p.Source);

        BackgroundRefresh(RefreshType.All);
        /*
        Observable.Interval(TimeSpan.FromMinutes(1))
            .Subscribe(_ => BackgroundRefresh(RefreshType.Local));

        Observable.Interval(TimeSpan.FromMinutes(30))
            .Subscribe(_ => BackgroundRefresh(RefreshType.Remote));
        */
    }
    public ProgramSource Source(AdaptorSettings settings)
        => Sources.First(s => s.Name.ToLower() == settings.Source?.ToLower());

    public IEnumerable<NabuProgram> Programs(AdaptorSettings settings)
    {
        var source = Source(settings);
        //var programs = Database.Collection<NabuProgram>().Find(p => p.Source == source.Name);
        //if (programs.Any()) return programs;
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


    public void BackgroundRefresh(RefreshType refresh)
    {
        Task.Run(() => {
            RefreshSources(refresh);
            if (refresh.HasFlag(RefreshType.Remote))
                GC.Collect();
        });
    }

    async Task<(bool, NabuProgram[])> IsKnownListType(string name, string path)
    {
        var tasks = new Task<(bool, NabuProgram[])>[] {
            IsNabuCaList(name, path),
            IsNabuNetworkList(name, path)
        };
        
        foreach (var task in tasks)
        {
            var (isList, programs) = await task;
            if (isList) return (isList, programs);
        }
        return (false, Array.Empty<NabuProgram>());
    }


    void RefreshSources(RefreshType refresh = RefreshType.All)
    {
        if (refresh.HasFlag(RefreshType.Remote))
            Log($"Refreshing remote sources");

        foreach (var source in Sources)
        {
            Task.Run(async () =>
            {
                var isRemote = IsWebSource(source.Path);

                var checkRemote = refresh.HasFlag(RefreshType.Remote);
                var checkLocal = refresh.HasFlag(RefreshType.Local);

                if (isRemote) source.SourceType = SourceType.Remote;
                else source.SourceType = SourceType.Local;

                if (checkRemote && source.SourceType is SourceType.Remote)
                {
                    var programs = new List<NabuProgram>();
                    source.SourceType = SourceType.Remote;
                    var (isList, items) = await IsKnownListType(source.Name, source.Path);
                    var (isPak, pakUrl, type) = await IsPak(source.Path, 1);
                    if (isList)
                    {
                        programs.AddRange(items);
                    }
                    else if (isPak)
                    {
                        programs.Add(new(
                            "Cycle Menu",
                            Constants.CycleMenuPak,
                            source.Name,
                            pakUrl,
                            source.SourceType,
                            type,
                            new[] { new PassThroughPatch(Logger) },
                            true
                        ));
                    }
                    else if (IsNabu(source.Path))
                    {
                        var name = Path.GetFileName(pakUrl);
                        programs.Add(new(
                            source.Name,
                            name,
                            source.Name,
                            source.Path,
                            source.SourceType,
                            ImageType.Raw,
                            new[] { new PassThroughPatch(Logger) }
                        ));
                    }
                    SourceCache[source] = programs;
                }
                else if (checkLocal && source.SourceType is SourceType.Local)
                {
                    if (Directory.Exists(source.Path) is false) return;
                    var programs = new List<NabuProgram>();
                    source.SourceType = SourceType.Local;


                    var files = Directory.GetFiles(source.Path);

                    var (supported, menuPak, type) = ContainsPak(files);

                    if (supported && (source.EnableExploitLoader is false && menuPak is not null))
                    {
                        programs.Add(new(
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
                    SourceCache[source] = programs;
                }
                //Database.Collection<NabuProgram>().DeleteMany(p => p.Source == source.Name);
                //Database.Collection<NabuProgram>().InsertBulk(programs);
            });
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

        //var programs = Database.Collection<NabuProgram>().Find(p => p.Source == source.Name);

        if (SourceCache.ContainsKey(source) is false)
            return (ImageType.None, ZeroBytes);

        //if (programs.Any() is false) 
        //    return (ImageType.None, ZeroBytes);

        if (PakCache.TryGetValue((settings, source, pak), out var value))
        {
            return (ImageType.Raw, value);
        }

        var path = source.Path;
        //var cycleFile = false;
        var image = pak switch
        {
            0x191 when source.EnableExploitLoader => settings.Image!,
            > 1 => FormatTriple(pak),
            1 when Empty(settings.Image) || source.EnableExploitLoader => FormatTriple(1),
            1 => settings.Image!,
            _ => null
        };

        if (image == null)
            return (ImageType.None, ZeroBytes);

        var prg = SourceCache[source].FirstOrDefault(p => p.Name == image);
        //var prg = programs.FirstOrDefault(p => p.Name == image) ?? 
        //          programs.FirstOrDefault();

        if (prg is null && pak > Constants.CycleMenuNumber)
        {
            prg = SourceCache.SelectMany(kv => kv.Value).FirstOrDefault(p => p.Name == image);  
        }

        if (prg is null)
            return (ImageType.None, ZeroBytes);

        if (prg.IsPakMenu && pak > Constants.CycleMenuNumber)
        {
            var ext = prg.ImageType switch
            {
                ImageType.Raw => Constants.NabuExtension,
                ImageType.Pak => Constants.PakExtension,
                ImageType.EncryptedPak => Constants.EncryptedPakExtension,
                _ => Constants.PakExtension
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
                SourceType.Local => await FileCache.GetFile(path),
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
        
        var type = source.EnableExploitLoader ? ImageType.QuirkLoadedPak : prg.ImageType;
        Log($"Type: {prg.ImageType}, Size: {bytes.Length}, Path: {path}");

        PakCache[(settings, source, pak)] = bytes;
        return (type, bytes);
    }   

    public void UncachePak(AdaptorSettings settings, int pak)
    {
        var source = Source(settings);
        if (PakCache.ContainsKey((settings, source, pak)))
        {
            Debug($"Removing pak {pak} from transfer cache");
            PakCache.TryRemove((settings, source, pak), out _);
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

            (_, found, cached, _) = await Http.GetUriStatus($"{url}/{FormatTriple(pak)}{Constants.NabuExtension}");
            if (found || cached)
                return (true, url, ImageType.Raw);

            (_, found, cached, _) = await Http.GetUriStatus($"{url}/{FormatTriple(pak)}{Constants.PakExtension}");
            if (found || cached)
                return (true, url, ImageType.Pak);

            (_, found, cached, _) = await Http.GetUriStatus($"{url}/{NabuLib.PakName(pak)}{Constants.EncryptedPakExtension}");
            if (found || cached)
                return (false, url, ImageType.EncryptedPak); //Encrypted pak support is disabled.

            return (false, url, ImageType.None);
        }

        (_, found, cached, _) = await Http.GetUriStatus(url);
        return (found || cached, url, type);
    }

    protected async Task<(bool, NabuProgram[])> IsNabuNetworkList(string source, string uri)
    {
        if (!uri.EndsWith(".xml") && !uri.Contains("nabunetwork.com")) { return (false, Array.Empty<NabuProgram>());}
        var (shouldDownload, found, cached, _) = await Http.GetUriStatus(uri);
        if (!found && !cached) { return (false, Array.Empty<NabuProgram>()); }

        var xml = await Http.GetString(uri);
        var list = new XmlDocument();
        list.LoadXml(xml);
        var cycleNodes = list.DocumentElement?.SelectNodes("Cycle");
        if (cycleNodes is null) { return (false, Array.Empty<NabuProgram>()); }
        var progs = new List<NabuProgram>();
        foreach (XmlNode cycleNode in cycleNodes)
        {
            var targetType = cycleNode["TargetType"]?.InnerText;
            if (targetType == "Cycle") continue;
            var name = cycleNode["Name"]?.InnerText ?? "Unnamed Program";
            var url = cycleNode["Url"]?.InnerText ?? string.Empty;
            progs.Add(new(
                name,
                name,
                source,
                url,
                SourceType.Remote,
                ImageType.Raw,
                new[] { new PassThroughPatch(Logger) }, 
                false
            ));
        }
        return (true, progs.ToArray());
    }

    protected async Task<(bool, NabuProgram[])> IsNabuCaList(string source, string uri)
    {

        if (!uri.EndsWith(".txt")) { return (false, Array.Empty<NabuProgram>()); }

        var (shouldDownload, found, cached, _) = await Http.GetUriStatus(uri);
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
