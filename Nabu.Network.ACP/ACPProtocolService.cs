using Microsoft.Extensions.Logging;

namespace Nabu.Network;

public class ACPProtocolService : IACPProtocolService
{
    ILogger Logger;
    AdaptorSettings Settings;
    Dictionary<byte, IStorageHandler> StorageSlots { get; } = new();
    Task<T> Task<T>(T item) => System.Threading.Tasks.Task.FromResult(item);
    public ACPProtocolService(ILogger logger, AdaptorSettings settings)
    {
        Logger = logger;
        Settings = settings;
    }

    byte NextIndex()
    {
        for (int i = 0x00; i < 0xFF; i++)
        {
            if (StorageSlots.ContainsKey((byte)i)) continue;
            return (byte)i;
        }
        return 0xFF;
    }

    public Task<(bool, string, string)> DateTime()
    {
        var now = System.DateTime.Now;
        return Task((
            true,
            now.ToString("yyyyMMdd"),
            now.ToString("HHmmss")
        ));
    }

    public async Task<(bool, string, byte, int)> Open(byte index, short flags, string uri)
    {
        if (index is 0xFF)
        {
            index = NextIndex();
            if (index is 0xFF)
                return (false, "All slots full", 0xFF, 0);
        }
        try
        {
            IStorageHandler? handler = uri.ToLower() switch
            {
                var path when path.StartsWith("file")
                    => new FileStorageHandler(Logger, Settings),
                var path when path.StartsWith("http") || path.StartsWith("https")
                    => new HttpStorageHandler(Logger, Settings),
                var path when path.StartsWith("ram")
                    => new RAMStorageHandler(Logger, Settings),
                _ => null
            };
            if (handler is null)
                return (false, "Unknown URI Type", 0xFF, 0);

            var (success, error, length) = await handler.Open(flags, uri);
            StorageSlots[index] = handler;
            return (success, error, index, length);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 0xFF, 0);
        }
    }

    public Task<(bool, string, byte[])> Get(byte index, int offset, short length)
    {
        try
        {
            var handler = StorageSlots[index];
            return handler.Get(offset, length);
        }
        catch (Exception ex)
        {
            return Task((false, ex.Message, Array.Empty<byte>()));
        }
    }

    public Task<(bool, string)> Put(byte index, int offset, byte[] buffer)
    {
        try
        {
            var handler = StorageSlots[index];
            return handler.Put(offset, buffer);
        }
        catch (Exception ex)
        {
            return Task((false, ex.Message));
        }
    }

    public Task<(bool, string, byte, byte[])> Command(byte index, byte command, byte[] data)
    {
        throw new NotImplementedException();
    }

    public void End()
    {
        foreach (var key in StorageSlots.Keys)
        {
            StorageSlots[key].End();
        }
        StorageSlots.Clear();
    }
}
