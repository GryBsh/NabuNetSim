using Microsoft.Extensions.Logging;

namespace Nabu;

public class MicrosoftExtensionsLoggingConsole<T> : IConsole<T>
{
    readonly ILogger Logger;

    public MicrosoftExtensionsLoggingConsole(ILogger<T> logger)
    {
        Logger = logger;
    }

    public void Write(string message)
    {
        Logger.LogInformation(message);
    }

    public void WriteError(string message, Exception? exception = null)
    {
        Logger.LogError(exception, message);
    }

    public void WriteVerbose(string message)
    {
        Logger.LogDebug(message);
    }

    public void WriteWarning(string message)
    {
        Logger.LogWarning(message);
    }

    public void WriteWarning(string message, Exception? exception = null)
    {
        Logger.LogWarning(exception, message);
    }
}
