using System.IO;
using System.Reflection;
using System.Resources;

namespace Lgc;

internal class AssemblyResources : IResourceService
{
    public AssemblyResources(IModule module)
    {
        Manager = new ResourceManager(module.BaseName, module.Assembly);
        Assembly = module.Assembly;
    }

    ~AssemblyResources()
    {
        Manager.ReleaseAllResources();
    }

    public Assembly Assembly { get; }
    private ResourceManager Manager { get; }

    public T? Get<T>(string name)
    {
        var t = typeof(T);

        object? result = t switch
        {
            _ when t.IsAssignableFrom(typeof(string)) => GetString(name),
            _ when t.IsAssignableFrom(typeof(Stream)) => GetStream(name),
            _ => GetObject(name)
        };

        return (T?)result;
    }

    public Stream? GetStream(string name)
    {
        return Manager.GetStream(name);
    }

    public string? GetString(string name)
    {
        return Manager.GetString(name);
    }

    public object? GetObject(string name)
    {
        return Manager.GetObject(name);
    }
}