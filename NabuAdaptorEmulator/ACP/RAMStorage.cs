using Microsoft.Extensions.Logging;
using Nabu.Adaptor;

namespace Nabu.Network;

public class RAMStorage : IStorageHandler
{
    protected ILogger Logger;
    protected AdaptorSettings Settings;
    protected byte[] Buffer = Array.Empty<byte>();

    public RAMStorage(ILogger logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    public virtual Task<(bool, string, int)> Open(short flags, string uri)
    {
        try
        {
            Buffer = new byte[0x100000];
            return Task.FromResult((true, string.Empty, Buffer.Length));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message, 0));
        }
    }

    public (bool, string, byte[]) Get(int offset, short length)
    {
        try
        {
            int l = length;
            if (length > Buffer.Length)
                l = Buffer.Length - offset;

            var buffer = Buffer.AsSpan(offset, l).ToArray();
            return (true, string.Empty, buffer);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, Array.Empty<byte>());
        }
    }

    public virtual (bool, string) Put(int offset, byte[] buffer)
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
