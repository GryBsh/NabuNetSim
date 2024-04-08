using Microsoft.Extensions.Hosting;

namespace Lgc;

public interface IBuild<T>
{
    void Build(T builder);
}

/// <summary>
/// Defines a method which, when called during host creation,
/// makes alterations to the IHostBuilder before the IHost is built.
/// </summary>
public interface IBuildHost : IBuild<IHostBuilder>
{
    
}