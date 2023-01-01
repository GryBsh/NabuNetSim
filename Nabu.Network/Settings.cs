using Nabu;
using Nabu.Network;

public class Settings {
    public RelaySettings Relay { get; set; } = new();
    public Dictionary<string, ImageSourceDefinition> Sources { get; set; } = new();
    public AdaptorSettings[] Adaptors { get; set; } = Array.Empty<AdaptorSettings>();
}