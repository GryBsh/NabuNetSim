using Microsoft.Extensions.Logging;
using Nabu.Adaptor;

namespace Nabu.Network;

public class FileStorage : IStorageHandler
{
    public string Protocol => "file";
    ILogger Logger;
    AdaptorSettings Settings;
    StorageFlags Flags = StorageFlags.ReadWrite;
    FileInfo? File;

    public FileStorage(ILogger logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    string CombinePath(string uri)
    {
        var path = uri.Split("://").Last();
        if (string.IsNullOrEmpty(path)) return string.Empty;

        return Path.Combine(Settings.StoragePath, path);
    }

    public Task<(bool, string, int)> Open(short flags, string uri)
    {
        StorageFlags storageFlags = (StorageFlags)flags;
        var path = CombinePath(uri);
        var fileExists = File?.Exists ?? false;
        if (fileExists)
        {
            try
            {
                File = new FileInfo(path);
                return Task.FromResult((true, string.Empty, (int)File.Length));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, ex.Message, 0));
            }
        } else
        {
            return Task.FromResult((false, "File Not Found", 0));
        }
    }

    public (bool, string, byte[]) Get(int offset, short length)
    {
        try
        {
            var buffer = new byte[length];
            var file = File?.OpenRead();
            file?.Seek(offset, SeekOrigin.Begin);
            file?.Read(buffer, 0, length);
            return (true, string.Empty, buffer);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, Array.Empty<byte>());
        }
    }

    public (bool, string) Put(int offset, byte[] buffer)
    {
        try
        {
            if (Flags == StorageFlags.ReadOnly)
                return (false, "File Opened Read Only");

            var file = File?.OpenWrite();
            file?.Seek(offset, SeekOrigin.Begin);
            file?.Write(buffer, 0, buffer.Length);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public void End()
    {
        File = null;
    }

    public (bool, string, byte, byte[]) Command(byte index, byte command, byte[] data)
    {
        throw new NotImplementedException();
    }
}

public class HttpStorage : IStorageHandler
{
    ILogger Logger;
    AdaptorSettings Settings;

    public string Protocol => "http";

    public HttpStorage(ILogger logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    public Task<(bool, string, int)> Open(short flags, string uri)
    {
        throw new NotImplementedException();
    }

    public (bool, string, byte[]) Get(int offset, short length)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Put(int offset, byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public void End()
    {
        throw new NotImplementedException();
    }

    public (bool, string, byte, byte[]) Command(byte index, byte command, byte[] data)
    {
        throw new NotImplementedException();
    }
}
