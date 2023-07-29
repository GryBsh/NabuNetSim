using Nabu.Services;

namespace Nabu.Network;

public class ProxyProtocol : Protocol
{
    public ProxyProtocol(ILog logger, string? label = null) : base(logger)
    {
        Label = label is null ? string.Empty : label;
    }

    public override byte[] Commands => Array.Empty<byte>();
    public override byte Version { get; } = 0x00;

    protected override Task Handle(byte unhandled, CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
}