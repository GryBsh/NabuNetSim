using Gry.Caching;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Settings;
using System.Collections.ObjectModel;

namespace Nabu.Sources;



public class SourceService : ISourceService
{
    
    public SourceService(
    ILogger<SourceService> logger,
    GlobalSettings settings)
    {
        this.logger = logger;
        this.settings = settings;

        SourceList ??= new List<ProgramSource>(settings.Sources)
        {
            new() {
                Name = "Local NABU Files",
                Path = settings.LocalProgramPath
            }
        };


    }
    
    private readonly ILogger<SourceService> logger;
    private readonly GlobalSettings settings;

    public event EventHandler<ProgramSource>? SourcesChanged;

    private static List<ProgramSource>? SourceList { get; set; }

    private static SemaphoreSlim SourcesLock { get; } = new(1, 1);
        
    public void Add(ProgramSource source)
    {
        lock (SourcesLock)
        {
            SourceList?.Add(source);
        }
        RaiseSourceChanged(source);
    }

    public IEnumerable<ProgramSource> List => SourceList?.ToArray() ?? [];

    public ProgramSource? Get(Predicate<ProgramSource> predicate)
    {
        lock (SourcesLock)
        {
            return SourceList?.FirstOrDefault(s => predicate(s));
        }
    }

    public ProgramSource? Get(string? name)
    {        if (name == null)        {            return null;        }
        lock (SourcesLock)
        {
            return SourceList?.FirstOrDefault(s => s.Name.LowerEquals(name));
        }
    }

    public void Refresh(ProgramSource source)
    {
        RemoveAll(s => s.Name.LowerEquals(source.Name));
        lock (SourcesLock)
        {
            SourceList?.Add(source);
        }
        RaiseSourceChanged(source);
    }

    public bool Remove(ProgramSource source)
    {
        lock (SourcesLock)
        {
            var r = SourceList?.Remove(source) ?? false;
            RaiseSourceChanged(source);
            return r;
        }
    }

    public void RemoveAll(Predicate<ProgramSource> predicate)
    {
        lock (SourcesLock)
        {
            SourceList?.RemoveAll(predicate);
        }
    }

    private void RaiseSourceChanged(ProgramSource source)
    {
        SourcesChanged?.Invoke(this, source);
    }

    
}