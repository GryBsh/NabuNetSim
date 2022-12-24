using Microsoft.Extensions.Logging;

namespace Nabu.Services;

public abstract class NabuService
{
    protected ILogger Logger;

    public NabuService(ILogger logger)
    {
        Logger = logger;
    }

    protected static string Format(params byte[] bytes) => Tools.Format(bytes);
    
    protected void Log(string message)     => Logger.LogInformation(message);
    protected void Debug(string message)   => Logger.LogDebug(message);
    protected void Trace(string message)   => Logger.LogTrace(message);
    protected void Warning(string message) => Logger.LogWarning(message);
    protected void Error(string message)   => Logger.LogError(message);


}
