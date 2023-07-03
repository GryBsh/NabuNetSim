using Microsoft.Extensions.Logging;
using Nabu.Messages;
using Nabu.Services;

namespace Nabu.Cli;

public class ConsoleLog<T> : ILog<T>
{
    public ConsoleLog(ILogger<T> logger)
    {
        Logger = logger;
    }

    private ILogger Logger { get; }

    public void Write(string message)
    {
        Logger.LogInformation(message);
    }

    public void WriteError(string message, Exception? exception = null)
    {
        if (exception is not null && message is null)
            Logger.LogError(exception, exception.Message);
        else Logger.LogError(exception, message);
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