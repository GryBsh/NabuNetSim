using Nabu.Services;
using System.Security;

namespace Nabu.Network.NHACP.V0;

public class FileStorageHandler : IStorageHandler
{
    private FileInfo? File;
    private StorageFlags Flags = StorageFlags.ReadWrite;
    private AdaptorSettings Settings;

    public FileStorageHandler(ILog logger, AdaptorSettings settings)
    {
        Settings = settings;
    }

    public Task<(bool, string, byte, byte[])> Command(byte index, byte command, byte[] data)
    {
        throw new NotImplementedException();
    }

    public void End()
    {
        File = null;
    }

    public async Task<(bool, string, Memory<byte>)> Get(int offset, ushort length)
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
                length = (ushort)(stream.Length - offset);
            await stream.ReadAsync(buffer, offset, length);
            return (true, string.Empty, buffer);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, Array.Empty<byte>());
        }
    }

    public Task<(bool, string, int)> Open(ushort flags, string uri)
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

    public async Task<(bool, string)> Put(int offset, Memory<byte> buffer)
    {
        try
        {
            if (Flags.HasFlag(StorageFlags.ReadOnly))
                return (false, "File Opened Read Only");
            using var stream = File!.OpenWrite();
            await stream.WriteAsync(buffer.ToArray(), offset, buffer.Length);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}