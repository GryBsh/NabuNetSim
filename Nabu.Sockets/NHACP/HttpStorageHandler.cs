using Microsoft.Extensions.Logging;
using Nabu.Services;

namespace Nabu.Network.NHACP;

public class HttpStorageHandler : RAMStorageHandler
{
    public HttpStorageHandler(IConsole logger, AdaptorSettings settings) : base(logger, settings)
    {
    }

    public override async Task<(bool, string, int)> Open(short flags, string uri)
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
                return (false, response.ReasonPhrase ?? string.Empty, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 500);
        }
    }


}
