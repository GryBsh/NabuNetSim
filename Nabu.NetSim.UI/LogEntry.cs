using Microsoft.Extensions.Logging;

namespace Nabu.NetSim.UI;

public record LogEntry
{
    public LogEntry(
        Guid guid,
        DateTime timestamp,
        LogLevel logLevel,
        EventId eventId,
        string name,
        string message,
        Exception? exception
    )
    {
        Guid = guid;
        Timestamp = timestamp;
        LogLevel = logLevel;
        EventId = eventId;
        Name = name;
        Message = message;
        Exception = exception;
    }

    public Guid Guid { get; }
    public DateTime Timestamp { get; }
    public LogLevel LogLevel { get; }
    public EventId EventId { get; }
    public string Name { get; }
    public string Message { get; }
    public Exception? Exception { get; }
    
}

