using Microsoft.Extensions.Hosting;

namespace Lgc;

public interface IBuild<T>
{
    void Build(T builder);
}

/// <summary>
/// A component to perform alterations to the IHostBuilder before the host container is built.
/// </summary>
public interface IBuildHost : IBuild<IHostBuilder>
{
    
}/// <summary>/// A component to perform alterations to IHostApplicationBuilders before the application container is built./// </summary>public interface IBuildApp<T> : IBuild<T>     where T : IHostApplicationBuilder{    }