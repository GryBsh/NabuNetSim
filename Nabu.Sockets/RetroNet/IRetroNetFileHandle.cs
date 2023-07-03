namespace Nabu.Network.RetroNet;

public interface IRetroNetFileHandle
{
    Task<bool> Open(string filename, FileOpenFlags flags, CancellationToken cancel);

    Task Close(CancellationToken cancel);

    Task<int> Size(CancellationToken cancel);

    Task<FileDetails> Details(CancellationToken cancel);

    Task<Memory<byte>> Read(int offset, short readLength, CancellationToken cancel);

    Task Append(Memory<byte> data, CancellationToken cancel);

    Task Insert(int offset, Memory<byte> data, CancellationToken cancel);

    Task Delete(int offset, short length, CancellationToken cancel);

    Task Empty(CancellationToken cancel);

    Task Replace(int offset, Memory<byte> data, CancellationToken cancel);

    public int Position { get; }

    Task<Memory<byte>> ReadSequence(short readLength, CancellationToken cancel);

    Task<int> Seek(int offset, FileSeekFlags flags, CancellationToken cancel);
}