using Gry.Adapters;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Gry
{
    public static partial class Directories
    {
        public static IEnumerable<string> List(string path, AdapterDefinition settings, params string[] patterns)
        {
            path = Files.FilePath(settings, path);
            return List(path, patterns);
        }

        public static IEnumerable<string> List(string path, params string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                var literalPath = Path.Combine(path, pattern);
                if (Path.Exists(literalPath))
                    yield return literalPath;
            }

            Matcher matcher = new();
            matcher.AddIncludePatterns(patterns);

            var matches = matcher.GetResultsInFullPath(Path.GetFullPath(path));

            foreach (var match in matches)
                yield return match;
        }
    }
}