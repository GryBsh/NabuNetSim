using System.Net;
using Nabu.Services;

namespace Nabu.Network.NHACP.V01;

public class HttpStorageHandler : RAMStorageHandler
{
    readonly CachingHttpClient Http;
    public HttpStorageHandler(IConsole logger, AdaptorSettings settings, HttpClient http, FileCache cache) : base(logger, settings)
    {
        Http = new CachingHttpClient(http, logger, cache);
    }

    public override async Task<(bool, string, int, NHACPError)> Open(OpenFlags flags, string uri)
    {
        try
        { 
            uri = NabuLib.Uri(Settings, uri);
            var response = await Http.GetHead(uri);
            if (response.IsSuccessStatusCode)
            {
                Buffer = await Http.GetBytes(uri);
                return (true, string.Empty, Buffer.Length, NHACPError.Undefined);
            }
            else
            {
                NHACPError errorCode = response.StatusCode switch {
                    HttpStatusCode.Forbidden or 
                        HttpStatusCode.Unauthorized => NHACPError.PermissionDenied,
                    HttpStatusCode.NotFound         => NHACPError.NotFound,
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
