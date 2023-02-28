using Microsoft.Extensions.Logging;
using Nabu.Network.RetroNet;
using Nabu.Services;

namespace Nabu.Network.RetroNetHandle;

public class RetroNetFileHandle : NabuService, IRetroNetFileHandle
{
    public RetroNetFileHandle(IConsole logger, AdaptorSettings settings) : base(logger, settings)
    {

    }

    //public FileInfo? FileHandle { get; set; }
    string Filename { get; set; }
    FileOpenFlags? Flags { get; set; }

    public Task Append(byte[] data, CancellationToken cancel)
    {
        using var stream = Stream();
        stream.Write(NabuLib.Append(Content(), data));
        
        return Task.CompletedTask;
    }

    public Task Close(CancellationToken cancel)
    {
        
        return Task.CompletedTask;
    }

    public Task Delete(int offset, short length, CancellationToken cancel)
    {
        using var stream = Stream();
        stream.Write(NabuLib.Delete(Content(), offset, length));
        return Task.CompletedTask;
    }

    public Task<FileDetails> Details(CancellationToken cancel)
    {
        return Task.FromResult(new FileDetails
        {
            Created = File.GetCreationTime(Filename),
            Modified = File.GetLastWriteTime(Filename),
            Filename = Path.GetFileName(Filename),
            FileSize = (int)new FileInfo(Filename).Length,
        });
    }

    public Task Empty(CancellationToken cancel)
    {
        using var _ = Stream(FileMode.Truncate);
        return Task.CompletedTask;
    }

    public Task Insert(int offset, byte[] data, CancellationToken cancel)
    {
        using var stream = Stream();
        stream.Write(NabuLib.Insert(Content(), offset, data));
        return Task.CompletedTask;
    }

    Span<byte> Content()
    {
        using var stream = Stream();
        using var reader = new BinaryReader(stream);
        
        return reader.ReadBytes((int)new FileInfo(Filename)!.Length);
    }

    FileStream Stream(FileMode mode = FileMode.OpenOrCreate)
    {
        var stream = new FileStream(
            Filename,
            mode,
            Flags is FileOpenFlags.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
            FileShare.ReadWrite
        );
        return stream;
    }

    public Task<bool> Open(string filename, FileOpenFlags flags, CancellationToken cancel)
    {
        Flags = flags;
        Filename = NabuLib.FilePath(Settings, filename);
        return Task.FromResult(true);
    }

    public async Task<byte[]> Read(int offset, short readLength, CancellationToken cancel)
    {
        var bytes = new byte[readLength];
        using var reader = Stream();
        reader.Seek(offset, SeekOrigin.Begin);
        await reader.ReadAsync(bytes, 0, readLength, cancel);
        return bytes;
    }



    public Task Replace(int offset, byte[] data, CancellationToken cancel)
    {
        using var stream = Stream();
        stream.Write(NabuLib.Replace(Content(), offset, data));
        return Task.CompletedTask;
    }



    public Task<int> Size(CancellationToken cancel)
    {
        return Task.FromResult((int)new FileInfo(Filename).Length);
    }

    public int Position { get; protected set; } = 0;
    public async Task<byte[]> ReadSequence(short readLength, CancellationToken cancel)
    {
        var end = Position + readLength;
        var length = new FileInfo(Filename).Length;
        if (end > length)
        {
            end = (int)length;
            readLength = (short)(length - Position);
        }
        if (Position >= length)
        {
            return new byte[0];
        }
        //Log($"ReadSeq: S:{Position}, L:{readLength}, E:{end}");
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
            FileSeekFlags.FromEnd => (int)new FileInfo(Filename).Length - offset,
            _ => offset
        };
        return Task.FromResult(Position);
    }
}
