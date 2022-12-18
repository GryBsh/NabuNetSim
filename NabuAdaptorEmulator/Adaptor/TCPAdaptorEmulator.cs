using Microsoft.Extensions.Logging;
using Nabu.Binary;
using Nabu.Network;

namespace Nabu.Adaptor;

public class TCPAdaptorEmulator : AdaptorEmulator
{
    public TCPAdaptorEmulator(
        NetworkEmulator network,
        ILogger<TCPAdaptorEmulator> logger,
        AdaptorSettings settings,
        TCPAdapterSettings adapterSettings
    ) : base(
        network,
        logger,
        settings,
        new TCPAdapter(adapterSettings, logger)
    )
    { }
}
