using Nabu.Patching;
using Nabu.Services;
using Napa;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Nabu.Network;

public partial class NabuNetwork : NabuBase, INabuNetwork
{
    public NabuNetwork(
        ILog<NabuNetwork> logger,
        Settings settings,
        HttpClient http,
        IFileCache cache,
        SourceService sources,
        IPackageManager packages,
        StorageService storage
    ) : base(logger)
    {
        Settings = settings;
        Sources = sources;
        Http = new(http, logger, cache, settings);
        FileCache = cache;
        Packages = packages;
        Storage = storage;
        BackgroundRefresh(RefreshType.All);
    }

    private static ConcurrentDictionary<(AdaptorSettings, ProgramSource, int), Memory<byte>> PakCache { get; } = new();
    private static ConcurrentDictionary<ProgramSource, IEnumerable<NabuProgram>> SourceCache { get; } = new();
    private IFileCache FileCache { get; set; }
    private HttpCache Http { get; }
    private IPackageManager Packages { get; }
    private Settings Settings { get; }
    private SourceService Sources { get; }
    private StorageService Storage { get; }

    public void BackgroundRefresh(RefreshType refresh)
    {
        RefreshSources(refresh);
    }

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

    public IEnumerable<NabuProgram> Programs(ProgramSource source)
    {
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

        var program = Empty(settings.Program) ?
                        SourceCache[source].FirstOrDefault(p => p.Name == settings.Program) :
                        null;

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

        program = SourceCache[source].FirstOrDefault(p => p.Name == targetProgram);

        //BECAUSE: A package using the exploit loader should not have to bundle a menu.
        if (program is null /* && pak > Constants.CycleMenuNumber */)
        {
            program = SourceCache.SelectMany(kv => kv.Value).FirstOrDefault(p => p.Name == targetProgram);
            if (program is not null)
                Logger.WriteWarning($"Source {program.Source} was used for {targetProgram}");
        }

        if (program is null)
            return (ImageType.None, Array.Empty<byte>());

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

    public ProgramSource? Source(string name)
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

    private async Task<(bool, NabuProgram[])> IsKnownListType(string name, string path)
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

    private async void RefreshSources(RefreshType refresh = RefreshType.All)
    {
        //if (refresh.HasFlag(RefreshType.Remote))
        //    Log($"Refreshing remote sources");

        //await Packages.Refresh(true);
        var installedPackages = Packages.Installed.ToArray();

        foreach (var source in Sources.All())
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
                        IsRawPak(file) ? string.Empty : name,
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
            else if (checkLocal && source.SourceType is SourceType.Package)
            {
                var package = installedPackages.FirstOrDefault(p => p.Name == source.Name);
                if (package is null ||
                    package.Manifest is null)
                    continue;
                var programs = new List<NabuProgram>();

                programs.AddRange(
                    from program in from p in package.Programs select p with { }
                    select new NabuProgram(
                        program.Name ?? program.Path,
                        program.Name ?? program.Path,
                        package.Name,
                        NabuLib.IsHttp(program.Path) ?
                            program.Path :
                            Path.Combine(PackageFeatures.Programs, program.Path),
                        NabuLib.IsHttp(program.Path) ?
                            SourceType.Remote :
                            SourceType.Local,
                        ImageType.Raw,
                        program.Option<bool>("ExploitPatchEnabled") ?
                            new[] { new PassThroughPatch(Logger) } :
                            new[] { new PassThroughPatch(Logger) },
                        options: program.Options
                    )
                );

                if (programs.Any())
                {
                    SourceCache[source] = programs;
                }
            }
        }
    }

    #region URLs

    protected static bool IsEncryptedPak(string path) => path.EndsWith(Constants.EncryptedPakExtension);

    protected static bool IsNabu(string path) => path.EndsWith(Constants.NabuExtension);

    protected static bool IsPak(string path) => path.EndsWith(Constants.PakExtension);

    protected static bool IsRawPak(string path) => PakFile().IsMatch(path);

    protected static bool IsWebSource(string path) => path.Contains("://");

    #endregion URLs

    #region Http Location

    protected async Task<(bool, NabuProgram[])> IsNabuCaList(string source, string uri)
    {
        if (!uri.EndsWith(".txt")) { return (false, Array.Empty<NabuProgram>()); }

        var (_, found, cached, _, _) = await Http.GetUriStatus(uri);
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

    protected async Task<(bool, NabuProgram[])> IsNabuNetworkList(string source, string uri)
    {
        if (!uri.EndsWith(".xml") && !uri.Contains("nabunetwork.com")) { return (false, Array.Empty<NabuProgram>()); }
        var (shouldDownload, found, cached, _, _) = await Http.GetUriStatus(uri);
        if (!found && !cached) { return (false, Array.Empty<NabuProgram>()); }

        var xml = await Http.GetString(uri);
        var list = new XmlDocument();
        list.LoadXml(xml);
        var cycleNodes = list.DocumentElement?.SelectNodes("Target");
        if (cycleNodes is null) { return (false, Array.Empty<NabuProgram>()); }
        var progs = new List<NabuProgram>();
        foreach (XmlNode cycleNode in cycleNodes)
        {
            var targetType = cycleNode["TargetType"]?.InnerText;
            if (targetType == "NabuNetwork")
                continue;

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
            (_, found, cached, _, _) = await Http.GetUriStatus($"{url}/{FormatTriple(pak)}{Constants.NabuExtension}");
            if (found || cached)
                return (true, url, ImageType.Raw);

            (_, found, cached, _, _) = await Http.GetUriStatus($"{url}/{FormatTriple(pak)}{Constants.PakExtension}");
            if (found || cached)
                return (true, url, ImageType.Pak);

            (_, found, cached, _, _) = await Http.GetUriStatus($"{url}/{NabuLib.PakName(pak)}{Constants.PakExtension}");
            if (found || cached)
                return (true, url, ImageType.Pak);

            (_, found, cached, _, _) = await Http.GetUriStatus($"{url}/{NabuLib.PakName(pak)}{Constants.EncryptedPakExtension}");
            if (found || cached)
                return (false, url, ImageType.EncryptedPak); //Encrypted pak support is disabled.

            return (false, url, ImageType.None);
        }

        (_, found, cached, _, _) = await Http.GetUriStatus(url);
        return (found || cached, url, type);
    }

    [GeneratedRegex(".*\\d{6}\\.nabu")]
    private static partial Regex PakFile();

    #endregion Http Location
}