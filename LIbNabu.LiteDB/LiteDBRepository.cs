using LiteDB;
using Nabu.Services;
using System.Linq.Expressions;

namespace Nabu;

public class LiteDbRepository<T> : DisposableBase, IRepository<T>
{
    private LiteDatabase? Database { get; set; }
    private ILiteCollection<T> Collection => Database!.GetCollection<T>();
    //protected ISettingsService Settings { get; }

    private LiteDatabase GetConnection(Settings settings)
    {
        var cs = new ConnectionString
        {
            Upgrade = true,
            Filename = settings.DatabasePath,
            Connection = ConnectionType.Shared
        };
        var database = new LiteDatabase(cs);
        return database;
    }

    
    public LiteDbRepository(Settings settings, ILiteDbModel<T>? model = null)
    {
        Database ??= GetConnection(settings);
        Disposables.Add(Database);
        model?.Configure(Database.Mapper.Entity<T>(), Collection);
    }

    public void Delete(Expression<Func<T, bool>> predicate)
    {
        Collection.DeleteMany(predicate);
    }

    public void DeleteAll()
    {
        Collection.DeleteAll();
    }

    public IEnumerable<T> Query<TQueryable>(Func<TQueryable, IEnumerable<T>> query)
    {
        var fail = new ArgumentException(
            $"{typeof(TQueryable).Name} is not valid for repository of type {typeof(LiteDbRepository<T>).Name}",
            nameof(TQueryable)
        );
        if (typeof(TQueryable).IsAssignableFrom(typeof(ILiteQueryable<T>)) is false)
        {
            throw fail;
        }
        try
        {
            return query.Invoke((TQueryable)Collection.Query());
        }
        catch (InvalidCastException)
        {
            throw fail;
        }
    }

    public IEnumerable<T> Select(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue)
    {
        return Collection.Find(predicate, skip, limit);
    }

    public IEnumerable<T> SelectAll(int skip = 0, int limit = int.MaxValue)
    {
        if (skip is 0 && limit is int.MaxValue) return Collection.FindAll();
        return Collection.FindAll().Skip(skip).Take(limit);
    }

    public IEnumerable<T> SelectAllAscending<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue)
    {
        return Collection.Query().OrderBy(order).Skip(skip).Limit(limit).ToEnumerable();
    }

    public IEnumerable<T> SelectDescending<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue)
    {
        return Collection.Query().OrderByDescending(order).Skip(skip).Limit(limit).ToEnumerable();
    }

    public IEnumerable<T> SelectAll<V>(Expression<Func<T, V>> order, int skip = 0, int limit = int.MaxValue)
    {
        return Collection.Query().OrderByDescending(order).Skip(skip).Limit(limit).ToEnumerable();
    }

    public int Count()
    {
        return Collection.Count();
    }

    public int Count(Expression<Func<T, bool>> predicate)
    {
        return Collection.Count(predicate);
    }

    public void Insert(params T[] items) => Collection.Insert(items);

    public void BulkInsert(params T[] items) => Collection.InsertBulk(items);

    public void RunMaintenance()
    {
        Database?.Rebuild();
        Database?.Checkpoint();
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