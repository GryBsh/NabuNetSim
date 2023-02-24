using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using System;
using Nabu.Services;

namespace Nabu.Network.RetroNet;

public class RetroNetHttpHandler : NabuService, IRetroNetFileHandler
{
    public RetroNetHttpHandler(IConsole logger, HttpClient client, AdaptorSettings settings) : base(logger, settings)
    {
        Client = new HttpCache(client, Logger);
    }
    public HttpCache Client { get; }

    public async Task<byte[]> Get(string filename, CancellationToken cancel)
    {
        try
        {
            var (_, found, cached)= await Client.CanGet(filename);
            if (found || cached)
            {
                return await Client.GetBytes(filename);
            }

            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            Error(ex.Message);
            return Array.Empty<byte>();
        }
    }

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
        var head = await Client.GetHead(filename);
        var size = (int?)head.Content.Headers.ContentLength ?? -1;
        Log($"HTTP: {filename}:{size}");
        return size;
    }
}

