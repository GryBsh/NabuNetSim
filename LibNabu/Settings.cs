using Nabu;
using Nabu.Network;
using Nabu.Packages;

/// <summary>
///     Emulators setings. This is merged with config on startup.
/// </summary>
public class Settings {
    //public string StoragePath { get; set; } = "./Files";
    public AdaptorCollection Adaptors { get; set; } = new();
    public List<ProtocolSettings> Protocols { get; set; } = new();
    public List<ProgramSource> Sources { get; set; } = new();
    public List<PackageSource> PackageSources { get; set; } = new();

    public bool EnablePython { get; set; } = false;
    public bool EnableJavaScript { get; set; } = false;

    public int MaxLogEntries { get; set; } = 1000;
    public int MaxLogEntryAgeHours { get; set; } = 4;
    public int LogCleanupIntervalMinutes { get; set; } = 15;

    public bool EnableLocalFileCache { get; set; } = true;
    
    public string CacheDatabasePath { get; set; } = "cache.db";
    public string DatabasePath { get; set; } = "data.db";
}

