using Gry;
using Gry.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nabu.Settings;
using Nabu.Sources;
using Napa;
using Newtonsoft.Json.Linq;
using System;
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
        IOptions<CacheOptions> cacheOptions,        LocationService location
    ) : base(logger)
    {
        Settings = settings;
        Sources = sources;
        Http = new(http, logger, cache, cacheOptions, location);
        FileCache = cache;
        Packages = packages;
        Storage = storage;
        DefaultPatches = [new PassThroughPatch(Logger)];
        //RefreshSources(RefreshType.All);
        
    }    protected IProgramPatch[] DefaultPatches { get; }
    private static ConcurrentDictionary<(AdaptorSettings, ProgramSource, int), Memory<byte>> PakCache { get; } = new();
    private static DataDictionary<IEnumerable<NabuProgram>> SourceCache { get; } = new();
    private IFileCache FileCache { get; set; }
    private HttpCache Http { get; }
    private IPackageManager Packages { get; }
    private SourceService Sources { get; }
    private StorageService Storage { get; }
    private GlobalSettings Settings { get; }
    private SemaphoreSlim UpdateLock { get; } = new(1, 1);
   

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

        if (SourceCache.ContainsKey(source.Name) is false)
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

        var program = SourceCache[source.Name].FirstOrDefault(p => p.Name == targetProgram);

        //BECAUSE: A package using the exploit loader should not have to bundle a menu.
        if (program is null /* && pak > Constants.CycleMenuNumber */)
        {
            program = SourceCache.SelectMany(kv => kv.Value).FirstOrDefault(p => p.Name == targetProgram);
            if (program is not null)
                Logger.LogWarning($"Source {program.Source} was used for {targetProgram}");
        }

    

        if (program is null)
            return (ImageType.None, Array.Empty<byte>());
        
        if (program.UseCPMDirect && Settings.CPMSource is not null)
        {
            source = Source(Settings.CPMSource)!;
            program = SourceCache[source.Name].FirstOrDefault(p => p.Name == Settings.CPMProgram);
            if (program is not null)
                path = Path.Combine(source.Path, program.Path);
            else
            {
                return (ImageType.None, Array.Empty<byte>());
            }
            var profile = Path.Combine(Settings.StoragePath, "A0", "PROFILE.SUB");            if (File.Exists(profile))
            {
                File.Copy(profile, Path.Combine(Settings.StoragePath, "A0", "PROFILE.BAK"), true);
            }            File.WriteAllText(profile, "");
        }
        else if (program.IsPakMenu && pak > Constants.CycleMenuNumber)
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

    private async Task CacheSource(ProgramSource source)
    {
        if (source is null || 
            source.SourceType is not SourceType.Remote || 
            !SourceCache.TryGetValue(source.Name, out IEnumerable<NabuProgram>? programs)
        )   return;

        foreach (var program in from p in programs where !p.IsPakMenu select p)
        {
            try
            {
                var status = await Http.GetPathStatus(program.Path);
                if (!status.ShouldDownload) continue;
                var (path, name) = Http.CacheFileNames(program.Path);
                Logger.LogDebug("Caching {} from {}", program.DisplayName, source.Name);
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
                                        var cached = SourceCache.TryGetValue(source.Name, out var progs);
                    var (isList, items) = await IsKnownListType(source);                    if (!isList || items.Length == 0 && cached)                        items = progs?.ToArray() ?? [];                     if (SourceCache.ContainsKey(source.Name) is false || !SourceCache[source.Name].Any())                        SourceCache[source.Name] = items;                        
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
                            DefaultPatches,
                            source.Author ?? Empty,
                            source.Description ?? Empty,
                            CommonUI.IconData(Settings.DefaultIconColor, CommonUI.DefaultIconClrStr),
                            CommonUI.IconData(Settings.DefaultIconPattern, CommonUI.DefaultIconPtrnStr),
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
                            DefaultPatches,
                            source.Author ?? Empty,
                            source.Description ?? Empty,
                            CommonUI.IconData(Settings.DefaultIconColor, CommonUI.DefaultIconClrStr),
                            CommonUI.IconData(Settings.DefaultIconPattern, CommonUI.DefaultIconPtrnStr)
                        ));
                    }

                    SourceCache[source.Name] = programs;

                }
                else if (checkLocal && source.SourceType is SourceType.Local)
                {
                    if (!Path.Exists(source.Path)) return;

                    var programs = new List<NabuProgram>();
                    source.SourceType = SourceType.Local;

                    var files = Directory.Exists(source.Path) ?
                                    Directory.GetFiles(source.Path) :
                                    [ source.Path ];

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
                            DefaultPatches,
                            source.Author ?? Empty,
                            Empty,
                            CommonUI.DefaultIconClrStr,
                            CommonUI.DefaultIconPtrnStr,
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
                            DefaultPatches,
                            source.Author ?? Empty,
                            Empty,
                            CommonUI.IconData(Settings.DefaultIconColor, CommonUI.DefaultIconClrStr),
                            CommonUI.IconData(Settings.DefaultIconPattern, CommonUI.DefaultIconPtrnStr)
                        ));
                    }

                    SourceCache[source.Name] = programs;
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
                            DefaultPatches,
                            program.Author ?? source.Author ?? Empty,
                            program.Description ?? Empty,
                            CommonUI.IconData(Settings.DefaultIconColor, CommonUI.DefaultIconClrStr),
                            CommonUI.IconData(Settings.DefaultIconPattern, CommonUI.DefaultIconPtrnStr),
                            options: program.Options
                        )
                    );

                    if (programs.Count != 0)
                    {
                        SourceCache[source.Name] = programs;
                    }
                }
            });

            if (Settings.PreCacheRemoteSources)
                await CacheSource(source);
        }


        UpdateLock.Release();
    }
}

public record SourceListResult(bool Handled, NabuProgram[] Programs);