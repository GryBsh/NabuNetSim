using Nabu.Adaptor;
using Nabu.Services;

namespace Nabu;

public class ProxyProtocol : Protocol
{
    public ProxyProtocol(IConsole logger) : base(logger)
    {
    }

    public override byte Version { get; } = 0x00;

    public override byte[] Commands => Array.Empty<byte>();

    protected override Task Handle(byte unhandled, CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
}

