using Microsoft.Extensions.Logging;
using Nabu.Network.RetroNet;

namespace Nabu.Network.RetroNetHandle;

public class HttpHandleHandler : RetroNetMemoryHandle
{
    HttpCache Http { get; }
    public HttpHandleHandler(IConsole logger, AdaptorSettings settings, HttpClient client ) : base(logger, settings)
    {
        Http = new HttpCache(client, Logger);
    }

    public override async Task<bool> Open(string uri, FileOpenFlags flags, CancellationToken cancel)
    {
        try
        {
            
            using var response = await Http.GetHead(uri);

            if (response.IsSuccessStatusCode)
            {
                Buffer = await Http.GetBytes(uri);
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
