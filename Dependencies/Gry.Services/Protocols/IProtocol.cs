using Gry.Adapters;
using Lgc;

namespace Gry.Protocols
{
    public interface IProtocolHostInfo : IDependency
    {
        string Name { get; }
        string Description { get; }
        string Version { get; }
    }

    public interface IProtocol<in TOptions> : IProtocol<byte, TOptions>
        where TOptions : AdapterDefinition;
    

    public interface IProtocol<TMessage, in TAdapterDefinition> 
        where TAdapterDefinition : AdapterDefinition
    {
        bool Attached { get; }
        TMessage[] Messages { get; }

        bool Attach(TAdapterDefinition settings, Stream stream);

        void Detach();

        Task<bool> HandleMessage(TMessage unhandled, CancellationToken cancel);

        void Reset();

        bool ShouldHandle(TMessage unhandled);
    }
}