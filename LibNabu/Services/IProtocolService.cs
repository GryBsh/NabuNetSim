using Nabu.Network;

namespace Nabu.Services
{
    public interface IProtocolService
    {
        IEnumerable<IProtocol> GetProtocols();
    }
}