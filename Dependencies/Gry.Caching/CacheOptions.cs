

namespace Gry.Caching
{
    public record CacheOptions
    {
        public const string DefaultFolderName = "cache";

        public string HttpCacheFolderName { get; set; } = DefaultFolderName;
        public int MinimumCacheTimeMinutes { get; set; } = 5;
    }
}
