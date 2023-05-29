using Nabu.Network;

namespace Nabu.Services;

public class SourceService
{
    static List<ProgramSource> Sources { get; set; } = new();
    SemaphoreSlim SourcesLock { get; } = new(1, 1);
    public SourceService(Settings settings)
    {
        Sources = settings.Sources.ToList();
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
    }

    public void Add(ProgramSource source)
    {
        lock (SourcesLock)
        {
            Sources.Add(source);
        }
    }

    public bool Remove(ProgramSource source)
    {
        lock (SourcesLock)
        {
            return Sources.Remove(source);
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
}

