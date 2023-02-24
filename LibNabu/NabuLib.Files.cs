namespace Nabu
{
    public partial class NabuLib
    {
        public static string SafeFileName(string name)
        {
            foreach (var bad in Path.GetInvalidFileNameChars())
                name = name.Replace(bad, '_');
            return name;
        }

        public static string FilePath(AdaptorSettings settings, string filePath) {
            filePath = filePath.Replace("..", string.Empty).Replace(":", string.Empty);
            return Path.Combine(settings.StoragePath, filePath);
        }
    }
}
