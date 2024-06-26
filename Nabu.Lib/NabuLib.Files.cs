﻿using Gry.Adapters;
using Nabu.Settings;
using System.Text.RegularExpressions;

namespace Nabu
{
    public static partial class NabuLib
    {
        public static string ApplyRedirect(AdapterDefinition settings, string filePath)
        {
            var modified = false;
            do
            {
                foreach (var redirect in settings.StorageRedirects)
                {
                    if (redirect.Key == filePath)
                    {
                        filePath = redirect.Value;
                        modified = true;
                    }
                }
            } while (modified is true);
            return filePath;
        }

        

        public static Task<bool> FileAvailable(string path)
        {
            return Task.Run(() =>
            {
                return IsFileAvailable(path);
            });
        }

        public static string FilePath(AdapterDefinition settings, string filePath)
        {
            filePath = ApplyRedirect(settings, filePath);
            filePath = SanitizePath(filePath);
            filePath = Path.Combine(settings.StoragePath, filePath);

            if (IsSymLink(filePath))
            {
                filePath = ResolveLink(filePath) ?? filePath;
            }
            return filePath;
        }

        public static int FileSize(string path)
        {
            return (int)new FileInfo(path).Length;
        }

        public static bool LowerEquals(this string path1, string path2)
        {
            return path1.Equals(path2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsFileAvailable(string path)
        {
            try
            {
                using FileStream inputStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return inputStream.Length > 0;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static bool IsHttp(string path)
            => Http().IsMatch(path);

        public static bool IsSymLink(string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists) return false;
            return file.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        public static Func<T, bool> Match<T>(Func<T, string> value, string desired)
                                    => s => LowerEquals(value(s), desired);

        public static (bool, string) PathInfo(AdapterDefinition settings, string filePath)
        {
            var isSymLink = false;

            filePath = ApplyRedirect(settings, filePath);
            filePath = Path.Combine(settings.StoragePath, filePath);
            //filePath = SanitizePath(filePath);
            if (IsSymLink(filePath))
            {
                isSymLink = true;
                filePath = File.ResolveLinkTarget(filePath, true)?.FullName ?? filePath;
            }
            return (isSymLink, filePath);
        }

        public static string PlatformPath(string path)
        {
            return path.Replace(@"\", $"{Path.DirectorySeparatorChar}")
                       .Replace('/', Path.DirectorySeparatorChar);
        }

        public static string ResolveLink(string filePath)
            => File.ResolveLinkTarget(filePath, true)?.FullName ?? filePath;

        

        public static string SanitizePath(string path)
        {
            return path.Replace("..", string.Empty).Replace(":", string.Empty);
        }

        public static string Uri(AdapterDefinition settings, string uri)
        {
            uri = ApplyRedirect(settings, uri);
            return uri;
        }

        [GeneratedRegex("[hH][tT]{2}[pP][sS]?://.*")]
        private static partial Regex Http();
    }
}