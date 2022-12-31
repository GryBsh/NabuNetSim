using Microsoft.Extensions.Logging;
using Nabu.Adaptor;

namespace Nabu.ACP;

public class HttpStorageHandler : RAMStorageHandler
{
    public HttpStorageHandler(ILogger logger, AdaptorSettings settings) : base(logger, settings)
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
                return (false, response.ReasonPhrase ?? string.Empty, 0);
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 0);
        }
    }


}
