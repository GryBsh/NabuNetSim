using Nabu.Adaptor;
using Nabu.Services;

namespace Nabu.Network.RetroNet;



public class RetroNetIAControlProtocol : Protocol
{
    public RetroNetIAControlProtocol(IConsole logger) : base(logger)
    {
    }

    public override byte Version => 0x01;

    public override byte[] Commands => throw new NotImplementedException();

    protected override Task Handle(byte unhandled, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }
}
