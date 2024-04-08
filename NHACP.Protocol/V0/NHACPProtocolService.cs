using Gry.Adapters;
using Microsoft.Extensions.Logging;

namespace NHACP.V0
{
    public class NHACPProtocolService : INHACPProtocolService
    {
        private ILogger Logger;
        private AdapterDefinition Settings;

        public NHACPProtocolService(ILogger logger, AdapterDefinition settings)
        {
            Logger = logger;
            Settings = settings;
        }

        private Dictionary<byte, IStorageHandler> StorageSlots { get; } = [];

        public Task<(bool, string, byte, byte[])> Command(byte index, byte command, byte[] data)
        {
            throw new NotImplementedException();
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

        public void End()
        {
            foreach (var key in StorageSlots.Keys)
            {
                StorageSlots[key].End();
            }
            StorageSlots.Clear();
        }

        public Task<(bool, string, Memory<byte>)> Get(byte index, int offset, ushort length)
        {
            try
            {
                var handler = StorageSlots[index];
                return handler.Get(offset, length);
            }
            catch (Exception ex)
            {
                return Task((false, ex.Message, new Memory<byte>([])));
            }
        }

        public async Task<(bool, string, byte, int)> Open(byte index, ushort flags, string uri)
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
                    var path when path.StartsWith("http") || path.StartsWith("https")
                        => new HttpStorageHandler(Logger, Settings),
                    var path when path.StartsWith("ram")
                        => new RAMStorageHandler(Logger, Settings),
                    _ => new FileStorageHandler(Logger, Settings)
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

        public Task<(bool, string)> Put(byte index, int offset, Memory<byte> buffer)
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

        private byte NextIndex()
        {
            for (int i = 0x00; i < 0xFF; i++)
            {
                if (StorageSlots.ContainsKey((byte)i)) continue;
                return (byte)i;
            }
            return 0xFF;
        }

        private Task<T> Task<T>(T item) => System.Threading.Tasks.Task.FromResult(item);
    }
}