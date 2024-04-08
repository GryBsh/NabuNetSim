using Microsoft.Extensions.Logging;

namespace Nabu.Logs
{
    public class ConsoleLog<T>(Microsoft.Extensions.Logging.ILogger<T> logger) : ILogger<T>
    {
        private ILogger Logger { get; } = logger;

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
}