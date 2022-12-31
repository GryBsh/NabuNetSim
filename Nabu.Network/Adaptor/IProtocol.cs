namespace Nabu.Adaptor;

public interface IProtocol
{
    byte Identifier { get; }
    bool Attached { get; }
    bool Attach(AdaptorSettings settings, Stream stream);
    Task<bool> Listen(CancellationToken cancel, byte unhandled);
    void Detach();
}
