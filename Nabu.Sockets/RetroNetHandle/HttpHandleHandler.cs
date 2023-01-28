using Microsoft.Extensions.Logging;
using Nabu.Network.RetroNet;

namespace Nabu.Network.RetroNetHandle;

public class HttpHandleHandler : RetroNetMemoryHandle
{
    public HttpHandleHandler(ILogger logger, AdaptorSettings settings) : base(logger, settings)
    {

    }

    public override async Task<bool> Open(string uri, FileOpenFlags flags, CancellationToken cancel)
    {
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(uri, cancel);

            if (response.IsSuccessStatusCode)
            {
                Buffer = await response.Content.ReadAsByteArrayAsync(cancel);
                Created = DateTime.Now;
                uri = uri.Replace("://", "-").Replace("/", "-");
                Filename = uri;
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Error(ex.Message);
            return false;
        }
    }
}
