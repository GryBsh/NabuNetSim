namespace Nabu.Network.NHACP.V01;

/*
public class NHACPV01ProtocolService
{
    IConsole Logger;

    Dictionary<byte, INHACPStorageHandler> StorageSlots { get; } = new();
    Task<T> Task<T>(T item) => System.Threading.Tasks.Task.FromResult(item);
    public NHACPV01ProtocolService(IConsole logger)
    {
        Logger = logger;
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

    public async Task<(bool, string, byte, int, NHACPError)> Open(AdaptorSettings settings, byte index, FileFlags flags, string uri)
    {
        if (index is 0xFF)
        {
            index = NextIndex();
            if (index is 0xFF)
                return (false, "All slots full", 0xFF, 0, NHACPError.TooManyOpen);
        }
        try
        {
            INHACPStorageHandler? handler = uri.ToLower() switch
            {
                var path when path.StartsWith("http") || path.StartsWith("https")
                        => new HttpStorageHandler(Logger, settings),
                var path when path.StartsWith("ram")  => new RAMStorageHandler(Logger, settings),
                _       => new FileStorageHandler(Logger, settings)
            };
            if (handler is null)
                return (false, "Unknown URI Type", 0xFF, 0, NHACPError.NotPermitted);

            var (success, error, length, code) = await handler.Open(flags, uri);
            if (success)
                StorageSlots[index] = handler;

            return (success, error, index, length, code);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 0xFF, 0, 0);
        }
    }

    public Task<(bool, string, byte[], NHACPError)> Get(byte index, int offset, short length)
    {
        try
        {
            var handler = StorageSlots[index];
            return handler.Get(offset, length);
        }
        catch (Exception ex)
        {
            return Task((false, ex.Message, Array.Empty<byte>(), NHACPError.Undefined));
        }
    }

    public Task<(bool, string, NHACPError)> Put(byte index, int offset, Memory<byte> buffer)
    {
        try
        {
            var handler = StorageSlots[index];
            return handler.Put(offset, buffer);
        }
        catch (Exception ex)
        {
            return Task((false, ex.Message, NHACPError.Undefined));
        }
    }

    public void Close(byte index)
    {
        StorageSlots[index].End();
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
*/