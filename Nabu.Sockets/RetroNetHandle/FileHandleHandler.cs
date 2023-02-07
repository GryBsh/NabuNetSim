using Microsoft.Extensions.Logging;
using Nabu.Network.RetroNet;

namespace Nabu.Network.RetroNetHandle;

public class FileHandleHandler : NabuService, IRetroNetFileHandle
{
    public FileHandleHandler(IConsole logger, AdaptorSettings settings) : base(logger, settings)
    {

    }

    public FileInfo? FileHandle { get; set; }
    FileOpenFlags? Flags { get; set; }

    public Task Append(byte[] data, CancellationToken cancel)
    {
        Stream().Write(NabuLib.Append(Content(), data));
        return Task.CompletedTask;
    }

    public Task Close(CancellationToken cancel)
    {
        FileHandle = null;
        return Task.CompletedTask;
    }

    public Task Delete(int offset, short length, CancellationToken cancel)
    {
        Stream().Write(NabuLib.Delete(Content(), offset, length));
        return Task.CompletedTask;
    }

    public Task<FileDetails> Details(CancellationToken cancel)
    {
        return Task.FromResult(new FileDetails
        {
            Created = FileHandle!.CreationTime,
            Modified = FileHandle.LastWriteTime,
            Filename = FileHandle.Name,
            FileSize = (int)FileHandle.Length
        });
    }

    public Task Empty(CancellationToken cancel)
    {
        using var _ = Stream(FileMode.Truncate);
        return Task.CompletedTask;
    }

    public Task Insert(int offset, byte[] data, CancellationToken cancel)
    {
        Stream().Write(NabuLib.Insert(Content(), offset, data));
        return Task.CompletedTask;
    }

    Span<byte> Content()
    {
        using var stream = Stream();
        using var reader = new BinaryReader(stream);
        return reader.ReadBytes((int)FileHandle!.Length);
    }

    FileStream Stream(FileMode mode = FileMode.OpenOrCreate)
    {
        var stream = new FileStream(
            FileHandle!.FullName,
            mode,
            Flags is FileOpenFlags.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
            FileShare.ReadWrite
        );
        return stream;
    }

    public Task<bool> Open(string filename, FileOpenFlags flags, CancellationToken cancel)
    {
        Flags = flags;
        if (!Path.IsPathRooted(filename))
            filename = Path.Combine(settings.StoragePath, filename);
        FileHandle = new FileInfo(filename);
        return Task.FromResult(true);
    }

    public async Task<byte[]> Read(int offset, short readLength, CancellationToken cancel)
    {
        var bytes = new byte[readLength];
        var reader = Stream();
        reader.Seek(offset, SeekOrigin.Begin);
        await reader.ReadAsync(bytes, 0, readLength);
        return bytes;
    }



    public Task Replace(int offset, byte[] data, CancellationToken cancel)
    {
        Stream().Write(NabuLib.Replace(Content(), offset, data));
        return Task.CompletedTask;
    }



    public Task<int> Size(CancellationToken cancel)
    {
        return Task.FromResult((int)FileHandle!.Length);
    }

    int Position { get; set; }
    public async Task<byte[]> ReadSequence(short readLength, CancellationToken cancel)
    {
        var end = Position + readLength;
        if (end > FileHandle!.Length)
        {
            end = (int)FileHandle!.Length;
            readLength = (short)(FileHandle!.Length - Position);
        }
        if (Position >= FileHandle!.Length)
        {
            return new byte[0];
        }
        Log($"ReadSeq: S:{Position}, L:{readLength}, E:{end}");
        var bytes = await Read(Position, readLength, cancel);
        Position += readLength;
        return bytes;
    }

    public Task<int> Seek(int offset, FileSeekFlags flags, CancellationToken cancel)
    {
        Position = flags switch
        {
            FileSeekFlags.FromCurrent => Position + offset,
            FileSeekFlags.FromBeginning => offset,
            FileSeekFlags.FromEnd => (int)FileHandle!.Length - offset,
            _ => offset
        };
        return Task.FromResult(Position);
    }
}
