using Microsoft.Extensions.Logging;
using Nabu;
using Nabu.Network;
using System.Text;

/// <summary>
///     Emulators setings. This is merged with config on startup.
/// </summary>
public class Settings {
    public AdaptorCollection Adaptors { get; set; } = new();
    public List<SourceFolder> Sources { get; set; } = new();
}

