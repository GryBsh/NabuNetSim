using System.Text.RegularExpressions;

namespace Napa
{
    public static partial class NapaLib
    {
        public static void EnsureFolder(string folder)
        {
            if (Directory.Exists(folder) is false)
                Directory.CreateDirectory(folder);
        }

        public static bool LowerEquals(this string path1, string path2)
        {
            return path1.Equals(path2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsHttp(string path)
            => Http().IsMatch(path);

        [GeneratedRegex("[hH][tT]{2}[pP][sS]?://.*")]
        private static partial Regex Http();

        public static string PlatformPath(string path)
        {
            return path.Replace(@"\", $"{Path.DirectorySeparatorChar}")
                       .Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
