using Nabu.Services;

namespace Nabu.Network;

public interface IProtocol
{
    bool Attached { get; }
    byte[] Commands { get; }

    bool Attach(AdaptorSettings settings, Stream stream);

    void Detach();

    Task<bool> HandleMessage(byte unhandled, CancellationToken cancel);

    void Reset();

    bool ShouldAccept(byte unhandled);
}

public interface IProtocolFactory
{
    string Type { get; }

    IProtocol CreateProtocol(IServiceProvider provider, ILog logger, ProtocolSettings protocol);
}