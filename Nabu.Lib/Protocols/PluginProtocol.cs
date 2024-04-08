using Gry.Protocols;
using Microsoft.Extensions.Logging;
using Nabu.Settings;

namespace Nabu.Protocols
{
    public abstract class PluginProtocol : Protocol<AdaptorSettings>
    {
        protected PluginProtocol(ILogger logger, ProtocolSettings protocol) : base(logger)
        {
            Protocol = protocol;
            Messages = Protocol.Commands;
            Version = Protocol.Version;
            //Label = protocol.Name;
        }

        public override byte[] Messages { get; }
        public override byte Version { get; }
        protected ProtocolSettings? Protocol { get; }
    }
}