using Gry;
using Gry.Adapters;
using System.Data.SqlTypes;

namespace Nabu.Settings;

public record NullAdaptorSettings : AdaptorSettings, INullType
{
    public override string Type { get; init; } = AdapterType.None;
}
