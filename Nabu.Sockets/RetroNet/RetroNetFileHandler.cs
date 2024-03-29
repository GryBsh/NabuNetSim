﻿using Nabu.Services;

namespace Nabu.Network.RetroNet;

public class RetroNetFileHandler : NabuService, IRetroNetFileHandler
{
    public RetroNetFileHandler(ILog logger, AdaptorSettings settings) : base(logger, settings)
    {
    }

    public async Task<Memory<byte>> Get(string filename, CancellationToken cancel)
    {
        filename = NabuLib.FilePath(Adaptor, filename);
        if (File.Exists(filename))
            return await File.ReadAllBytesAsync(filename, cancel);
        return Array.Empty<byte>();
    }

    public Task Copy(string source, string destination, CopyMoveFlags flags)
    {
        File.Copy(NabuLib.FilePath(Adaptor, source), NabuLib.FilePath(Adaptor, destination), flags is CopyMoveFlags.Replace);
        return Task.CompletedTask;
    }

    public Task Delete(string filename)
    {
        File.Delete(NabuLib.FilePath(Adaptor, filename));
        return Task.CompletedTask;
    }

    public Task<FileDetails> FileDetails(string filename)
    {
        var file = new FileInfo(NabuLib.FilePath(Adaptor, filename));
        var details = file.Exists switch
        {
            true => new FileDetails
            {
                Created = file.CreationTime,
                Modified = file.LastWriteTime,
                Filename = file.Name,
                FileSize = (int)file.Length
            },
            false => new FileDetails
            {
                Filename = filename,
                FileSize = -2
            }
        };

        return Task.FromResult(details);
    }

    public Task<IEnumerable<FileDetails>> List(string path, string wildcard, FileListFlags flags)
    {
        var r = new List<FileDetails>();
        var dir = new DirectoryInfo(NabuLib.FilePath(Adaptor, path));
        if (flags is FileListFlags.Files)
            foreach (var file in dir.EnumerateFiles(wildcard))
                r.Add(new FileDetails
                {
                    Created = file.CreationTime,
                    Modified = file.LastWriteTime,
                    Filename = file.Name,
                    FileSize = (int)file.Length,
                });
        if (flags is FileListFlags.Directories)
            foreach (var folder in dir.EnumerateDirectories(wildcard))
                r.Add(new FileDetails
                {
                    Created = folder.CreationTime,
                    Modified = folder.LastWriteTime,
                    Filename = folder.Name,
                    FileSize = -1
                });
        return Task.FromResult((IEnumerable<FileDetails>)r);
    }

    public Task Move(string source, string destination, CopyMoveFlags flags)
    {
        File.Move(NabuLib.FilePath(Adaptor, source), NabuLib.FilePath(Adaptor, destination), flags is CopyMoveFlags.Replace);
        return Task.CompletedTask;
    }

    public Task<int> Size(string filename)
    {
        filename = NabuLib.FilePath(Adaptor, filename);
        var file = new FileInfo(filename);
        int length = -2;
        if (file!.Exists) length = (int)file.Length;
        return Task.FromResult(length);
    }
}