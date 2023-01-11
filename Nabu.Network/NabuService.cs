using Microsoft.Extensions.Logging;

namespace Nabu;

public abstract class NabuService
{
    protected ILogger Logger { get; }
    protected AdaptorSettings Settings { get; set; }
    public NabuService(ILogger logger, AdaptorSettings settings, int index = -1)
    {
        Logger = logger;
        Settings = settings;
        Index = index;
    }
    public int Index { get; }
    protected static bool Empty(string? str) => string.IsNullOrWhiteSpace(str);
    protected static byte[] ZeroBytes => Array.Empty<byte>();
    protected static string FormatSeperated(params byte[] bytes) => NabuLib.FormatSeperated(bytes);
    protected static string Format(params byte[] bytes) => NabuLib.Format(bytes);
    protected static string Format(byte byt) => NabuLib.Format(byt);
    protected static string FormatTriple(int triple) => NabuLib.FormatTriple(triple);

    string Message(string message)
    {
        return $"{Settings.Type}:{Settings.Port}:{message}";
    }

    protected void Log(string message) => Task.Run(() => Logger.LogInformation(Message(message)));
    protected void Debug(string message) => Task.Run(() => Logger.LogDebug(Message(message)));
    protected void Trace(string message) => Task.Run(() => Logger.LogTrace(Message(message)));
    protected void Warning(string message) => Task.Run(() => Logger.LogWarning(Message(message)));
    protected void Error(string message) => Task.Run(() => Logger.LogError(Message(message)));


}