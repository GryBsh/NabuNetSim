using Microsoft.Extensions.Logging;

namespace Nabu.Network.NHACP;

public class FileStorageHandler : IStorageHandler
{
    AdaptorSettings Settings;
    StorageFlags Flags = StorageFlags.ReadWrite;
    FileInfo? File;

    public FileStorageHandler(ILogger logger, AdaptorSettings settings)
    {
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
        Flags = (StorageFlags)flags;
        var path = CombinePath(uri);
        try
        {
            File = new FileInfo(path);
            return Task.FromResult((true, string.Empty, (int)File.Length));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message, 0));
        }
    }

    public async Task<(bool, string, byte[])> Get(int offset, short length)
    {
        try
        {
            var buffer = new byte[length];
            using var stream = File!.OpenRead();
            await stream.ReadAsync(buffer, offset, length);
            return (true, string.Empty, buffer);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, Array.Empty<byte>());
        }
    }

    public async Task<(bool, string)> Put(int offset, byte[] buffer)
    {
        try
        {
            if (Flags.HasFlag(StorageFlags.ReadOnly))
                return (false, "File Opened Read Only");
            using var stream = File!.OpenWrite();
            await stream.WriteAsync(buffer, offset, buffer.Length);
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

    public Task<(bool, string, byte, byte[])> Command(byte index, byte command, byte[] data)
    {
        throw new NotImplementedException();
    }
}
