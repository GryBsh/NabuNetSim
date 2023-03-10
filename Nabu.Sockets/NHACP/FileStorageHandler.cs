using System.Security;
using Nabu.Services;

namespace Nabu.Network.NHACP;

public class FileStorageHandler : IStorageHandler
{
    AdaptorSettings Settings;
    StorageFlags Flags = StorageFlags.ReadWrite;
    FileInfo? File;

    public FileStorageHandler(IConsole logger, AdaptorSettings settings)
    {
        Settings = settings;
    }


    public Task<(bool, string, int)> Open(short flags, string uri)
    {
        Flags = (StorageFlags)flags;
        var path = Path.Combine(Settings.StoragePath, uri);
        try
        {
            File = new FileInfo(path);
            var length = File.Exists ? (int)File.Length : 0;
            return Task.FromResult((true, string.Empty, length));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult((false, ex.Message, 7));
        }
        catch (SecurityException ex)
        {
            return Task.FromResult((false, ex.Message, 7));
        }
        catch (FileNotFoundException ex) 
        {
            return Task.FromResult((false, ex.Message, 3));
        }
        catch (IOException ex) 
        {
            return Task.FromResult((false, ex.Message, 8));
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
            using var stream = new FileStream(
                File!.FullName,
                FileMode.OpenOrCreate,
                Flags is StorageFlags.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
                FileShare.Read
            );
            var end = offset + length;
            if (end > stream.Length)
                length = (short)(stream.Length - offset);
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
