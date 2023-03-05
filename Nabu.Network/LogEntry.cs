using Microsoft.Extensions.Logging;

namespace Nabu;

public record LogEntry : IEntity
{
    public LogEntry() { }
    public LogEntry(
        Guid id,
        DateTime timestamp,
        LogLevel logLevel,
        EventId eventId,
        string name,
        string message,
        Exception? exception
    )
    {
        Id = id;
        Timestamp = timestamp;
        LogLevel = logLevel;
        EventId = eventId;
        Name = name;
        Message = message;
        Exception = exception;
    }

    public Guid Id { get; private set; } = Guid.Empty;
    public DateTime Timestamp { get; set; }
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }

}

