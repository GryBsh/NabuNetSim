namespace Nabu.Network.RetroNet;

public interface IRetroNetFileHandler
{
    Task<Memory<byte>> Get(string filename, CancellationToken cancel);

    Task<int> Size(string filename);

    Task<FileDetails> FileDetails(string filename);

    Task Delete(string filename);

    Task Copy(string source, string destination, CopyMoveFlags flags);

    Task Move(string source, string destination, CopyMoveFlags flags);

    Task<IEnumerable<FileDetails>> List(string path, string wildcard, FileListFlags flags);

    //Task<FileDetails> Item(short index);
}