namespace Nabu.Adaptor;

public interface IProtocol
{
    byte Command { get; }
    bool Attached { get; }
    bool Attach(AdaptorSettings settings, Stream stream);
    Task<bool> Handle(byte unhandled, CancellationToken cancel);
    void Detach();
}
