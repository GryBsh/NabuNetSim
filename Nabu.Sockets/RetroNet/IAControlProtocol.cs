using Nabu.Adaptor;
using Nabu.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Network.RetroNet;



public class RetroNetIAControlProtocol : Protocol
{
    public RetroNetIAControlProtocol(IConsole logger) : base(logger)
    {
    }

    public override byte Version => 0x01;

    public override byte[] Commands => throw new NotImplementedException();

    public override Task Handle(byte unhandled, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }
}
