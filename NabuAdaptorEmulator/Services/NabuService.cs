using Microsoft.Extensions.Logging;

namespace Nabu.Services;

public abstract class NabuService
{
    protected ILogger Logger;

    public NabuService(ILogger logger)
    {
        Logger = logger;
    }

    protected static bool Empty(string? str) => string.IsNullOrWhiteSpace(str);
    protected static byte[] ZeroBytes() => Array.Empty<byte>();
    protected static string FormatSeperated(params byte[] bytes) => NABU.FormatSeperated(bytes);
    protected static string Format(params byte[] bytes) => NABU.Format(bytes);
    protected static string FormatTriple(int triple) => NABU.FormatTriple(triple);


    protected void Log(string message) => Logger.LogInformation(message);
    protected void Debug(string message) => Logger.LogDebug(message);
    protected void Trace(string message) => Logger.LogTrace(message);
    protected void Warning(string message) => Logger.LogWarning(message);
    protected void Error(string message) => Logger.LogError(message);


}