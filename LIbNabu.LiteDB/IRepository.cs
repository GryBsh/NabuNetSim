using LiteDB;
using System.Linq.Expressions;

namespace Nabu;

public interface IRepository : IDisposable
{
    ILiteCollection<T> Collection<T>(string? name = null);
}


