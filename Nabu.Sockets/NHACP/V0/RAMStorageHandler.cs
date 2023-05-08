using Nabu.Services;

namespace Nabu.Network.NHACP.V0;

public class RAMStorageHandler : IStorageHandler
{
    protected IConsole Logger;
    protected AdaptorSettings Settings;
    protected byte[] Buffer = Array.Empty<byte>();

    Task<T> Task<T>(T item) => System.Threading.Tasks.Task.FromResult(item);

    public RAMStorageHandler(IConsole logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    public virtual Task<(bool, string, int)> Open(short flags, string uri)
    {
        try
        {
            Buffer = new byte[0x100000];
            return Task((true, string.Empty, Buffer.Length));
        }
        catch (Exception ex)
        {
            return Task((false, ex.Message, 0));
        }
    }

    public Task<(bool, string, Memory<byte>)> Get(int offset, short length)
    {
        try
        {
            var (_, buffer) = NabuLib.Slice(Buffer, offset, length);
            return Task((true, string.Empty, buffer));
        }
        catch (Exception ex)
        {
            return Task((false, ex.Message, new Memory<byte>(Array.Empty<byte>())));
        }
    }

    public virtual Task<(bool, string)> Put(int offset, Memory<byte> buffer)
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
            buffer.CopyTo(Buffer[offset..]);
            return Task((true, string.Empty));
        }
        catch (Exception ex)
        {
            return Task((false, ex.Message));
        }
    }

    public void End()
    {
        Buffer = Array.Empty<byte>();
    }

    public Task<(bool, string, byte, byte[])> Command(byte index, byte command, byte[] data)
    {
        throw new NotImplementedException();
    }
}
