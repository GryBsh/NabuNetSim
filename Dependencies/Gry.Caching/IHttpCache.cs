namespace Gry.Caching
{
    public interface IHttpCache
    {
        string? CachePath(string uri);

        Task<Memory<byte>> GetBytes(string uri);

        Task<string?> GetFile(string uri, bool bypassCache = false);

        Task<HttpResponseMessage?> Head(string uri);

        Task<string> GetString(string uri);

        Task<PathStatus> GetPathStatus(string uri, string? path = null);
        Task<Memory<byte>> DownloadAndCache(string uri, string path, string name);
        (string, string) CacheFileNames(string uri);
    }
}