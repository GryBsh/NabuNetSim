using System.IO;

namespace Lgc;

public interface IResourceService
{
    //ResourceManager Manager { get; }
    //Assembly Assembly { get; }

    Stream? GetStream(string name);
    string? GetString(string name);
    object? GetObject(string name);
    T? Get<T>(string name);
    
}
