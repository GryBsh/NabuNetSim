using Nabu.Services;

namespace Nabu.Network;

public abstract class PluginProtocol : Protocol
{
    protected PluginProtocol(ILog logger, ProtocolSettings protocol, AdaptorSettings? settings = null) : base(logger, settings)
    {
        Protocol = protocol;
        Commands = Protocol.Commands;
        Version = Protocol.Version;
        Label = protocol.Name;
    }

    public override byte[] Commands { get; }
    public override byte Version { get; }
    protected ProtocolSettings? Protocol { get; }
}