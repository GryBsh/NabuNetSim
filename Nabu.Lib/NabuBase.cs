
using Microsoft.Extensions.Logging;

namespace Nabu;

public abstract class NabuBase(ILogger logger, string? label = null)
{
    public string Label { get; protected set; } = label ?? string.Empty;
    protected ILogger Logger { get; } = logger;

    protected static bool Empty(string? str) => string.IsNullOrWhiteSpace(str);

    protected static string Format(params byte[] bytes) => NabuLib.Format(bytes);

    protected static string Format(byte byt) => NabuLib.Format(byt);

    protected static string FormatSeparated(params byte[] bytes) => NabuLib.FormatSeparated('|', bytes);

    protected static string FormatTriple(int triple) => NabuLib.FormatTriple(triple);

    protected void Debug(string message) => Task.Run(() => Logger.LogDebug(LogMessage(message)));

    protected void Error(string message) => Task.Run(() => Logger.LogError(LogMessage(message)));

    protected void Log(string message) => Task.Run(() => Logger.LogInformation(LogMessage(message)));

    protected virtual string LogMessage(string message)
    {
        var header = string.IsNullOrWhiteSpace(Label) ? string.Empty : $"{Label}: ";
        return $"{header}{message}";
    }

    protected void Trace(string message) => Task.Run(() => Logger.LogTrace(LogMessage(message)));

    protected void Warning(string message) => Task.Run(() => Logger.LogWarning(LogMessage(message)));
}