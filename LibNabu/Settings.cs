using Nabu;
using Nabu.Network;
using Nabu.Packages;
using System.Collections.Concurrent;

namespace Nabu;

/// <summary>
///     Emulators setings. This is merged with config on startup.
/// </summary>
public class Settings {
    //public string StoragePath { get; set; } = "./Files";
    public AdaptorCollection Adaptors { get; set; } = new();
    public List<ProtocolSettings> Protocols { get; set; } = new();
    public List<ProgramSource> Sources { get; set; } = new();
    public IList<PackageSource> PackageSources { get; set; } = Array.Empty<PackageSource>();

    public bool EnablePython { get; set; } = false;
    public bool EnableJavaScript { get; set; } = false;

    public int MaxLogEntries { get; set; } = 1000;
    public int MaxLogEntryUIAgeHours { get; set; } = 24;
    public int MaxLogEntryDatabaseAgeDays { get; set; } = 7;
    public int LogCleanupIntervalMinutes { get; set; } = 15;

    public bool EnableLocalFileCache { get; set; } = true;
    
    //public string CacheDatabasePath { get; set; } = "cache.db";
    public string DatabasePath { get; set; } = "data.db";
}

