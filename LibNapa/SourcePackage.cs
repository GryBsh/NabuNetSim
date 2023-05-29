using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Napa;

public record SourcePackage : Package
{
    public SourcePackage(Package package, string source, string path) : base(package)
    {
        Source = source;
        Path = path;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string Source
    {
        get => Get<string>(nameof(Source)) ?? string.Empty;
        set => Set(nameof(Source), value);
    }

    public string Path
    {
        get => Get<string>(nameof(Path)) ?? string.Empty;
        set => Set(nameof(Path), value);
    }
}

