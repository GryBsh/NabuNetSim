using Nabu.Settings;
using Gry.Protocols;

namespace Nabu.NetSimWeb;

public class ProtocolFailedToLoad(ILogger<ProtocolFailedToLoad> logger) : Protocol<AdaptorSettings>(logger)
{
    public override byte[] Messages { get; } = Array.Empty<byte>();

    public override byte Version { get; } = 0;

    protected override Task Handle(byte unhandled, CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
}
