using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lgc;

/// <summary>
/// Defines a class used to ensure types are discovered in the containing assembly.
/// </summary>
public interface IModule
{

    IEnumerable<string?> Namespaces
        => ServiceTypes
            .Select(t => t.Namespace)
            .Distinct();

    Assembly Assembly => GetType().Assembly;
    string BaseDirectory
    {
        get
        {
            try { return Assembly?.Location ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
    string BaseName => Assembly.GetName().Name ?? GetType().Namespace ?? String.Empty;

    IEnumerable<Type> ServiceTypes { get; }

    bool Contains(Type type) => ServiceTypes.Contains(type);

    T? Resource<T>(string name);
    
    bool Discovery { get; }
}