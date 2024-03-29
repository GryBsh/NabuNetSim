using Nabu.Network;
using Nabu.Packages;

namespace Nabu;

/// <summary>
///     Emulators setings. This is merged with config on startup.
/// </summary>

public class Settings
{
    public static IList<string> UninstallPackageIds { get; set; } = new List<string>() {
        { "nns-bundle-iskurcpm" } // 0.9.8 has this package id, and the corrected package won't update it
        // Which then causes duplication in the UI.
    };

    //public string StoragePath { get; set; } = "./Files";
    public AdaptorCollection Adaptors { get; set; } = new();

    //public string CacheDatabasePath { get; set; } = "cache.db";
    public string DatabasePath { get; set; } = "data.db";

    
    public bool DisableHeadless { get; set; } = false;
    public string HeadlessSource { get; set; }

    public string EmulatorPath { get; set; } = string.Empty;
    
    public bool EnableJavaScript { get; set; } = false;
    
    //public bool EnableLocalFileCache { get; set; } = true;
    public bool EnableSymLinks { get; set; } = false;

    public string StoragePath { get; set; } = "./Files";
    public string LocalProgramPath { get; set; } = "./NABUs";
    
    public int LogCleanupIntervalMinutes { get; set; } = 15;
    public int MaxLogEntryDatabaseAgeDays { get; set; } = 7;
    public int MaxLogEntries { get; set; } = 10000;
    
    public int MinimumCacheTimeMinutes { get; set; } = 5;

    public List<PackageSource> PackageSources { get; set; } = new();
    public List<string> InstallPackages { get; set; } = new();
    public List<string> UninstallPackages { get; set; } = new();
    
    public List<ProgramSource> Sources { get; set; } = new();
    public List<ProtocolSettings> Protocols { get; set; } = new();
    
    public int RemoteSourceRefreshIntervalMinutes { get; set; } = 15;
    
    
    
}