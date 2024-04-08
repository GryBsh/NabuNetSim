using Lgc;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Nabu.Logs
{
    public class NLogBuilder : ILoggerBuilder
    {
        public ILoggingBuilder Build(ILoggingBuilder builder)
        {
            return builder.AddNLog("nlog.config");
        }
    }
}
