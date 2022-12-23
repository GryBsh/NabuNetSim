using Microsoft.Extensions.Logging;
using Nabu.Binary;
using Nabu.Network;

namespace Nabu.Adaptor;

public class StreamAdaptorEmulator : AdaptorEmulator
{
    public StreamAdaptorEmulator(
        NetworkEmulator network,
        ILogger<StreamAdaptorEmulator> logger,
        Stream stream
    ) : base(
        network,
        logger,
        new StreamAdapter(stream, logger)
    )
    { }
}


