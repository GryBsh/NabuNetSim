using Nabu;
using Nabu.Network;

/// <summary>
///     Emulators setings. This is merged with config on startup.
/// </summary>
public class Settings {
    public RelaySettings Relay { get; set; } = new();
    public Dictionary<string, ImageSourceDefinition> Sources { get; set; } = new();
    public AdaptorSettings[] Adaptors { get; set; } = Array.Empty<AdaptorSettings>();
}