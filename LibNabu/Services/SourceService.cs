using Nabu.Network;

namespace Nabu.Services;


public class SourceService : ISourceService
{
    
    private List<ProgramSource> Sources => Settings.Sources;

    private SemaphoreSlim SourcesLock { get; } = new(1, 1);
    private Settings Settings { get; }

    public SourceService(Settings settings)
    {
        Settings = settings;
    }

    public void RemoveAll(Predicate<ProgramSource> predicate)
    {
        lock (SourcesLock)
        {
            Sources.RemoveAll(predicate);
        }
    }

    public void Refresh(ProgramSource source)
    {
        RemoveAll(s => NabuLib.InsensitiveEqual(s.Name, source.Name));
        lock (SourcesLock)
        {
            Sources.Add(source);
        }
        RaiseSourceChanged(source);
    }

    public void Add(ProgramSource source)
    {
        lock (SourcesLock)
        {
            Sources.Add(source);
        }
        RaiseSourceChanged(source);
    }

    public bool Remove(ProgramSource source)
    {
        lock (SourcesLock)
        {
            var r = Sources.Remove(source);
            RaiseSourceChanged(source);
            return r;
        }

    }

    public IEnumerable<ProgramSource> All() => Sources.ToArray();

    public ProgramSource? Get(Predicate<ProgramSource> predicate)
    {
        lock (SourcesLock)
        {
            return Sources.FirstOrDefault(s => predicate(s));
        }
    }

    public ProgramSource? Get(string name)
    {
        lock (SourcesLock)
        {
            return Sources.FirstOrDefault(s => NabuLib.InsensitiveEqual(s.Name, name));
        }
    }

    public event EventHandler<ProgramSource> SourceChanged;
    void RaiseSourceChanged(ProgramSource source)
    {
        SourceChanged?.Invoke(this, source);
    }
}