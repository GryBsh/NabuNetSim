using LiteDB;
using Nabu.Services;

namespace Nabu;

public class LiteDatabaseRepository : IRepository
{
    private bool disposedValue;
    protected static LiteDatabase Database { get; set; }

    public LiteDatabaseRepository(Settings settings)
    {
        if (Database is null) {
            var cs = new ConnectionString();
            cs.Upgrade = true;
            cs.Filename = settings.DatabasePath;
            Database ??= new LiteDatabase(cs);
        }

        Database.Mapper.Entity<IEntity>().Id(e => e.Id);
    }

    public ILiteCollection<T> Collection<T>()
    {
        return Database.GetCollection<T>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Database.Dispose();
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
