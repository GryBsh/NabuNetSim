using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Services;

public interface IRepository<T>
{
    IEnumerable<T> Query<TQueryable>(Func<TQueryable, IEnumerable<T>> query);
    IEnumerable<T> Select(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
    IEnumerable<T> SelectAll(int skip = 0, int limit = int.MaxValue);
    IEnumerable<T> SelectAllDescending<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue);
    IEnumerable<T> SelectAll<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue);
    int Count();
    int Count(Expression<Func<T, bool>> predicate);

    IEnumerable<T> Page(int page, int size);
    void Insert(params T[] items);
    void BulkInsert(params T[] items);

    void Delete(Expression<Func<T, bool>> predicate);
    void DeleteAll();
}
