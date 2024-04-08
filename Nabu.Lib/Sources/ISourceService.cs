namespace Nabu.Sources
{
    public interface ISourceService
    {
        void Add(ProgramSource source);

        IEnumerable<ProgramSource> List { get; }

        ProgramSource? Get(Predicate<ProgramSource> predicate);

        ProgramSource? Get(string name);

        void Refresh(ProgramSource source);

        bool Remove(ProgramSource source);

        void RemoveAll(Predicate<ProgramSource> predicate);
    }
}