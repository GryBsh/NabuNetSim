using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Lgc;

public abstract class Module : IModule
{
    public Module() 
    {
        Assembly = GetType().Assembly;
        Resources = new AssemblyResources(this); 
    }
    public Module(object? options = null, bool enableDiscovery = true) : this()
    {
        _options = options;
        Discovery = enableDiscovery;
    }

    public bool Discovery { get; }

    protected virtual List<Type> ModuleTypes { get; } = [];

    public virtual IEnumerable<Type> ServiceTypes
    {
        get
        {
            if (!Discovery) return ModuleTypes.ToArray();

            return from serviceType in Runtime.ServiceTypes
                   from type in Runtime.GetImplementationsOf(
                        serviceType,
                        Assembly
                   ) select type;
        }
     }
    private Assembly Assembly { get; }
    private AssemblyResources? Resources { get; }

    public T? Resource<T>(string name)
    {
        if (Resources is not null)
            return Resources.Get<T>(name);
        return default;
    }

    readonly object? _options;
    public T? Options<T>() => (T?)_options;
}