namespace Nabu.Models;

public class LogEntry : IEntity
{
    public LogEntry()
    { }

    public LogEntry(
        Guid id,
        DateTime timestamp,
        string logLevel,
        string name,
        string message,
        bool highlight = false
    )
    {
        Id = id;
        Timestamp = timestamp;
        LogLevel = logLevel;

        Name = name;
        Message = message;

        Key = new LogKey(Round(Timestamp, TimeSpan.FromMinutes(1)), Name);

        Highlight = highlight;
    }

    public Guid Id { get; private set; } = Guid.Empty;
    public DateTime Timestamp { get; set; }
    public string LogLevel { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public LogKey Key { get; set; } = new(DateTime.MinValue, string.Empty);
    public bool Highlight { get; set; }

    private DateTime Round(DateTime date, TimeSpan span)
    {
        long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
        return new DateTime(ticks * span.Ticks, date.Kind);
    }
}