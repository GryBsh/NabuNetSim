using Nabu.Services;
using System.Net;

namespace Nabu.Network.NHACP.V01;

public class HttpStorageHandler : RAMStorageHandler
{
    private readonly HttpCache Http;

    public HttpStorageHandler(ILog logger, AdaptorSettings adaptor, HttpClient http, IFileCache cache, Settings settings) : base(logger, adaptor)
    {
        Http = new HttpCache(http, logger, cache, settings, adaptor);
    }

    public override async Task<(bool, string, int, NHACPError)> Open(OpenFlags flags, string uri)
    {
        try
        {
            var response = await Http.GetHead(uri);
            if (response.IsSuccessStatusCode)
            {
                Buffer = await Http.GetBytes(uri);
                return (true, string.Empty, Buffer.Length, NHACPError.Undefined);
            }
            else
            {
                NHACPError errorCode = response.StatusCode switch
                {
                    HttpStatusCode.Forbidden or
                        HttpStatusCode.Unauthorized => NHACPError.AccessDenied,
                    HttpStatusCode.NotFound => NHACPError.NotFound,
                    _ => NHACPError.Undefined // Undefined error
                };
                return (false, response.ReasonPhrase ?? string.Empty, 0, errorCode);
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 0, NHACPError.Undefined);
        }
    }
}