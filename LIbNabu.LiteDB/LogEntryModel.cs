using LiteDB;
using Nabu.Models;

namespace Nabu;

public class LogEntryModel : ILiteDbModel<LogEntry>
{
    public LogEntryModel()
    { }

    public void Configure(EntityBuilder<LogEntry> builder, ILiteCollection<LogEntry> collection)
    {
        builder.Id(e => e.Id);
        collection.EnsureIndex(e => e.Timestamp);
    }
}