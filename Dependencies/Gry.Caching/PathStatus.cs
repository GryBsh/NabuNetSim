using System.Net;

namespace Gry.Caching
{
    public record PathStatus(
        bool ShouldDownload, 
        bool Found, 
        bool Cached, 
        DateTime CacheTime, 
        HttpStatusCode? StatusCode = null, 
        string? ReasonPhrase = null, 
        int Length = 0
    );
}