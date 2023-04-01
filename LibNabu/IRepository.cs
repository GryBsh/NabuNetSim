using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Nabu;

public interface IRepository<T>
{
    IEnumerable<T> Select(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);
    IEnumerable<T> SelectAll();

    void Insert(params T[] items);
    void BulkInsert(params T[] items);

    void Delete(Expression<Func<T, bool>> predicate);
    void DeleteAll();
}
