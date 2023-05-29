using Nabu.Services;

namespace Nabu.Network.NHACP.V01;

public class RAMStorageHandler : INHACPStorageHandler
{
    protected ILog Logger;
    protected AdaptorSettings Settings;
    protected Memory<byte> Buffer = Array.Empty<byte>();

    public int Position { get; set; } = 0;
    public string Path { get; set; } = string.Empty;
    public RAMStorageHandler(ILog logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    public virtual Task<(bool, string, int, NHACPError)> Open(OpenFlags flags, string uri)
    {
        try
        {
            Path = uri;
            uri = uri.Replace("0x", string.Empty);
            var size = int.Parse(uri);
            Buffer = new byte[size];
            return Task.FromResult((true, string.Empty, Buffer.Length, NHACPError.Undefined));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message, 0, NHACPError.Undefined));
        }
    }

    public Task<(bool, string, Memory<byte>, NHACPError)> Get(int offset, int length, bool realLength = false)
    {
        try
        {
            if (offset >= Buffer.Length) return Task.FromResult((false, "Offset beyond end of file", (Memory<byte>)Array.Empty<byte>(), NHACPError.InvalidRequest));
            var (_, buffer) = NabuLib.Slice(Buffer, offset, length);
            if (realLength is false && buffer.Length != length) {
                var read = buffer;
                buffer = new Memory<byte>(new byte[length]);
                read.CopyTo(buffer[..read.Length]);
            }
            return Task.FromResult((true, string.Empty, buffer, NHACPError.Undefined));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message, (Memory<byte>)Array.Empty<byte>(), NHACPError.Undefined));
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
        return (true, Position += offset, string.Empty, NHACPError.Undefined);
    }
    public (bool, string, string, NHACPError) Info()
    {
        if (Path == string.Empty) return (false, string.Empty, "Memory Bank does not exist", NHACPError.InvalidRequest);
        return (true, Path, string.Empty, 0);
    }

    public (bool, int, string, NHACPError) SetSize(int size)
    {
        var tmpBuffer = new Memory<byte>(new byte[size]);
        Buffer.CopyTo(tmpBuffer);
        Buffer = tmpBuffer;
        return (true, size, string.Empty, 0);
    }

    public Task<(bool, string, Memory<byte>, NHACPError)> Read(int length)
    {
        var (_, slice) = NabuLib.Slice(Buffer, Position, length);
        Position += length;
        return Task.FromResult((true, string.Empty, slice, NHACPError.Undefined));
    }

    public Task<(bool, string, NHACPError)> Write(Memory<byte> buffer)
    {
        var length = buffer.Length + Position;
        if (length > Buffer.Length)
        {
            var temp = new Memory<byte>(new byte[length]);
            Buffer.CopyTo(temp);
            Buffer = temp;
        }
        buffer.CopyTo(Buffer[Position..]);
        return Task.FromResult((true, string.Empty, NHACPError.Undefined));
    }

    public (bool, string, NHACPError) ListDir(string pattern)
    {
        throw new NotImplementedException();
    }

    public (bool, string?, string, NHACPError) GetDirEntry(byte maxNameLength)
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
        End();
        return Task.CompletedTask;
    }
}
