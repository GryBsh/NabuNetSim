namespace Nabu.Network
{
    public interface IHttpCache
    {
        Task<(bool, bool, bool, DateTime)> GetUriStatus(string uri, string? path = null);
        Task<byte[]> GetBytes(string uri);
        Task<HttpResponseMessage> GetHead(string uri);
        Task<string> GetString(string uri);
    }
}