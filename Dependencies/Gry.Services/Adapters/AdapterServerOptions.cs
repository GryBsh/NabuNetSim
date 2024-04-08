using Gry.Caching;
using Lgc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Gry.Adapters;
[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
public record AdapterServerOptions<TAdapter, TTCPAdapter, TSerialAdapter>
    where TAdapter : AdapterDefinition
    where TTCPAdapter : TAdapter
    where TSerialAdapter : TAdapter
{    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<TTCPAdapter> TCP { get; set; } = [];         [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<TSerialAdapter> Serial { get; set; } = [];    [JsonIgnore]    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<TAdapter> Adapters
        => Enumerable.Concat<TAdapter>(TCP, Serial).ToArray();
}