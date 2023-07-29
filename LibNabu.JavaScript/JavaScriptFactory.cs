using Nabu.Network;
using Nabu.Services;

namespace Nabu.JavaScript;

public class JavaScriptFactory : IProtocolFactory
{
    public string Type => "JavaScript";

    public IProtocol CreateProtocol(IServiceProvider provider, ILog logger, ProtocolSettings protocol)
    {
        return new JavaScriptProtocol(
            logger,
            protocol
        );
    }
}