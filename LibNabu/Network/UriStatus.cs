namespace Nabu.Network
{
    public record UriStatus(bool ShouldDownload, bool Found, bool Cached, DateTime CacheTime, int length = 0);
}