using LiteDB;
using Nabu.Services;
using System.Linq.Expressions;

namespace Nabu;

public class LiteDBRepository<T> : IRepository<T>, IDisposable
{
    private bool disposedValue;
    protected static LiteDatabase? Database { get; set; }
    static ILiteCollection<T> Collection => Database!.GetCollection<T>();

    public LiteDBRepository(Settings settings)
    {
        LiteDatabase init()
        {
            var cs = new ConnectionString
            {
                Upgrade = true,
                Filename = settings.DatabasePath,
                Connection = ConnectionType.Shared
            };
            var database = new LiteDatabase(cs);
            database.Rebuild();
            database.Mapper.Entity<IEntity>().Id(e => e.Id);
            return database;
        }

        Database ??= init();
    }

    public void Delete(Expression<Func<T, bool>> predicate)
    {
        LiteDBRepository<T>.Collection.DeleteMany(predicate);
    }

    public void DeleteAll()
    {
        LiteDBRepository<T>.Collection.DeleteAll();
    }

    public IEnumerable<T> Query<TQueryable>(Func<TQueryable, IEnumerable<T>> query)
    {
        var fail = new ArgumentException($"{typeof(TQueryable).Name} is not valid for repository of type {typeof(LiteDBRepository<T>).Name}", nameof(TQueryable));
        if (typeof(TQueryable).IsAssignableFrom(typeof(ILiteQueryable<T>)) is false)
        {
            throw fail;
        }
        try
        {
            return query.Invoke((TQueryable)LiteDBRepository<T>.Collection.Query());
        } catch (InvalidCastException)
        {
            throw fail;
        }
    }

    public IEnumerable<T> Select(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue)
    {
        return LiteDBRepository<T>.Collection.Find(predicate, skip, limit);
    }

    public IEnumerable<T> SelectAll(int skip = 0, int limit = int.MaxValue)
    {
        //var collection = Database!.GetCollection<T>();
        if (skip is 0 && limit is int.MaxValue) return LiteDBRepository<T>.Collection.FindAll();
        //var count = collection.Count();
        //if (count is 0) return Array.Empty<T>();    

        //var remains = (count -  skip); 
        return LiteDBRepository<T>.Collection.FindAll().Skip(skip).Take(limit);
    }

    public IEnumerable<T> SelectAllDescending<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue)
    {
        return LiteDBRepository<T>.Collection.Query().OrderByDescending(order).Skip(skip).Limit(limit).ToEnumerable();
    }

    public IEnumerable<T> SelectAll<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue)
    {
        return LiteDBRepository<T>.Collection.Query().OrderByDescending(order).Skip(skip).Limit(limit).ToEnumerable();
    }

    public int Count()
    {
        return LiteDBRepository<T>.Collection.Count();
    }

    public int Count(Expression<Func<T, bool>> predicate)
    {
        return LiteDBRepository<T>.Collection.Count(predicate);
    }

    public IEnumerable<T> Page(int page, int size)
    {
        //var collection = Database!.GetCollection<T>();
        var count = LiteDBRepository<T>.Collection.Count();
        if (count is 0) return Array.Empty<T>();

        var skip = (page - 1) * size;
        return SelectAll(skip, size);
    }

    public void Insert(params T[] items) => LiteDBRepository<T>.Collection.Insert(items);
    public void BulkInsert(params T[] items) => LiteDBRepository<T>.Collection.InsertBulk(items);
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Database!.Dispose();
                Database = null;
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/*
public class LiteDatabaseRepository : IRepository
{
    private bool disposedValue;
    protected static LiteDatabase? Database { get; set; }

    public LiteDatabaseRepository(Settings settings)
    {
        if (Database is null) {
            var cs = new ConnectionString();
            cs.Upgrade = true;
            cs.Filename = settings.DatabasePath;
            Database ??= new LiteDatabase(cs);
            Database.Mapper.Entity<IEntity>().Id(e => e.Id);
        }
    }

    public ILiteCollection<T> Collection<T>(string? name = null)
    {
        if (name is null)
            return Database!.GetCollection<T>();
        return Database!.GetCollection<T>(name);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Database!.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
*/