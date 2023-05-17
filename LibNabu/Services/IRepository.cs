using System.Linq.Expressions;

namespace Nabu.Services;

public interface IRepository<T>
{
    IEnumerable<T> Query<TQueryable>(Func<TQueryable, IEnumerable<T>> query);
    IEnumerable<T> Select(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
    IEnumerable<T> SelectAll(int skip = 0, int limit = int.MaxValue);
    IEnumerable<T> SelectAllAscending<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue);
    IEnumerable<T> SelectAllDescending<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue);
    IEnumerable<T> SelectAll<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue);
    int Count();
    int Count(Expression<Func<T, bool>> predicate);

    void Insert(params T[] items);
    void BulkInsert(params T[] items);

    void Delete(Expression<Func<T, bool>> predicate);
    void DeleteAll();
}
