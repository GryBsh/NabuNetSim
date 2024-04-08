using Gry.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NHACPy.Adapters;

public class TCPAdapter(
    ILogger<TCPAdapter> logger,
    IServiceScopeFactory scopes
) : TCPListenerBase<NHACPyAdapter, NHACPyAdapter>(logger, scopes);
