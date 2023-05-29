using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Joins;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nabu
{
    public static partial class NabuLibEx
    {
        public static IEnumerable<string> List(string path, AdaptorSettings settings, params string[] patterns)
        {
            path = NabuLib.FilePath(settings, path);
            Matcher matcher = new();
            matcher.AddIncludePatterns(patterns);
            return matcher.GetResultsInFullPath(Path.GetFullPath(path));
        }
    }
}
