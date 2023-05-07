namespace Nabu.Network
{
    public record UriStatus(bool ShouldDownload, bool Found, bool Cached, DateTime CacheTime);

    public interface IHttpCache
    {
        Task<UriStatus> GetUriStatus(string uri, string? path = null);
        Task<byte[]> GetBytes(string uri);
        Task<HttpResponseMessage> GetHead(string uri);
        Task<string> GetString(string uri);
    }
}