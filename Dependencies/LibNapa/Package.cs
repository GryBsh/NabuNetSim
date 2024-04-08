using Gry;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Napa;

/// <summary>
/// A <see cref="Model"/> that contains named option values
/// </summary>
public record OptionsObject : Model
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
            return Convert<T>(value, TryConvert<T>);
        }
        return default;
    }

}

public record Package : OptionsObject
{
    public string Id
    {
        get => Get<string>(nameof(Id)) ?? string.Empty;
        set => Set(nameof(Id), value);
    }

    public string Name
    {
        get => Get<string>(nameof(Name)) ?? string.Empty;
        set => Set(nameof(Name), value);
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string Description
    {
        get => Get<string>(nameof(Description)) ?? string.Empty;
        set => Set(nameof(Description), value);
    }

    public string Version
    {
        get => Get<string>(nameof(Version)) ?? string.Empty;
        set => Set(nameof(Version), value);
    }

    public string Author
    {
        get => Get<string>(nameof(Author)) ?? string.Empty;
        set => Set(nameof(Author), value);
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string Url
    {
        get => Get<string>(nameof(Url)) ?? string.Empty;
        set => Set(nameof(Url), value);
    }

    public IDictionary<string, ManifestItem[]?>? Manifest
    {
        get => Get<IDictionary<string, ManifestItem[]?>>(nameof(Manifest));
        set => Set(nameof(Manifest), value);
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public IDictionary<string, bool>? Features
    {
        get => Get<IDictionary<string, bool>>(nameof(Features));
        set => Set(nameof(Features), value);
    }

    public bool FeatureEnabled(string feature)
    {
        var features = Features;
        if (features?.TryGetValue(feature, out var enabled) is true)
        {
            return enabled;
        }
        return false;
    }

    [JsonIgnore()]
    [YamlIgnore()]
    public ManifestItem[] Programs
    {
        get
        {
            ManifestItem[]? programs = [];
            Manifest?.TryGetValue(PackageFeatures.Programs, out programs);
            if (programs == null) return [];
            return programs;
        }
    }

    [JsonIgnore()]
    [YamlIgnore()]
    public ManifestItem[] Storage
    {
        get
        {
            ManifestItem[]? storage = [];
            Manifest?.TryGetValue(PackageFeatures.Storage, out storage);
            if (storage == null) return [];
            return storage;
        }
    }

    [JsonIgnore()]
    [YamlIgnore()]
    public ManifestItem[] PAKs
    {
        get
        {
            ManifestItem[]? paks = [];
            Manifest?.TryGetValue(PackageFeatures.PAKs, out paks);
            if (paks == null) return [];
            return paks;
        }
    }

    [JsonIgnore()]
    [YamlIgnore()]
    public ManifestItem[] Sources
    {
        get
        {
            ManifestItem[]? sources = [];
            Manifest?.TryGetValue(PackageFeatures.Sources, out sources);
            if (sources == null) return [];
            return sources;
        }
    }

    protected override (bool, object?) TryConvert<T>(object? value)
    {
        return typeof(T) switch
        {
            _   when typeof(T) == typeof(DataDictionary<ManifestItem[]?>) && 
                     value is IDictionary<string, ManifestItem[]?> list
                => (true, new DataDictionary<ManifestItem[]?>(list)),
            _   => (false, value)
        };
    }
}