
namespace Nabu.Models;

public class LogEntry : IEntity
{
    public LogEntry() { }
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

        Highlight = highlight;
    }

    public Guid Id { get; private set; } = Guid.Empty;
    public DateTime Timestamp { get; set; }
    public string LogLevel { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool Highlight { get; set; }

}

