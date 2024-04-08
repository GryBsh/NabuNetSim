using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lgc;

//public delegate IServiceCollection TypeRegistrar(Type serviceType, Type implementationType);

public static class Normalize
{
    public static IList<string> Prefixes { get; } = new List<string>();

    public static IList<string> Suffixes { get; } = new List<string>
    {
        "Options",
        "Settings"
    };

    public static string Name(string name)
    {

        var prefixes = string.Join("|", Prefixes);
        var suffixes = string.Join("|", Suffixes);
        var matches = Regex.Matches(name, $@"^({prefixes})?(?<name>.+)({suffixes})$");
        if (matches.Count == 0) return name;
        
        return matches[0].Groups["name"].Value;

        //foreach (var word in Suffixes.Concat(removed))
        //    name = name.Replace(word, string.Empty);
        //return name;
    }
}
