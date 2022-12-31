using Nabu.Adaptor;
using Nabu.Network;

public class Settings {
    public Dictionary<string, ImageSourceDefinition> Sources { get; set; } = new();
    public AdaptorSettings[] Adaptors { get; set; } = Array.Empty<AdaptorSettings>();
}