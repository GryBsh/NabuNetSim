using Nabu.Services;

namespace Nabu.Network;

public abstract class PluginProtocol : Protocol
{
    protected ProtocolSettings? Protocol { get; private set; }

    protected PluginProtocol(ILog logger, AdaptorSettings? settings = null) : base(logger, settings)
    {
    }

    public virtual void Activate(ProtocolSettings settings)
    {
        Protocol = settings;
    }
}