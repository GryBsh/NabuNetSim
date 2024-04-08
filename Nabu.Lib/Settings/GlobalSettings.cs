using Gry.Adapters;
using Gry.Caching;
using Gry.Settings;
using Lgc;
using Nabu.Sources;
using Napa;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace Nabu.Settings;

//[Runtime.SectionName("Settings")]
[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
public record GlobalSettings : AdapterServerOptions<AdaptorSettings, TCPAdaptorSettings, SerialAdaptorSettings>
{
    public static IList<string> UninstallPackageIds { get; set; } = new List<string>() {
            { "nns-bundle-iskurcpm" } // 0.9.8 has this package id, and the corrected package won't update it
            // Which then causes duplication in the UI.
        };

    //public string StoragePath { get; set; } = "./Files";
    //public AdaptorCollection Adaptors { get; set; } = new();

    //public string CacheDatabasePath { get; set; } = "cache.db";
    //[DefineSetting("Database Path")]
    //public string DatabasePath { get; set; } = "data.db";

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool DisableHeadless { get; set; } = false;

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool ForceHeadless { get; set; } = false;

    // GENERAL

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Remote Source Refresh Interval (Minutes)", Section = "Sources", Description = "How often to check for updates from remote sources")]
    public int RemoteSourceRefreshIntervalMinutes { get; set; } = 15;


    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Emulator Script Path", Description = "Path to a script which will start a local NABU Emulator")]
    public string EmulatorPath { get; set; } = string.Empty;    // STORAGE        [JsonIgnore]    [System.Text.Json.Serialization.JsonIgnore]
    public string StoragePath { get; set; } = "Files";

    // SOURCES

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Precache Remote Sources", Section = "Sources", Description = "Caches items from remote sources during refresh instead of on-demand")]
    public bool PreCacheRemoteSources { get; set; } = true;

    //public bool EnableLocalFileCache { get; set; } = true;
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Enable Symlinks", Advanced = true, Section= "Sources", Description = "Use SymLinks for storage files sourced from packages. Not recommended.")]
    public bool EnableSymLinks { get; set; } = false;

    [JsonIgnore()]    [System.Text.Json.Serialization.JsonIgnore]
    public string LocalProgramPath { get; set; } = "NABUs";
    
    // LOGS

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Log Cleanup Interval (Minutes)", Section = "Logs", Description = "How often old logs age out of the UI")]
    public int LogCleanupIntervalMinutes { get; set; } = 15;
    
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Max Log Entries", Section = "Logs", Description = "How many log entries to keep in the UI")]
    public int MaxLogEntries { get; set; } = 10000;

    // HEADLESS
   

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Headless Source", Section = "Headless", Options = SettingValueType.LauncherSource)]
    public string? HeadlessSource { get; set; }    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Headless Program", Section = "Headless", Options = SettingValueType.LauncherProgram)]
    public string? HeadlessProgram { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting(        "Full Headless Mode",         Section = "Headless",         Description = "When nns is started in headless mode, automatically start the configured emulator"    )]
    public bool StartEmulatorInHeadlessMode { get; set; } = false;

    // PLUGINS

    [JsonProperty()]
    [Setting("Enable JavaScript (Experimental)", Section = "Plugins", Advanced = true, Description = "Enable JavaScript plugin support")]
    public bool EnableJavaScript { get; set; } = false;
    [JsonProperty()]    [Setting("Refresh Interval", Section = "Packages", Description = "How often to check for updates from remote sources")]    public int PackageRefreshIntervalMinutes { get; set; } = 5;
    // Non-UI Editable Settings

    //[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    //public int MaxLogEntryDatabaseAgeDays { get; set; } = 7;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<ProgramSource> Sources { get; set; } = [];

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<ProtocolSettings> Protocols { get; set; } = [];

    //public string? CPMSource { get; set; }
    //public string? CPMProgram { get; set; }    
}
