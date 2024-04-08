using System;

namespace Lgc;

public static partial class Runtime
{
    /// <summary>
    /// Renders types invisible to the runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class InvisibleAttribute : Attribute
    {
    }

}