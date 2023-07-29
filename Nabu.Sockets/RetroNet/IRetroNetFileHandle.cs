namespace Nabu.Network.RetroNet;

public interface IRetroNetFileHandle
{
    public int Position { get; }

    Task Append(Memory<byte> data, CancellationToken cancel);

    Task Close(CancellationToken cancel);

    Task Delete(int offset, ushort length, CancellationToken cancel);

    Task<FileDetails> Details(CancellationToken cancel);

    Task Empty(CancellationToken cancel);

    Task Insert(int offset, Memory<byte> data, CancellationToken cancel);

    Task<bool> Open(string filename, FileOpenFlags flags, CancellationToken cancel);

    Task<Memory<byte>> Read(int offset, ushort readLength, CancellationToken cancel);

    Task<Memory<byte>> ReadSequence(ushort readLength, CancellationToken cancel);

    Task Replace(int offset, Memory<byte> data, CancellationToken cancel);

    Task<int> Seek(int offset, FileSeekFlags flags, CancellationToken cancel);

    Task<int> Size(CancellationToken cancel);
}