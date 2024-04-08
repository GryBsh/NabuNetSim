using Lgc.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lgc;

//public delegate Assembly AssemblyFactory(string path);



public static partial class Runtime
{
    public delegate object? InstanceFactory(Type type, params object[] args);

    private static InstanceFactory Activator { get; set; } = System.Activator.CreateInstance;

    private static Dictionary<Type, IModule> Modules { get; } = [];

    public static T? Activate<T>(params object[] args) => Activate<T, T>(args);

    public static T? Activate<T>(Type type, params object[] args) => (T?)Activate(type, args);

    public static Y? Activate<T, Y>(params object[] args) => (Y?)Activate(typeof(T), args);

    public static object? Activate(Type t, params object[] args)
            => Activator(t, args);

    public static void AddModule<T>(object? options = null, bool enableDiscovery = true) where T : IModule
    {
        if (Modules.ContainsKey(typeof(T))) return;

        var module = options switch
        {
            null => Activate<T>(),
            not null => Activate<T>(options, enableDiscovery)
        } ?? throw new InvalidOperationException("Cannot activate module");

        Modules.Add(typeof(T), module);
    }

    public static IEnumerable<Type> AssignableFrom(Type type, IEnumerable<Type>? types = default)
    {
        return (types ?? GetTypes()).Where(type.IsAssignableFrom);
    }

    public static bool AssignableToAny(Type type, IEnumerable<Type> types)
            => types.Any(type.IsAssignableTo);

    public static IEnumerable<Assembly> GetAssemblies()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        return from a in assemblies //.Concat(ExternalAssemblies)
               where IsValidAssembly(a)
               select a;
    }

    //t.IsGenericTypeDefinition is false;
    public static IEnumerable<Type> GetImplementationsOf(Type type)
        => GetImplementationsOf(type, GetTypes());

    public static IEnumerable<Type> GetImplementationsOf(Type type, Assembly assembly)
           => GetImplementationsOf(type, GetTypes(assembly));

    public static IEnumerable<Type> GetImplementationsOf(Type type, Assembly[] assembly)
            => GetImplementationsOf(type, GetTypes(assembly));

    public static IEnumerable<Type> GetImplementationsOf(Type type, IEnumerable<Type>? types = default)
    {
        types ??= GetTypes();
        return from assignable in AssignableFrom(type, types)
               where IsVisible(assignable) &&
                     IsValidServiceType(assignable)
               select assignable;
    }

    public static IEnumerable<Type> GetImplementationsOf<T>(IEnumerable<Type>? types = default)
            => GetImplementationsOf(typeof(T), types);

    public static IEnumerable<Type> GetImplementationsOf<T>(Assembly[] assembly)
            => GetImplementationsOf(typeof(T), assembly);

    public static IEnumerable<Type> GetInterfacesImplementing(Type desired, Type t)
    {
        if (desired.IsInterface is false)
            throw new InvalidOperationException("Desired Type must be an interface");

        var tVisible = IsVisible(t);
        var dVisible = IsVisible(desired);

        if (tVisible is false || dVisible is false)
            return Array.Empty<Type>();

        return from implemented in t.GetInterfaces()
               where IsVisible(implemented)
               where desired.IsAssignableFrom(implemented)
               select implemented;
    }

    public static IEnumerable<Type> GetInterfacesImplementing<T>(Type t)
            => GetInterfacesImplementing(typeof(T), t);

    public static IEnumerable<Type> GetLineage(Type type)
    {
        yield return type;
        while (type.BaseType != null &&
               type.BaseType != typeof(object))
            yield return type = type.BaseType;
    }

    /// <summary>
    ///     Gets all exported public types in either all assembles or the specified assembles
    /// </summary>
    /// <param name="assembly">(optional) the specific assemblies from which to list types</param>
    /// <returns>
    ///     An enumerable of types
    /// </returns>
    public static IEnumerable<Type> GetTypes(params Assembly[] assembly)
    {
        assembly = assembly.Length == 0 ? GetAssemblies().ToArray() : assembly;

        return from a in assembly
               from t in a.GetExportedTypes()
               where t.IsPublic
               select t;
    }

    public static IEnumerable<Type> GetServiceTypes()
        => Options?.Strict switch
        {
            true => Modules.Values.SelectMany(m => m.ServiceTypes),
            false or null 
                => ServiceTypes
                       .SelectMany(GetImplementationsOf)
                       .Distinct()
        };

    public static bool IsValidAssembly(Assembly a)
            => a.IsDynamic is false;

    public static bool IsValidServiceType(Type t)
            => t.IsAbstract is false &&
               t.IsInterface is false;

    // TODO: Actually Load External Assemblies
    //private static IList<Assembly> ExternalAssemblies { get; } = new List<Assembly>();
    private static bool IsVisible(Type t)
        => t.CustomAttributes.Any(a => a.AttributeType == typeof(InvisibleAttribute)) is false &&
            Ignored.Contains(t) is false;

    /*
    protected static AssemblyFactory AssemblyFactory { get; set; } = (path) => default;

    public static Assembly LoadAssembly(string path)
    {
        var assembly = AssemblyFactory(path);
        if (assembly != default) ExternalAssemblies.Add(assembly);
        return assembly;
    }
    */
    //&&
}