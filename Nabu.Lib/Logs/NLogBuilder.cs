using Lgc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Nabu.Logs
{
    public class NLogBuilder : ILoggerBuilder
    {
        public ILoggingBuilder Build(ILoggingBuilder builder, IConfiguration configuration)
        {
            return builder.AddConfiguration(configuration)                          .AddNLog("nlog.config");
        }
    }  
}
