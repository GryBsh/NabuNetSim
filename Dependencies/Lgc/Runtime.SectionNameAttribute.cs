using System;

namespace Lgc;

public static partial class Runtime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class SectionNameAttribute : Attribute
    {
        public SectionNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}