using Nabu.Services;

namespace Nabu.Network.RetroNet;

public class RetroNetMemoryHandle : NabuService, IRetroNetFileHandle
{
    public RetroNetMemoryHandle(ILog logger, AdaptorSettings settings, string? cachePath = null, Memory<byte>? buffer = null) : base(logger, settings)
    {
        if (buffer is not null)
            Buffer = buffer.Value;

        Filename = CachePath = cachePath;
    }
    protected string? CachePath { get; }
    protected Memory<byte> Buffer { get; set; } = new(Array.Empty<byte>());
    protected FileOpenFlags Flags { get; set; }
    protected DateTime Created { get; set; } = DateTime.Now;
    protected DateTime Modified { get; set; } = DateTime.Now;

    public string? Filename { get; set; }

    public virtual Task<bool> Open(string filename, FileOpenFlags flags, CancellationToken cancel)
    {
        Flags = flags;
        try
        {
            Created = DateTime.Now;
            Filename ??= filename;
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task Close(CancellationToken cancel)
    {
        Buffer = Array.Empty<byte>();
        return Task.CompletedTask;
    }

    public Task<int> Size(CancellationToken cancel)
        => Task.FromResult(Buffer.Length);

    public virtual Task<FileDetails> Details(CancellationToken cancel)
    {
        if (CachePath is not null)
        {
            return Task.FromResult(
                new FileDetails
                {
                    Created = File.GetCreationTime(CachePath),
                    Modified = File.GetLastWriteTime(CachePath),
                    Filename = Path.GetFileName(CachePath),
                    FileSize = NabuLib.FileSize(CachePath),
                }
            );
        }

        return Task.FromResult(
            new FileDetails
            {
                Created = Created,
                Modified = Modified,
                Filename = Filename ?? string.Empty,
                FileSize = Buffer.Length
            }
        );
    }

    public Task<Memory<byte>> Read(int offset, short readLength, CancellationToken cancel)
    {
        return Task.FromResult(Buffer[offset..(offset + readLength)]);
    }

    public Task Append(Memory<byte> data, CancellationToken cancel)
    {
        Buffer = NabuLib.Append(Buffer, data).ToArray();
        return Task.CompletedTask;
    }

    public Task Insert(int offset, Memory<byte> data, CancellationToken cancel)
    {
        Buffer = NabuLib.Insert(Buffer, offset, data).ToArray();
        return Task.CompletedTask;
    }

    public Task Delete(int offset, short length, CancellationToken cancel)
    {
        Buffer = NabuLib.Delete(Buffer, offset, length).ToArray();
        return Task.CompletedTask;
    }

    public Task Empty(CancellationToken cancel)
    {
        Buffer = Array.Empty<byte>();
        return Task.CompletedTask;
    }

    public Task Replace(int offset, Memory<byte> data, CancellationToken cancel)
    {
        Buffer = NabuLib.Replace(Buffer, offset, data).ToArray();
        return Task.CompletedTask;
    }

    public int Position { get; protected set; } = 0;

    public Task<Memory<byte>> ReadSequence(short readLength, CancellationToken cancel)
    {
        var end = Position + readLength;
        if (end > Buffer.Length)
        {
            end = Buffer.Length;
            readLength = (short)(Buffer.Length - Position);
        }
        if (Position >= Buffer.Length)
        {
            return Task.FromResult<Memory<byte>>(Array.Empty<byte>());
        }
        Memory<byte> bytes = Buffer[Position..end];
        Position += readLength;
        return Task.FromResult(bytes);
    }

    public Task<int> Seek(int offset, FileSeekFlags flags, CancellationToken cancel)
    {
        Position = flags switch
        {
            FileSeekFlags.FromCurrent => Position + offset,
            FileSeekFlags.FromBeginning => offset,
            FileSeekFlags.FromEnd => Buffer.Length - offset,
            _ => offset
        };
        return Task.FromResult(Position);
    }
}
