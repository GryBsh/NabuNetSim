using Nabu.Network;

namespace Nabu.Services;

public class SourceService : ISourceService
{
    public SourceService(Settings settings)
    {
        Settings = settings;
    }

    public event EventHandler<ProgramSource> SourceChanged;

    private Settings Settings { get; }
    private List<ProgramSource> Sources => Settings.Sources;

    private SemaphoreSlim SourcesLock { get; } = new(1, 1);

    public void Add(ProgramSource source)
    {
        lock (SourcesLock)
        {
            Sources.Add(source);
        }
        RaiseSourceChanged(source);
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
            return Sources.FirstOrDefault(s => s.Name.LowerEquals(name));
        }
    }

    public void Refresh(ProgramSource source)
    {
        RemoveAll(s => s.Name.LowerEquals(source.Name));
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

    public void RemoveAll(Predicate<ProgramSource> predicate)
    {
        lock (SourcesLock)
        {
            Sources.RemoveAll(predicate);
        }
    }

    private void RaiseSourceChanged(ProgramSource source)
    {
        SourceChanged?.Invoke(this, source);
    }
}