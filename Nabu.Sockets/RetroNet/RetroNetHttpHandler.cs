﻿using Nabu.Services;

namespace Nabu.Network.RetroNet;

public class RetroNetHttpHandler : NabuService, IRetroNetFileHandler
{
    public RetroNetHttpHandler(
        IConsole logger, 
        HttpClient client, 
        AdaptorSettings settings, 
        FileCache cache
    ) : base(logger, settings)
    {
        Client = new CachingHttpClient(client, Logger, cache, settings);
    }
    public CachingHttpClient Client { get; }

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
        filename = NabuLib.Uri(Settings, filename);
        var head = await Client.GetHead(filename);
        var size = (int?)head.Content.Headers.ContentLength ?? -1;
        Log($"HTTP: {filename}:{size}");
        return size;
    }
}

