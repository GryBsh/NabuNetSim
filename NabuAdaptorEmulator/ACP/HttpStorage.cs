using Microsoft.Extensions.Logging;
using Nabu.Adaptor;

namespace Nabu.Network;

public class HttpStorage : IStorageHandler
{
    ILogger Logger;
    AdaptorSettings Settings;
    byte[] Buffer = Array.Empty<byte>();

    public HttpStorage(ILogger logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    public async Task<(bool, string, int)> Open(short flags, string uri)
    {
        try {
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
        } catch (Exception ex)
        {
            return (false, ex.Message, 0);
        }
    }

    public (bool, string, byte[]) Get(int offset, short length)
    {
        try
        {
            var buffer = Buffer.AsSpan(offset, length).ToArray();
            return (true, string.Empty, buffer);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, Array.Empty<byte>());
        }
    }

    public (bool, string) Put(int offset, byte[] buffer)
    {
        try 
        {
            
            if (offset + buffer.Length > Buffer.Length)
            {
                var old = Buffer;
                Buffer = new byte[offset + buffer.Length];
                old.AsSpan().CopyTo(Buffer);
            }
            buffer.AsSpan().CopyTo(Buffer.AsSpan(offset));
            return (true, string.Empty);
        } 
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public void End()
    {
        Buffer = Array.Empty<byte>();
    }

    public (bool, string, byte, byte[]) Command(byte index, byte command, byte[] data)
    {
        throw new NotImplementedException();
    }
}
