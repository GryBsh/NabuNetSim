namespace Nabu;

public abstract class NabuBase
{
    protected IConsole Logger { get; }
    public NabuBase(IConsole logger, int index = -1)
    {
        Logger = logger;
        Index = index;
    }
    public int Index { get; }
    protected static bool Empty(string? str) => string.IsNullOrWhiteSpace(str);
    protected static byte[] ZeroBytes => Array.Empty<byte>();
    protected static string FormatSeparated(params byte[] bytes) => NabuLib.FormatSeperated(bytes);
    protected static string Format(params byte[] bytes) => NabuLib.Format(bytes);
    protected static string Format(byte byt) => NabuLib.Format(byt);
    protected static string FormatTriple(int triple) => NabuLib.FormatTriple(triple);

    protected virtual string LogMessage(string message)
    {
        return message;
    }

    protected void Log(string message) => Task.Run(() => Logger.Write(LogMessage(message)));
    protected void Debug(string message) => Task.Run(() => Logger.WriteVerbose(LogMessage(message)));
    protected void Trace(string message) => Task.Run(() => Logger.WriteVerbose(LogMessage(message)));
    protected void Warning(string message) => Task.Run(() => Logger.WriteWarning(LogMessage(message)));
    protected void Error(string message) => Task.Run(() => Logger.WriteError(LogMessage(message)));
}
