﻿using Gry;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Napa;

public record ManifestItem : OptionsObject
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
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string? Author
    {
        get => Get<string>(nameof(Author));
        set => Set(nameof(Author), value);
    }
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string? Description
    {
        get => Get<string>(nameof(Description));
        set => Set(nameof(Description), value);
    }
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string? TileColor
    {
    }
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults)]
    public string? TilePattern
}