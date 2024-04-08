using Gry.Adapters;
using Lgc;


namespace Gry.Options;

[Runtime.Invisible()]
public record NullAdapterDefinition : AdapterDefinition
{
    public override string Type { get; init; } = AdapterType.None;
}
