using Microsoft.Extensions.Logging;
using Nabu.Network.RetroNet;
using Nabu.Services;
using System.Collections.Immutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Nabu.Network.RetroNetHandle;

public class RetroNetMemoryHandle : NabuService, IRetroNetFileHandle
{
    public RetroNetMemoryHandle(IConsole logger, AdaptorSettings settings, byte[]? buffer = null) : base(logger, settings)
    {
        if (buffer is not null) Buffer = buffer;
    }

    protected byte[] Buffer { get; set; } = Array.Empty<byte>();
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
            Filename = filename.Split("0x//")[0];
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

    public Task<FileDetails> Details(CancellationToken cancel)
    {
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

    public Task<byte[]> Read(int offset, short readLength, CancellationToken cancel)
    {
        return Task.FromResult(Buffer[offset..(offset + readLength)]);
    }

    public Task Append(byte[] data, CancellationToken cancel)
    {
        Buffer = NabuLib.Append(Buffer, data).ToArray();
        return Task.CompletedTask;
    }

    public Task Insert(int offset, byte[] data, CancellationToken cancel)
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

    public Task Replace(int offset, byte[] data, CancellationToken cancel)
    {
        Buffer = NabuLib.Replace(Buffer, offset, data).ToArray();
        return Task.CompletedTask;
    }

    protected int Position { get; set; } = 0;

    public Task<byte[]> ReadSequence(short readLength, CancellationToken cancel)
    {
        var end = Position + readLength;
        if (end > Buffer.Length)
        {
            end = Buffer.Length;
            readLength = (short)(Buffer.Length - Position);
        }
        if (Position >= Buffer.Length)
        {
            return Task.FromResult(new byte[0]);
        }
        Log($"ReadSeq: S:{Position}, L:{readLength}, E:{end}");
        Span<byte> bytes = Buffer[Position..end];
        Position += readLength;
        return Task.FromResult(bytes.ToArray());
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
