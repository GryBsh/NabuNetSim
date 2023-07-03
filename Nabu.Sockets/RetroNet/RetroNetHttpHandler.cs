using Nabu.Services;

namespace Nabu.Network.RetroNet;

public class RetroNetHttpHandler : NabuService, IRetroNetFileHandler
{
    public RetroNetHttpHandler(
        ILog logger,
        HttpClient client,
        AdaptorSettings adaptor,
        Settings settings,
        IFileCache cache
    ) : base(logger, adaptor)
    {
        Client = new HttpCache(client, Logger, cache, settings, adaptor);
    }

    public HttpCache Client { get; }

    public Task Copy(string source, string destination, CopyMoveFlags flags)
    {
        throw new NotImplementedException();
    }

    public Task Delete(string filename)
    {
        throw new NotImplementedException();
    }

    public Task<FileDetails> FileDetails(string filename)
    {
        throw new NotImplementedException();
    }

    public async Task<Memory<byte>> Get(string filename, CancellationToken cancel)
    {
        //filename = NabuLib.Uri(Settings, filename);
        try
        {
            return await Client.GetBytes(filename);
        }
        catch (Exception ex)
        {
            Error(ex.Message);
            return Array.Empty<byte>();
        }
    }

    public Task<FileDetails> Item(short index)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<FileDetails>> List(string path, string wildcard, FileListFlags flags)
    {
        throw new NotImplementedException();
    }

    public Task Move(string source, string destination, CopyMoveFlags flags)
    {
        throw new NotImplementedException();
    }

    public async Task<int> Size(string filename)
    {
        filename = NabuLib.Uri(Adaptor, filename);
        var head = await Client.GetHead(filename);
        var size = (int?)head.Content.Headers.ContentLength ?? -1;
        Log($"HTTP: {filename}:{size}");
        return size;
    }
}