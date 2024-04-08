using Gry.Adapters;
using Gry.Settings;
using Newtonsoft.Json;

namespace Nabu.Settings;


[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
public record AdaptorSettings : AdapterDefinition
{
    public AdaptorSettings() {
        AdapterChannel = 1;
        Source = null;
        TCPServerProtocol = string.Empty;
    }

    //[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public ushort AdapterChannel
    {
        get => Get<ushort>(nameof(AdapterChannel));
        set => Set(nameof(AdapterChannel), value);
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting(
        "Source", 
        Section = "Source", 
        Description = "The source to use for the adapter", 
        Options = SettingValueType.Source
    )]
    public string? Source
    {
        get => Get<string>(nameof(Source))!;
        set => Set(nameof(Source), value);
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting(        "Program",         Section = "Source",         Description = "The program to use for the adapter",         Options = SettingValueType.Program    )]
    public string? Program
    {
        get => Get<string>(nameof(Program));
        set => Set(nameof(Program), value);
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Headless
    {
        get => Get<bool>(nameof(Headless));
        set => Set(nameof(Headless), value);
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? HeadlessSource
    {
        get => Get<string>(nameof(HeadlessSource));
        set => Set(nameof(HeadlessSource), value);
    }    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? HeadlessProgram
    {
        get => Get<string>(nameof(HeadlessProgram));
        set => Set(nameof(HeadlessProgram), value);
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ReturnToSource
    {
        get => Get<string>(nameof(ReturnToSource));
        set => Set(nameof(ReturnToSource), value);
    }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ReturnToProgram
    {
        get => Get<string>(nameof(ReturnToProgram));
        set => Set(nameof(ReturnToProgram), value);
    }

    #region TCP Server

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool TCPServerActive
    {
        get => Get<bool>(nameof(TCPServerActive));
        set => Set(nameof(TCPServerActive), value);
    }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int TCPServerPort
    {
        get => Get<int>(nameof(TCPServerPort));
        set => Set(nameof(TCPServerPort), value);
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string TCPServerProtocol
    {
        get => Get<string>(nameof(TCPServerProtocol))!;
        set => Set<string>(nameof(TCPServerProtocol), value);
    }    #endregion
    public bool ChangedSinceLoad { get; set; }    public void SetChanged() => ChangedSinceLoad = true;    public void ResetChanged() => ChangedSinceLoad = false;
}
