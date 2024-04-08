using Gry.Adapters;
using Gry.Protocols;
using Microsoft.Extensions.Logging;
using Nabu.Settings;

namespace Nabu.Protocols
{
    public interface IProtocolFactory<T>
        where T : AdapterDefinition
    {
        string Type { get; }

        IProtocol<T> CreateProtocol(IServiceProvider provider, ILogger logger, ProtocolSettings protocol);
    }
}