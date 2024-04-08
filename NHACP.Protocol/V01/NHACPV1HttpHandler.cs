using Gry.Adapters;
using Gry.Caching;
using Microsoft.Extensions.Logging;
using System.Net;
using YamlDotNet.Serialization;

namespace NHACP.V01
{
    public class NHACPV1HttpHandler(ILogger logger, AdapterDefinition adaptor, HttpClient http, IFileCache cache, CacheOptions cacheOptions) : NHACPV1RamHandler(logger, adaptor)
    {
        private readonly HttpCache Http = new(http, logger, cache, cacheOptions, adaptor.CacheFolderPath(cacheOptions));

        public override async Task<(bool, string, uint, NHACPErrors)> Open(NHACPOpenFlags flags, string uri)
        {
            try
            {
                //var response = await Http.GetHead(uri);
                var response = await Http.GetPathStatus(uri);
                if (response.Found || response.Cached)
                {
                    Buffer = await Http.GetBytes(uri);
                    return (true, string.Empty, (uint)Buffer.Length, NHACPErrors.Undefined);
                }
                else
                {
                    NHACPErrors errorCode = response?.StatusCode switch
                    {
                        HttpStatusCode.Forbidden or
                            HttpStatusCode.Unauthorized => NHACPErrors.AccessDenied,
                        HttpStatusCode.NotFound => NHACPErrors.NotFound,
                        _ => NHACPErrors.Undefined // Undefined error
                    };
                    return (false, response?.ReasonPhrase ?? string.Empty, 0, errorCode);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message, 0, NHACPErrors.Undefined);
            }
        }
    }
}