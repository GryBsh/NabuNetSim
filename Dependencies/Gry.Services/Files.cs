using Gry.Adapters;

namespace Gry;

public static partial class Files
{
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

    public static string SanitizePath(string path)
    {
        return path.Replace("..", string.Empty).Replace(":", string.Empty);
    }

    public static bool IsSymLink(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists) return false;
        return file.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    public static string ResolveLink(string filePath)
            => File.ResolveLinkTarget(filePath, true)?.FullName ?? filePath;

    public static string Uri(AdapterDefinition settings, string uri)
    {
        uri = ApplyRedirect(settings, uri);
        return uri;
    }

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
}
