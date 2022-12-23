using Microsoft.Extensions.Logging;
using Nabu.Binary;
using Nabu.Network;

namespace Nabu.Adaptor;

public class SerialAdaptorEmulator : AdaptorEmulator
{
    public SerialAdaptorEmulator(
        NetworkEmulator network, 
        ILogger<SerialAdaptorEmulator> logger, 
        AdaptorSettings settings
    ) : base(
        network, 
        logger, 
        new SerialAdapter(settings, logger)
    ) {}
}
