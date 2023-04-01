using System.Net;
using Nabu.Services;

namespace Nabu.Network.NHACP.V01;

public class HttpStorageHandler : RAMStorageHandler
{
    public HttpStorageHandler(IConsole logger, AdaptorSettings settings) : base(logger, settings)
    {
    }

    public override async Task<(bool, string, int, NHACPError)> Open(OpenFlags flags, string uri)
    {
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                Buffer = await response.Content.ReadAsByteArrayAsync();
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
