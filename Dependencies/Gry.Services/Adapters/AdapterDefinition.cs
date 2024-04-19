using Gry.Caching;
using Gry.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using YamlDotNet.Serialization;

namespace Gry.Adapters;


[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
public record AdapterDefinition : Model
{
    protected AdapterDefinition() { 
        Enabled = true;
        Timeout = TimeSpan.FromSeconds(1);
        StoragePath = string.Empty;
        StorageRedirects = [];
        Type ??= nameof(AdapterType.None);
        EnableCopyOnSymLinkWrite = true;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual string Type { get; init; }
    public string? Name
    {
        get => Get<string>(nameof(Name));
        set => Set(nameof(Name), value);
    }



    #region Settings

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Port", Section = "Port",  Description = "The port to use", Options = SettingValueType.Port)]
    public string? Port
    {
        get => Get<string?>(nameof(Port));
        set => Set(nameof(Port), value);
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Enabled", Description = "If the adapter is enabled (running)")]
    public bool Enabled
    {
        get => Get<bool>(nameof(Enabled));
        set => Set(nameof(Enabled), value);
    }
    

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    //[Setting("Timeout", Advanced = true, Section = "Port", Description = "The timeout for the adapter loop")]
    public TimeSpan Timeout
    {
        get => Get<TimeSpan>(nameof(Timeout));
        set => Set(nameof(Timeout), value);
    }    public bool Attached { get; set; } = false;

    #endregion

    #region Current Adapter State

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Adapter? Adapter
    {
        get => Get<Adapter>(nameof(Adapter));
        set        {            Set(nameof(Adapter), value);            Set(nameof(Attached), value != null);        }
    }

    #endregion

    #region Storage

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string StoragePath
    {
        get => Get<string>(nameof(StoragePath))!;
        set => Set(nameof(StoragePath), value);
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, string> StorageRedirects
    {
        get => Get<Dictionary<string, string>>(nameof(StorageRedirects))!;
        set => Set(nameof(StorageRedirects), value);
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Copy on SymLink write", 
                Advanced = true, 
                Section = "Storage",
                Description = "When SymLinks are enabled, a write to a symlink will cause the file to be copied to location of the symlink."
    )]
    public bool EnableCopyOnSymLinkWrite
    {
        get => Get<bool>(nameof(EnableCopyOnSymLinkWrite));
        set => Set(nameof(EnableCopyOnSymLinkWrite), value);
    }

    #endregion

    
}