using Nabu.Services;

namespace Nabu;

public abstract class NabuBase
{
    public NabuBase(ILog logger, string? label = null)
    {
        Logger = logger;
        Label = label ?? string.Empty;
    }

    public string Label { get; protected set; }
    protected ILog Logger { get; }

    protected static bool Empty(string? str) => string.IsNullOrWhiteSpace(str);

    protected static string Format(params byte[] bytes) => NabuLib.Format(bytes);

    protected static string Format(byte byt) => NabuLib.Format(byt);

    protected static string FormatSeparated(params byte[] bytes) => NabuLib.FormatSeperated(bytes);

    protected static string FormatTriple(int triple) => NabuLib.FormatTriple(triple);

    protected void Debug(string message) => Task.Run(() => Logger.WriteVerbose(LogMessage(message)));

    protected void Error(string message) => Task.Run(() => Logger.WriteError(LogMessage(message)));

    protected void Log(string message) => Task.Run(() => Logger.Write(LogMessage(message)));

    protected virtual string LogMessage(string message)
    {
        var header = string.IsNullOrWhiteSpace(Label) ? string.Empty : $"{Label}: ";
        return $"{header}{message}";
    }

    protected void Trace(string message) => Task.Run(() => Logger.WriteVerbose(LogMessage(message)));

    protected void Warning(string message) => Task.Run(() => Logger.WriteWarning(LogMessage(message)));
}