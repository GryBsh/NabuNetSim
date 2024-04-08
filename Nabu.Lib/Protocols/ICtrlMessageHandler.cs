using Gry.Adapters;
using Gry.Protocols;
using Lgc;

namespace Nabu.Protocols
{
    public interface ICtrlMessageHandler<TOptions, TMessage> : IDependency
        where TOptions : AdapterDefinition
    {
        TMessage[] Types { get; }

        Task<CtrlItem> Set(
            Protocol<TOptions> protocol,
            TMessage type,
            Memory<TMessage> data
        );

        Task<IEnumerable<CtrlItem>> List(
            Protocol<TOptions> protocol,
            TMessage type,
            Memory<TMessage> data
        );
    }
}
