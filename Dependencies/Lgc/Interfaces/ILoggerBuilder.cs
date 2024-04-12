using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lgc;

public interface ILoggerBuilder
{
    ILoggingBuilder Build(ILoggingBuilder builder, IConfiguration configuration);
}
