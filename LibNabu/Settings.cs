using Nabu;
using Nabu.Network;

/// <summary>
///     Emulators setings. This is merged with config on startup.
/// </summary>
public class Settings {
    public string StoragePath { get; set; } = "./Files";
    public AdaptorCollection Adaptors { get; set; } = new();
    public List<ProtocolSettings> Protocols { get; set; } = new();
    public List<ProgramSource> Sources { get; set; } = new();

    public List<string> Flags { get; set; } = new();

    public bool EnablePython { get; set; } = false;
    public bool EnableJavaScript { get; set; } = false;

    public int MaxUIEntryAgeMinutes { get; set; } = 30;
    public int MaxLogEntryAgeDays { get; set; } = 1;
    public int LogCleanupIntervalHours { get; set; } = 1;

    public bool EnableLocalFileCache { get; set; } = true;
    
    public string CacheDatabasePath { get; set; } = "cache.db";
    public string DatabasePath { get; set; } = "data.db";
}

