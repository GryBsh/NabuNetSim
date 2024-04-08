using Gry.Adapters;
using Gry.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NHACPy.Options;

namespace NHACPy;

public class Server(
    ILogger<Server> logger,
    IOptions<ServerOptions> options,
    IServiceScopeFactory scopes,
    AdapterManager adapters
) : AdapterServer<ServerOptions, AdapterDefinition, TCPAdapterOptions, SerialAdapterOptions>(
    logger,
    options.Value,
    scopes,
    adapters
)
{

}


