using Gry.Protocols;
using Microsoft.Extensions.Logging;

namespace NHACPy.Adapters;

public abstract class Protocol(ILogger logger) : Protocol<NHACPyAdapter>(logger)
{
}
