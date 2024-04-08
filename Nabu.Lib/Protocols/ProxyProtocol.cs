
using Gry.Protocols;
using Microsoft.Extensions.Logging;
using Nabu.Settings;

namespace Nabu.Protocols
{
    public class ProxyProtocol : Protocol<AdaptorSettings>
    {
        public ProxyProtocol(ILogger logger) : base(logger)
        {

        }

        public override byte[] Messages => [];
        public override byte Version { get; } = 0x00;

        protected override Task Handle(byte unhandled, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }
    }
}