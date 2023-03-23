using LiteDB;
using System.Linq.Expressions;

namespace Nabu;

public class LiteDBRepository<T> : IRepository<T>, IDisposable
{
    private bool disposedValue;
    protected static LiteDatabase? Database { get; set; }

    public LiteDBRepository(Settings settings)
    {
        if (Database is null)
        {
            var cs = new ConnectionString();
            cs.Upgrade = true;
            cs.Filename = settings.DatabasePath;
            Database ??= new LiteDatabase(cs);
            Database.Mapper.Entity<IEntity>().Id(e => e.Id);
        }
    }

    public void Delete(Expression<Func<T, bool>> predicate)
    {
        Database!.GetCollection<T>().DeleteMany(predicate);
    }

    public void DeleteAll()
    {
        Database!.GetCollection<T>().DeleteAll();
    }

    public IEnumerable<T> Select(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue)
    {
        return Database!.GetCollection<T>().Find(predicate, skip, limit);
    }

    public IEnumerable<T> SelectAll()
    {
        return Database!.GetCollection<T>().FindAll();
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
