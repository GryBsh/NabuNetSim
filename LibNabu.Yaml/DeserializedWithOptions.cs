using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Nabu;

public record DeserializedWithOptions : DeserializedObject
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public IDictionary<string, object?>? Options
    {
        get => Get<IDictionary<string, object?>>(nameof(Options));
        set => Set(nameof(Options), value);
    }

    public T? Option<T>(string name)
    {
        if (Options?.TryGetValue(name, out var value) is true)
        {
            return FromValue<T>(value);
        }
        return default;
    }
}