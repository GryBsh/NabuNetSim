namespace Nabu.Adaptor;

public interface IProtocol
{
    byte[] Commands { get; }
    bool Attached { get; }
    bool Attach(AdaptorSettings settings, Stream stream);
    Task<bool> Listen(byte unhandled, CancellationToken cancel);
    void Detach();
    void Reset();
    bool ShouldAccept(byte unhandled);
}
