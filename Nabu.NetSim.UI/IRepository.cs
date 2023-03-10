using LiteDB;

namespace Nabu;

public interface IRepository : IDisposable
{
    ILiteCollection<T> Collection<T>();
}
