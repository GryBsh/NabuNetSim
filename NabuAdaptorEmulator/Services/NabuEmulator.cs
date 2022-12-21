using Microsoft.Extensions.Logging;

namespace Nabu.Services;

public abstract class NabuEmulator
{
    protected ILogger Logger;

    public NabuEmulator(ILogger logger)
    {
        Logger = logger;
    }

    protected static string Format(params byte[] bytes) => Tools.Format(bytes);
    protected IEnumerable<byte> EscapeBytes(IEnumerable<byte> sequence)
    {
        foreach (byte b in sequence)
        {
            if (b == Messages.Escape)
            {
                yield return Messages.Escape;
                yield return b;
            }
            else
                yield return b;
        }
    }
    protected void Log(string message)     => Logger.LogInformation(message);
    protected void Debug(string message)   => Logger.LogDebug(message);
    protected void Trace(string message)   => Logger.LogTrace(message);
    protected void Warning(string message) => Logger.LogWarning(message);
    protected void Error(string message)   => Logger.LogError(message);


}
