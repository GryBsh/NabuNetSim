using Microsoft.Extensions.Logging;
using Nabu;
using System.Text;

/// <summary>
///     Emulators setings. This is merged with config on startup.
/// </summary>
public class Settings {
    public string StoragePath { get; set; } = "./Files";
    public AdaptorCollection Adaptors { get; set; } = new();
    public List<ProgramSource> Sources { get; set; } = new();
}

