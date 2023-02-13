using Nabu.Patching;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nabu.Network;

public partial class NabuNetwork : NabuBase
{
    HttpCache Http { get; }
    Settings Settings { get; }
    List<ProgramSource> Sources { get; }
    Dictionary<ProgramSource, IEnumerable<NabuProgram>> SourceCache { get; } = new();
    Dictionary<(string?, int), byte[]> PakCache { get; } = new();
    public NabuNetwork(
        IConsole<NabuNetwork> logger,
        Settings settings, 
        HttpClient http
    ) : base(logger)
    {
        Settings = settings;
        Sources = settings.Sources; 
        Http = new (http, logger);
        Task.Run(() => RefreshSources());
        Observable.Interval(TimeSpan.FromMinutes(1))
                  .SubscribeOn(ThreadPoolScheduler.Instance)
                  .Subscribe(async _ => await RefreshSources(true));
        Observable.Interval(TimeSpan.FromMinutes(1))
                  .SubscribeOn(ThreadPoolScheduler.Instance)
                  .Subscribe(async _ => await RefreshSources(true));
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

    public async Task RefreshSources(bool localOnly = false, bool remoteOnly = false)
    {
        foreach (var source in Sources)
        {
            var pak = 1;
            var nabuName = FormatTriple(pak);
            
            var isRemote = IsWebSource(source.Path);
            if (!localOnly && (source.SourceType is SourceType.Remote || isRemote))
            {
                source.SourceType = SourceType.Remote;
                var (isList, items) = await IsNabuCaList(source.Name, source.Path);
                if (isList)
                {
                    SourceCache[source] = items;
                    continue;
                }
                var (isPak, pakUrl) = await IsPak(source.Path, 1);
                if (isPak)
                {
                    SourceCache[source] = new NabuProgram[] { new(
                        "Cycle Menu",
                        Constants.CycleMenuPak,
                        source.Name,
                        DefinitionType.Folder,
                        pakUrl,
                        source.SourceType,
                        ImageType.Raw,
                        new[] { new PassThroughPatch(Logger) },
                        true
                    )};
                    continue;
                }
                if (IsNabu(source.Path))
                {
                    SourceCache[source] = new NabuProgram[] { new(
                        source.Name,
                        string.Empty,
                        source.Name,
                        DefinitionType.Folder,
                        source.Path,
                        source.SourceType,
                        ImageType.Raw,
                        new[] { new PassThroughPatch(Logger) }
                    )};
                }
            }
            else if (!remoteOnly && (source.SourceType is SourceType.Local || !isRemote))
            {
                source.SourceType = SourceType.Local;
                var files = Directory.GetFiles(source.Path, "*.nabu");
                var programs = new List<NabuProgram>();
                var menuPak = files.FirstOrDefault(
                    f => Path.GetFileNameWithoutExtension(f) is Constants.CycleMenuPak
                );
                if (menuPak is not null)
                {   // We found a raw Cycle folder with a 000001.nabu file in it.
                    programs.Add(new (
                            "Cycle Menu",
                            Constants.CycleMenuPak,
                            source.Name,
                            DefinitionType.Folder,
                            menuPak,
                            source.SourceType,
                            ImageType.Raw,
                            new[] { new PassThroughPatch(Logger) },
                            true
                    ));
                    files = files.Except(new[] { menuPak }).ToArray();
                }
                
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    programs.Add(new(
                        name,
                        name,
                        source.Name,
                        DefinitionType.Folder,
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

        if (PakCache.TryGetValue((source.Name.ToLower(), pak), out var value))
        {
            return (ImageType.Raw, value);
        }

        var path = source.Path;
        var image = pak switch
        {

            > 1 => FormatTriple(pak),
            _ when Empty(settings.Image) => NabuLib.FormatTriple(1),
            _ => settings.Image!
        };

        var prg = SourceCache[source].FirstOrDefault(p => p.Name == image) ?? 
                  SourceCache[source].FirstOrDefault(p => p.IsPakMenu);
        
        if (prg is null) 
            return (ImageType.None, ZeroBytes);
        
        

        if (prg.IsPakMenu && pak > 1)
        {
            path = $"{path}/{FormatTriple(pak)}.nabu";
        }
        else path = prg.Path;


        byte[] bytes = ZeroBytes;
        try
        {
            bytes = (prg.SourceType, prg.ImageType) switch
            {
                (SourceType.Remote, ImageType.Raw) => await Http.GetBytes(path),
                (SourceType.Local, ImageType.Raw) => await File.ReadAllBytesAsync(path),
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

    public void UncachePak(string source, int pak)
    {
        source = source.ToLower();
        if (PakCache.ContainsKey((source, pak)))
        {
            Log($"Removing pak {pak} from cache");
            PakCache.Remove((source, pak));
        }
    }

    #region URLs
    protected static bool IsWebSource(string path) => path.Contains("://");
    protected static bool IsNabu(string path) => path.EndsWith(".nabu");
    protected static bool IsPak(string path) => PakFile().IsMatch(path);

    //protected static bool IsEncryptedPak(string path) => path.EndsWith(".npak");

    #endregion

    #region Http Location
    public async Task<(bool, string)> IsPak(string url, int pak)
    {
        url = url.TrimEnd('/');
        
        url = url switch
        {
            _ when IsPak(url) => url,
            _ => $"{url}/{FormatTriple(pak)}.nabu"
        };

        var (_, found, cached) = await Http.CanGet(url);
        return (found || cached, url);
    }

    public async Task<(bool, NabuProgram[])> IsNabuCaList(string source, string uri)
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
                DefinitionType.NabuCaList,
                url,
                SourceType.Remote,
                ImageType.Raw,
                new[] { new PassThroughPatch(Logger) }
            ));
        }
        return (true, progs.ToArray());
    }


    [GeneratedRegex(".*d{6}.nabu")]
    private static partial Regex PakFile();
    #endregion

}
