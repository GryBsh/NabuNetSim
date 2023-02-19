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
    public Dictionary<string, object> Options { get; set; } = new();
}

