using Gry.Protocols;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Protocols;
using Nabu.Settings;


namespace Nabu.JavaScript;



public class JavaScriptFactory : IProtocolFactory <AdaptorSettings>
{
    public string Type => "JavaScript";

    public IProtocol<AdaptorSettings> CreateProtocol(IServiceProvider provider, ILogger logger, ProtocolSettings protocol)
    {
        return new JavaScriptProtocol(
            logger,
            protocol
        );
    }
}