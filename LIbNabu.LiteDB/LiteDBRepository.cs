using LiteDB;
using Nabu.Services;
using System.Linq.Expressions;

namespace Nabu;

public class LiteDBRepository<T> : IRepository<T>, IDisposable
{
    private bool disposedValue;
    protected static LiteDatabase? Database { get; set; }

    public LiteDBRepository(Settings settings)
    {
        LiteDatabase init()
        {
            var cs = new ConnectionString();
            cs.Upgrade = true;
            cs.Filename = settings.DatabasePath;
            var database = new LiteDatabase(cs);
            database.Mapper.Entity<IEntity>().Id(e => e.Id);
            return database;
        }

        Database ??= init();
    }

    public void Delete(Expression<Func<T, bool>> predicate)
    {
        Database!.GetCollection<T>().DeleteMany(predicate);
    }

    public void DeleteAll()
    {
        Database!.GetCollection<T>().DeleteAll();
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
            return query.Invoke((TQueryable)Database!.GetCollection<T>().Query());
        } catch (InvalidCastException)
        {
            throw fail;
        }
    }

    public IEnumerable<T> Select(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue)
    {
        return Database!.GetCollection<T>().Find(predicate, skip, limit);
    }

    public IEnumerable<T> SelectAll(int skip = 0, int limit = int.MaxValue)
    {
        var collection = Database!.GetCollection<T>();
        if (skip is 0 && limit is int.MaxValue) return collection.FindAll();
        //var count = collection.Count();
        //if (count is 0) return Array.Empty<T>();    

        //var remains = (count -  skip); 
        return collection.FindAll().Skip(skip).Take(limit);
    }

    public int Count()
    {
        return Database!.GetCollection<T>().Count();
    }

    public int Count(Expression<Func<T, bool>> predicate)
    {
        return Database!.GetCollection<T>().Count(predicate);
    }

    public IEnumerable<T> Page(int page, int size)
    {
        var collection = Database!.GetCollection<T>();
        var count = collection.Count();
        if (count is 0) return Array.Empty<T>();

        var skip = (page - 1) * size;
        return SelectAll(skip, size);
    }

    public void Insert(params T[] items) => Database!.GetCollection<T>().Insert(items);
    public void BulkInsert(params T[] items) => Database!.GetCollection<T>().InsertBulk(items);
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