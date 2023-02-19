﻿namespace Nabu.Network
{
    public interface IHttpCache
    {
        string CacheFolder { get; }

        Task<(bool, bool, bool)> CanGet(string uri, string? path = null);
        Task<byte[]> GetBytes(string uri);
        Task<HttpResponseMessage> GetHead(string uri);
        Task<string> GetString(string uri);
    }
}