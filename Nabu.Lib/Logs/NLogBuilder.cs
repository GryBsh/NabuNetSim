using Gry.Settings;
using Lgc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nabu.Settings;
using NLog.Extensions.Logging;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Nabu.Logs
{
    public class NLogBuilder : ILoggerBuilder<ILocationService>
    {
        public ILoggingBuilder Build(            ILocationService location,            ILoggingBuilder builder,             IConfiguration configuration)
        {
            var config = new NLogLoggingConfiguration(configuration.GetSection("NLog"));                        var target = config.FindTargetByName("file") as AsyncTargetWrapper;            var fileTarget = target?.WrappedTarget as FileTarget;            if (fileTarget is not null)                fileTarget.FileName = Path.Combine(location.Of.Logs, "current.log");                        return builder.AddConfiguration(configuration)                           .AddNLog(config);            
        }
    }  
}
