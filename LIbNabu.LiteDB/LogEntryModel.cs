using LiteDB;
using Nabu.Models;

namespace Nabu;

public class LogEntryModel : LiteDbModel<LogEntry>
{
    public override void Configure(EntityBuilder<LogEntry> builder, ILiteCollection<LogEntry> collection)
    {
        builder.Id(e => e.Id);
        collection.EnsureIndex(e => e.Timestamp);
    }
}