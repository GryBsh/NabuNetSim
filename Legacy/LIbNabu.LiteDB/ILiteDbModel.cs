using LiteDB;

namespace Nabu;

public interface ILiteDbModel<T>
{
    void Configure(EntityBuilder<T> builder, ILiteCollection<T> collection);
}