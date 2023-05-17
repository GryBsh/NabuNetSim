using LiteDB;

namespace Nabu;

public abstract class LiteDbModel<T>
{
    public abstract void Configure(EntityBuilder<T> builder, ILiteCollection<T> collection);
}