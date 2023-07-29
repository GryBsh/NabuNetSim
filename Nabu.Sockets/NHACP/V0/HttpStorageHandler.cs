using Nabu.Services;
using System.Net;

namespace Nabu.Network.NHACP.V0;

public class HttpStorageHandler : RAMStorageHandler
{
    public HttpStorageHandler(ILog logger, AdaptorSettings settings) : base(logger, settings)
    {
    }

    public override async Task<(bool, string, int)> Open(ushort flags, string uri)
    {
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                Buffer = await response.Content.ReadAsByteArrayAsync();
                return (true, string.Empty, Buffer.Length);
            }
            else
            {
                short errorCode = response.StatusCode switch
                {
                    HttpStatusCode.Forbidden or
                        HttpStatusCode.Unauthorized => 0x07,
                    HttpStatusCode.NotFound => 0x03,

                    _ => 0x00 // Undefined error
                };
                return (false, response.ReasonPhrase ?? string.Empty, errorCode);
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 0);
        }
    }
}