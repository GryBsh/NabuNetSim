using Nabu;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Napa;

public record ManifestItem : DeserializedWithOptions
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string Path
    {
        get => Get<string>(nameof(Path)) ?? string.Empty;
        set => Set(nameof(Path), value);
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string? Type
    {
        get => Get<string>(nameof(Type));
        set => Set(nameof(Type), value);
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string? Name
    {
        get => Get<string>(nameof(Name));
        set => Set(nameof(Name), value);
    }
}