

namespace Gry.Caching
{
    public record CacheOptions
    {
        public const string FolderName = "cache";
        public int MinimumCacheTimeMinutes { get; set; } = 5;
    }
}
