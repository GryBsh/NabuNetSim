using Gry;
using Gry.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nabu.Settings;
using Nabu.Sources;
using Napa;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Data;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Nabu.Network;

public partial class NabuNetwork : NabuBase, INabuNetwork
{
    public const string HeadlessSourceName = "headless";

    public NabuNetwork(
        ILogger<NabuNetwork> logger,
        HttpClient http,
        IFileCache cache,
        SourceService sources,
        IPackageManager packages,
        StorageService storage,
        GlobalSettings settings,
        IOptions<CacheOptions> cacheOptions
    ) : base(logger)
    {
        Settings = settings;
        Sources = sources;
        Http = new(http, logger, cache, cacheOptions.Value);
        FileCache = cache;
        Packages = packages;
        Storage = storage;
        DefaultPatches = [new PassThroughPatch(Logger)];
        //RefreshSources(RefreshType.All);
        
    }

    private static ConcurrentDictionary<(AdaptorSettings, ProgramSource, int), Memory<byte>> PakCache { get; } = new();
    private static ConcurrentDictionary<ProgramSource, IEnumerable<NabuProgram>> SourceCache { get; } = new();
    private IFileCache FileCache { get; set; }
    private HttpCache Http { get; }
    private IPackageManager Packages { get; }
    private SourceService Sources { get; }
    private StorageService Storage { get; }
    private GlobalSettings Settings { get; }
    private SemaphoreSlim UpdateLock { get; } = new(1, 1);
    public IEnumerable<NabuProgram> Programs(AdaptorSettings settings)
    {
        var source = Source(settings);
        if (source is null)
            return Array.Empty<NabuProgram>();

        return Programs(source);
    }

    public IEnumerable<NabuProgram> Programs(string name)
    {
        var source = Source(name);
        if (source is null)
            return Array.Empty<NabuProgram>();

        return Programs(source);
    }

    public IEnumerable<NabuProgram> Programs(ProgramSource? source)
    {        if (source is null)            return [];
        if (SourceCache.TryGetValue(source, out IEnumerable<NabuProgram>? value))
            return value;

        return Array.Empty<NabuProgram>();
    }

    public async Task<(ImageType, Memory<byte>)> Request(AdaptorSettings settings, int pak)
    {
        if (Empty(settings.Source))
        {
            Warning("No Source Defined");
            return (ImageType.None, Array.Empty<byte>());
        }

        var source = Source(settings);
        if (source is null)
            return (ImageType.None, Array.Empty<byte>());

        if (SourceCache.ContainsKey(source) is false)
            return (ImageType.None, Array.Empty<byte>());

        if (PakCache.TryGetValue((settings, source, pak), out var value))
        {
            return (ImageType.Raw, value);
        }

        var path = source.Path;

        //var program = Empty(settings.Program) ?
        //                SourceCache[source].FirstOrDefault(p => p.Name == settings.Program) :
        //                null;

        var targetProgram = pak switch
        {
            1 when Empty(settings.Program) || source.EnableExploitLoader => FormatTriple(1),
            1 => settings.Program!,
            0x191 when source.EnableExploitLoader => settings.Program!,
            > 1 => FormatTriple(pak),
            _ => null
        };

        if (targetProgram == null)
            return (ImageType.None, Array.Empty<byte>());

        var program = SourceCache[source].FirstOrDefault(p => p.Name == targetProgram);

        //BECAUSE: A package using the exploit loader should not have to bundle a menu.
        if (program is null /* && pak > Constants.CycleMenuNumber */)
        {
            program = SourceCache.SelectMany(kv => kv.Value).FirstOrDefault(p => p.Name == targetProgram);
            if (program is not null)
                Logger.LogWarning($"Source {program.Source} was used for {targetProgram}");
        }

    

        if (program is null)
            return (ImageType.None, Array.Empty<byte>());
        /*
        if (program.UseCPMDirect && Settings.CPMSource is not null)
        {
            source = Source(Settings.CPMSource)!;
            program = SourceCache[source].FirstOrDefault(p => p.Name == Settings.CPMProgram);
            if (program is not null)
                path = Path.Combine(source.Path, program.Path);
            else
            {
                return (ImageType.None, Array.Empty<byte>());
            }
            Path.Combine(Settings.StoragePath, "A0", "PROFILE.SUB");
        }*/
        //else 
        if (program.IsPakMenu && pak > Constants.CycleMenuNumber)
        {
            var ext = program.ImageType switch
            {
                ImageType.Raw => Constants.NabuExtension,
                ImageType.Pak => Constants.PakExtension,
                ImageType.EncryptedPak => Constants.EncryptedPakExtension,
                _ => Constants.PakExtension
            };
            var name = program.ImageType switch
            {
                ImageType.EncryptedPak => NabuLib.PakName(pak),
                _ => FormatTriple(pak)
            };
            path = $"{path}/{name}.{ext}";
        }
        else if (source.SourceType is SourceType.Package)
        {
            path = Path.Combine(source.Path, program.Path);
        }
        else path = program.Path;

        Memory<byte> bytes = Array.Empty<byte>();
        try
        {
            bytes = program.SourceType switch
            {
                SourceType.Remote => await Http.GetBytes(path),
                SourceType.Local => await FileCache.GetBytes(path),
                _ => Array.Empty<byte>()
            };
        }
        catch (Exception ex)
        {
            Warning(ex.Message);
            return (ImageType.None, Array.Empty<byte>());
        }

        foreach (var patch in program.Patches)
        {
            if (patch.Name is not nameof(PassThroughPatch))
                Log($"Applying Patch: {patch.Name}");

            bytes = await patch.Patch(program, bytes.ToArray());
        }

        var type = source.EnableExploitLoader ? ImageType.ExploitLoaded : program.ImageType;
        Log($"Type: {program.ImageType}, Size: {bytes.Length}, Path: {path}");

        PakCache[(settings, source, pak)] = bytes;

        return (type, bytes);
    }

    public ProgramSource? Source(AdaptorSettings settings)
        => Source(settings.Source);

    public ProgramSource? Source(string? name)
        => Sources.Get(name);

    public void UnCachePak(AdaptorSettings settings, int pak)
    {
        var source = Source(settings);
        if (source is null) return;

        if (PakCache.ContainsKey((settings, source, pak)))
        {
            Debug($"Removing pak {pak} from transfer cache");
            PakCache.TryRemove((settings, source, pak), out _);
        }
    }

    private (bool, string, ImageType) ContainsPak(string[] files)
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

    private async Task<(bool, NabuProgram[])> IsKnownListType(ProgramSource source)
    {
        var tasks = new Task<(bool, NabuProgram[])>[] {
            IsNabuCaJson(source),
            IsNabuNetworkList(source)
        };

        foreach (var task in tasks)
        {
            var (isList, programs) = await task;
            if (isList) return (isList, programs);
        }
        return (false, Array.Empty<NabuProgram>());
    }

    private (bool, ImageType) IsSupportedType(string file)
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

    private readonly IProgramPatch[] DefaultPatches;

    private async Task CacheSource(ProgramSource source)
    {
        if (source is null || 
            source.SourceType is not SourceType.Remote || 
            !SourceCache.TryGetValue(source, out IEnumerable<NabuProgram>? programs)
        )   return;

        foreach (var program in from p in programs where !p.IsPakMenu select p)
        {
            try
            {
                var status = await Http.GetPathStatus(program.Path);
                if (!status.ShouldDownload) continue;
                var (path, name) = Http.CacheFileNames(program.Path);
                Logger.LogInformation("Caching {} from {}", program.DisplayName, source.Name);
                //await Http.DownloadAndCache(program.Path, path, name);
                _ = await Http.GetFile(program.Path);
            }
            catch { continue; }
        }
    }

    public async void RefreshSources(RefreshType refresh = RefreshType.All)
    {
        
        //if (UpdateLock.CurrentCount == 0) return; //An update is in progress

        await UpdateLock.WaitAsync();
        var installedPackages = Packages.Installed.ToArray();

        foreach (var source in Sources.List)
        {
            if (source is null)
                continue;

            await Task.Run(async () =>
            {
                var isRemote = IsWebSource(source.Path);

                var checkRemote = refresh.HasFlag(RefreshType.Remote);
                var checkLocal = refresh.HasFlag(RefreshType.Local);

                if (isRemote)
                    source.SourceType = SourceType.Remote;
                else if (source.SourceType is SourceType.Unknown)
                    source.SourceType = SourceType.Local;

                if (checkRemote && source.SourceType is SourceType.Remote)
                {
                    var programs = new List<NabuProgram>();
                    //source.SourceType = SourceType.Remote;
                    
                    var (isList, items) = await IsKnownListType(source);
                    
                    var (isPak, pakUrl, type) = isList ? 
                        (false, string.Empty, ImageType.None) : 
                        await IsPak(source.Path, 1);

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
                            DefaultPatches,                            source.Author ?? Empty,                            source.Description ?? Empty,                            BlankIconClrStr,                            BlankIconPtrnStr,
                            isPakMenu: true
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
                            DefaultPatches,                            source.Author ?? Empty,                            source.Description ?? Empty,                            BlankIconClrStr,                            BlankIconPtrnStr
                        ));
                    }

                    SourceCache[source] = programs;

                }
                else if (checkLocal && source.SourceType is SourceType.Local)
                {
                    if (!Path.Exists(source.Path)) return;

                    var programs = new List<NabuProgram>();
                    source.SourceType = SourceType.Local;

                    var files = Directory.Exists(source.Path) ?
                                    Directory.GetFiles(source.Path) :
                                    [source.Path];

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
                            DefaultPatches,                            source.Author ?? Empty,                            Empty,                            BlankIconClrStr,                            BlankIconPtrnStr,
                            isPakMenu: true
                        ));
                        files = files.Except(new[] { menuPak }).ToArray();
                    }

                    files = files.Where(IsNabu).ToArray();

                    foreach (var file in files)
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        programs.Add(new(
                            IsRawPak(file) ? string.Empty : name,
                            name,
                            source.Name,
                            file,
                            source.SourceType,
                            ImageType.Raw,
                            DefaultPatches,                            source.Author ?? Empty,                            Empty,                            BlankIconClrStr,                            BlankIconPtrnStr
                        ));
                    }

                    SourceCache[source] = programs;
                }
                else if (checkLocal && source.SourceType is SourceType.Package)
                {
                    var package = installedPackages.FirstOrDefault(p => p.Name.LowerEquals(source.Name));
                    if (package is null ||
                        package.Manifest is null)
                        return;
                    var programs = new List<NabuProgram>();

                    programs.AddRange(
                        from program in from p in package.Programs select p with { }
                        select new NabuProgram(
                            program.Name ?? program.Path,
                            program.Name ?? program.Path,
                            source.Name,
                            NabuLib.IsHttp(program.Path) ?
                                program.Path :
                                Path.Combine(PackageFeatures.Programs, program.Path),
                            NabuLib.IsHttp(program.Path) ?
                                SourceType.Remote :
                                SourceType.Local,
                            ImageType.Raw,
                            DefaultPatches,                            program.Author ?? source.Author ?? Empty,                            program.Description ?? Empty,                            program.TileColor ?? BlankIconClrStr,                            program.TilePattern ?? BlankIconPtrnStr,
                            options: program.Options
                        )
                    );

                    if (programs.Count != 0)
                    {
                        SourceCache[source] = programs;
                    }
                }
            });

            if (Settings.PreCacheRemoteSources)
                await CacheSource(source);
        }


        UpdateLock.Release();
    }

    #region URLs

    protected static bool IsEncryptedPak(string path) => path.EndsWith(Constants.EncryptedPakExtension);

    protected static bool IsNabu(string path) => path.EndsWith(Constants.NabuExtension);

    protected static bool IsPak(string path) => path.EndsWith(Constants.PakExtension);

    protected static bool IsRawPak(string path) => PakFile().IsMatch(path);

    protected static bool IsWebSource(string path) => path.Contains("://");    #endregion URLs
    #region Http Location
    public static byte[] BlankIconPattern { get; } = [        0xFF,0x80,0xA2,0xB2,0xAA,0xA6,0xA2,0x80,        0x80,0xBE,0xA2,0xAC,0xA2,0xBE,0x80,0xFF,        0xFF,0x01,0x7D,0x45,0x7D,0x45,0x45,0x01,        0x01,0x45,0x45,0x45,0x45,0x7D,0x01,0xFF    ];     public static byte[] BlankIconColor { get; } = [        0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,        0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,        0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,        0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F,0x0F    ];    public static string BlankIconClrStr { get; } = Convert.ToBase64String(BlankIconColor);    public static string BlankIconPtrnStr { get; } = Convert.ToBase64String(BlankIconPattern);    const string Empty = " ";    protected async Task<(bool, NabuProgram[])> IsNabuCaJson(ProgramSource source)    {        var uri = source.Path;        if (!(uri.EndsWith(".json") && uri.Contains("nabu.ca"))) { return (false, Array.Empty<NabuProgram>()); }        var (_, found, cached, _, _, _, _) = await Http.GetPathStatus(uri);
        if (!found && !cached) { return (false, []); }        var json = await Http.GetString(uri);        try        {            var progs = new List<NabuProgram>();            var sections = JObject.Parse(json)["Items"];            if (sections is null) { return (false, []); }            string[] skipSections = ["NABU Cycles", "Demos", "Utilities"];            foreach (var section in sections)            {                var title = section["Title"]?.ToString();                if (skipSections.Contains(title)) continue;                var files = section["Files"];                if (files is null) continue;                foreach (var item in files)                {                    var name = item["Title"]?.ToString() ?? "Unnamed Program";                    var filename = item["Filename"]?.ToString() ?? Empty;                    var url = $"https://cloud.nabu.ca/HomeBrew/titles/{filename}";                    var author = item["Author"]?.ToString() ?? Empty;                    var description = item["Description"]?.ToString() ?? Empty;                    var tileColor = item["IconTileColor"]?.ToString() ?? BlankIconClrStr;                    var tilePattern = item["IconTilePattern"]?.ToString() ?? BlankIconPtrnStr;                    var headless = item["IsHeadless"]?.ToObject<bool>() is true;                    if (headless && !source.HeadlessMenu)                        source.HeadlessMenu = true;                    progs.Add(new(                        name,                        filename,                        source.Name,                        url,                        SourceType.Remote,                        ImageType.Raw,                        DefaultPatches,                        author,                        description,                        tileColor,                        tilePattern,                        category: title,                        headless: headless                    ));                }            }            return (true, progs.ToArray());        }        catch { return (false, []); }    }

    protected async Task<(bool, NabuProgram[])> IsNabuNetworkList(ProgramSource source)
    {        var uri = source.Path;
        if (!uri.EndsWith(".xml") && !uri.Contains("nabunetwork.com")) { return (false, Array.Empty<NabuProgram>()); }
        var (_, found, cached, _, _, _, _) = await Http.GetPathStatus(uri);
        if (!found && !cached) { return (false, []); }
        try        {            var xml = await Http.GetString(uri);            var list = new XmlDocument();            list.LoadXml(xml);            var cycleNodes = list.DocumentElement?.SelectNodes("Target");            if (cycleNodes is null) { return (false, Array.Empty<NabuProgram>()); }            var progs = new List<NabuProgram>();            foreach (XmlNode cycleNode in cycleNodes)            {                var targetType = cycleNode["TargetType"]?.InnerText;                if (targetType == "NabuNetwork")                    continue;                var name = cycleNode["Name"]?.InnerText ?? "Unnamed Program";                var url = cycleNode["Url"]?.InnerText ?? Empty;                url = url.Replace("loader.nabu", "000002.nabu");                progs.Add(new(                    name,                    name,                    source.Name,                    url,                    SourceType.Remote,                    ImageType.Raw,                    DefaultPatches,                    source.Author ?? Empty,                    Empty,                    BlankIconClrStr,                    BlankIconPtrnStr,                    category: targetType                ));            }            return (true, progs.ToArray());        }        catch { return (false, []); }
    }

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
        bool found, cached;

        if (type is ImageType.None)
        {
            (_, found, cached, _, _, _, _) = await Http.GetPathStatus($"{url}/{FormatTriple(pak)}{Constants.NabuExtension}");
            if (found || cached)
                return (true, url, ImageType.Raw);

            (_, found, cached, _, _, _, _) = await Http.GetPathStatus($"{url}/{FormatTriple(pak)}{Constants.PakExtension}");
            if (found || cached)
                return (true, url, ImageType.Pak);

            (_, found, cached, _, _, _, _) = await Http.GetPathStatus($"{url}/{NabuLib.PakName(pak)}{Constants.PakExtension}");
            if (found || cached)
                return (true, url, ImageType.Pak);

            (_, found, cached, _, _, _, _) = await Http.GetPathStatus($"{url}/{NabuLib.PakName(pak)}{Constants.EncryptedPakExtension}");
            if (found || cached)
                return (false, url, ImageType.EncryptedPak); //Encrypted pak support is disabled.

            return (false, url, ImageType.None);
        }

        (_, found, cached, _, _, _, _) = await Http.GetPathStatus(url);
        return (found || cached, url, type);
    }

    [GeneratedRegex(".*\\d{6}\\.nabu")]
    private static partial Regex PakFile();

    #endregion Http Location
}

public record SourceListResult(bool Handled, NabuProgram[] Programs);