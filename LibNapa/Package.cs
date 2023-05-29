using Nabu;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Napa;

public record Package : DeserializedWithOptions
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

    public ManifestItem[] Programs
    {
        get
        {
            var programs = Array.Empty<ManifestItem>();
            Manifest?.TryGetValue(PackageFeatures.Programs, out programs);
            if (programs == null) return Array.Empty<ManifestItem>();
            return programs;
        }
    }

    public ManifestItem[] Storage
    {
        get
        {
            var storage = Array.Empty<ManifestItem>();
            Manifest?.TryGetValue(PackageFeatures.Storage, out storage);
            if (storage == null) return Array.Empty<ManifestItem>();
            return storage;
        }
    }

    public ManifestItem[] PAKs
    {
        get
        {
            var paks = Array.Empty<ManifestItem>();
            Manifest?.TryGetValue(PackageFeatures.PAKs, out paks);
            if (paks == null) return Array.Empty<ManifestItem>();
            return paks;
        }
    }

    public ManifestItem[] Sources
    {
        get
        {
            var sources = Array.Empty<ManifestItem>();
            Manifest?.TryGetValue(PackageFeatures.Sources, out sources);
            if (sources == null) return Array.Empty<ManifestItem>();
            return sources;
        }
    }

    protected override object? MapValue(object? value)
    {
        return value switch
        {
            IDictionary<string, ManifestItem[]?> list and not DeserializedDictionary<ManifestItem[]?>
                => new DeserializedDictionary<ManifestItem[]?>(list),
            _ => value
        };
    }
}

