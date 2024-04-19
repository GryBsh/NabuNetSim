using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lgc;

public interface ILoggerBuilder<TContext>
{
    ILoggingBuilder Build(        TContext context,         ILoggingBuilder builder,         IConfiguration configuration    );
}
