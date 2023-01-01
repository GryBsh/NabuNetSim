using Microsoft.Extensions.Logging;

namespace Nabu.ACP;

public class RAMStorageHandler : IStorageHandler
{
    protected ILogger Logger;
    protected AdaptorSettings Settings;
    protected byte[] Buffer = Array.Empty<byte>();

    public RAMStorageHandler(ILogger logger, AdaptorSettings settings)
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
            var (_, buffer) = NabuLib.Slice(Buffer, offset, length);
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
            var length = buffer.Length + offset;
            if (length > Buffer.Length)
            {
                var temp = new byte[length];
                Buffer.AsSpan().CopyTo(temp);
                Buffer = temp;
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
