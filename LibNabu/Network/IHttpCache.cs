namespace Nabu.Network
{
    public interface IHttpCache
    {
        string? CachePath(string uri);

        Task<Memory<byte>> GetBytes(string uri);

        Task<string?> GetFile(string uri, bool bypassCache = false);

        Task<HttpResponseMessage?> GetHead(string uri);

        Task<string> GetString(string uri);

        Task<UriStatus> GetUriStatus(string uri, string? path = null);
    }
}