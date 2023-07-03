using Nabu.Network;

namespace Nabu.Services
{
    public interface ISourceService
    {
        void Add(ProgramSource source);

        IEnumerable<ProgramSource> All();

        ProgramSource? Get(Predicate<ProgramSource> predicate);

        ProgramSource? Get(string name);

        void Refresh(ProgramSource source);

        bool Remove(ProgramSource source);

        void RemoveAll(Predicate<ProgramSource> predicate);
    }
}