using Nabu.Services;

namespace Nabu.Network.NHACP.V01;

public class RAMStorageHandler : INHACPStorageHandler
{
    protected IConsole Logger;
    protected AdaptorSettings Settings;
    protected Memory<byte> Buffer = Array.Empty<byte>();

    public int Position => throw new NotImplementedException();

    public RAMStorageHandler(IConsole logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    public virtual Task<(bool, string, int, NHACPError)> Open(OpenFlags flags, string uri)
    {
        try
        {
            var size = int.Parse(uri);
            Buffer = new byte[size];
            return Task.FromResult((true, string.Empty, Buffer.Length, NHACPError.Undefined));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message, 0, NHACPError.Undefined));
        }
    }

    public Task<(bool, string, Memory<byte>, NHACPError)> Get(int offset, int length)
    {
        try
        {
            var (_, buffer) = NabuLib.Slice(Buffer, offset, length);
            return Task.FromResult((true, string.Empty, buffer, NHACPError.Undefined));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message, new Memory<byte>(Array.Empty<byte>()), NHACPError.Undefined));
        }
    }

    public virtual Task<(bool, string, NHACPError)> Put(int offset, Memory<byte> buffer)
    {
        try
        {
            var length = buffer.Length + offset;
            if (length > Buffer.Length)
            {
                var temp = new Memory<byte>(new byte[length]);
                Buffer.CopyTo(temp);
                Buffer = temp;
            }
            buffer.CopyTo(Buffer[offset..]);
            return Task.FromResult((true, string.Empty, NHACPError.Undefined));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message, NHACPError.Undefined));
        }
    }

    public void End()
    {
        Buffer = Array.Empty<byte>();
    }


    public (bool, int, string, NHACPError) Seek(int offset, NHACPSeekOrigin origin)
    {
        throw new NotImplementedException();
    }
    public (bool, string, string, NHACPError) Info()
    {
        throw new NotImplementedException();
    }

    public (bool, int, string, NHACPError) SetSize(int size)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string, Memory<byte>, NHACPError)> Read(int length)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string, NHACPError)> Write(Memory<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public (bool, string, NHACPError) ListDir(string pattern)
    {
        throw new NotImplementedException();
    }

    public (bool, string, string, NHACPError) GetDirEntry(byte maxNameLength)
    {
        throw new NotImplementedException();
    }

    public (bool, string, NHACPError) Remove(RemoveFlags removeFlags, string url)
    {
        throw new NotImplementedException();
    }

    public (bool, string, NHACPError) Rename(string oldName, string newName)
    {
        throw new NotImplementedException();
    }

    public Task Close()
    {
        throw new NotImplementedException();
    }
}
