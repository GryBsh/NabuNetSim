using Gry.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NHACPy.Adapters;

public class SerialAdapter(
    ILogger<SerialAdapter> logger,
    IServiceScopeFactory scopes
) : SerialListenerBase<NHACPyAdapter, NHACPyAdapter>(logger, scopes);
