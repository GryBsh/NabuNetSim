using Microsoft.Extensions.Logging;

namespace Nabu;

public abstract class NabuService
{
    protected IConsole Logger { get; }
    protected AdaptorSettings settings { get; set; }
    public NabuService(IConsole logger, AdaptorSettings settings, int index = -1)
    {
        Logger = logger;
        this.settings = settings;
        Index = index;
    }
    public int Index { get; }
    protected static bool Empty(string? str) => string.IsNullOrWhiteSpace(str);
    protected static byte[] ZeroBytes => Array.Empty<byte>();
    protected static string FormatSeparated(params byte[] bytes) => NabuLib.FormatSeperated(bytes);
    protected static string Format(params byte[] bytes) => NabuLib.Format(bytes);
    protected static string Format(byte byt) => NabuLib.Format(byt);
    protected static string FormatTriple(int triple) => NabuLib.FormatTriple(triple);

    string Message(string message)
    {
        return $"{settings.Type}:{settings.Port}:{message}";
    }

    protected void Log(string message) => Task.Run(() => Logger.Write(Message(message)));
    protected void Debug(string message) => Task.Run(() => Logger.WriteVerbose(Message(message)));
    protected void Trace(string message) => Task.Run(() => Logger.WriteVerbose(Message(message)));
    protected void Warning(string message) => Task.Run(() => Logger.WriteWarning(Message(message)));
    protected void Error(string message) => Task.Run(() => Logger.WriteError(Message(message)));


}