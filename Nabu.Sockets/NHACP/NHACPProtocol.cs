using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using System;
using System.Reflection;
using System.Text;
using Nabu.Services;

namespace Nabu.Network.NHACP;

public class NHACPV01ProtocolService : INHACPProtocolService
{
    IConsole Logger;
    AdaptorSettings Settings;
    Dictionary<byte, IStorageHandler> StorageSlots { get; } = new();
    Task<T> Task<T>(T item) => System.Threading.Tasks.Task.FromResult(item);
    public NHACPV01ProtocolService(IConsole logger, AdaptorSettings settings)
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
                return (false, "All slots full", 0xFF, 12);
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
                return (false, "Unknown URI Type", 0xFF, 1);

            var (success, error, length) = await handler.Open(flags, uri);
            if (success) 
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


public class NHACPV01Protocol : Protocol
{
    public NHACPV01Protocol(IConsole logger, NHACPProtocolService storage) : base(logger)
    {
        Storage = storage;
    }

    public override byte Version => 0x03;
    Dictionary<byte, byte[]> Buffers { get; } = new();
    NHACPProtocolService Storage { get; }

    Dictionary<byte, Func<byte[], Task>> PrepareHandlers()
        => new() {
            { 0x01, Open },
            { 0x02, Get },
            { 0x03, Put },
            { 0x04, DateTime },
            { 0x05, Close },
            //{ 0x06, StorageErrorDetails }
        };
    public override byte[] Commands => new byte[] { 0x8F };

    void StorageStarted()
    {
        SendFramed(
            0x80,
            NabuLib.FromShort(Version),
            NabuLib.ToSizedASCII(Emulator.Id)
        );
    }

    void StorageError(short code, string message)
    {
        LastError = (code, message);
        SendFramed(
            0x82,
            NabuLib.FromShort(code),
            NabuLib.ToSizedASCII(string.Empty)
        );
    }
    
    (short, string) LastError;
    Dictionary<short, string> ErrorDescriptions = new()
    {
        [0] = "Undefined Error", //Undefined
        [1] = "Unsupported", //ENOTSUP
        [2] = "Not Permitted", //EPERM
        [3] = "File Not Found", //ENOENT
        [4] = "IO Error", //EIO
        [5] = "Bad Descriptor", //EBADF
        [6] = "Out of Memory", //ENOMEM
        [7] = "Access Denied", //EACCES
        [8] = "Busy", //EBUSY
        [9] = "Exists", //EEXIST
        [10] = "Is Directory", //EISDIR
        [11] = "Invalid Request", //EINVAL
        [12] = "Too Many Open", //ENFILE
        [13] = "Too Big", //EFBIG
        [14] = "No Space", //ENOSPC
        [15] = "No Seek", //ESEEK
        [16] = "Is File" //ENOTDIR
    };
    void StorageErrorDetails(short code, byte maxLength)
    {
        (short lastCode, string lastMessage) = LastError;
        if (code != lastCode) {
            if (ErrorDescriptions.TryGetValue(code, out var message))
                lastMessage = message;
            else
                lastMessage = ErrorDescriptions[0];
        }

        SendFramed(
            0x82,
            NabuLib.FromShort(code),
            NabuLib.ToSizedASCII(lastMessage)
        );
    }

    void StorageLoaded(short index, int length)
    {
        SendFramed(
            0x83,
            NabuLib.FromShort(index),
            NabuLib.FromInt(length)
        );
    }
    void DataBuffer(byte[] buffer)
    {
        SendFramed(
            0x84,
            NabuLib.FromShort((short)buffer.Length),
            buffer
        );
    }

    void OnPacketSliced(int next, long length)
    {
        if (next > 0) Warning($"Packet length {length - next} != Frame length {length}");
    }

    async Task Open(byte[] buffer)
    {
        Log($"NPC: Open");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, short flags) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, byte uriLength) = NabuLib.Pop(buffer, i);
            (i, string uri) = NabuLib.Slice(buffer, i, uriLength, NabuLib.ToASCII);
            OnPacketSliced(i, buffer.Length);

            var (success, error, slot, length) = await Storage.Open(index, flags, uri);
            if (success)
            {
                Log($"NA: Loading into slot {index}: {uri}");
                StorageLoaded(slot, length);
            }
            else
                Log($"NA: Loading into slot {index}: {uri} failed: {error}");
                StorageError((short)length, error);
        }
        catch (Exception ex)
        {
            StorageError(500, ex.Message);
        }
    }

    async Task Close(byte[] buffer) {

        Storage.End();
    }

    async Task Get(byte[] buffer)
    {
        Log($"NPC: Get");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset)     = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, short length)   = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            OnPacketSliced(i, buffer.Length);

            var (success, error, data) = await Storage.Get(index, offset, length);
            if (success is false) StorageError(0, error);
            else
            {
                Log($"NA: slot {index}:{offset}->{length} bytes");
                DataBuffer(data);
            }
        }
        catch (Exception ex)
        {
            StorageError(500, ex.Message);
        }

    }

    async Task Put(byte[] buffer)
    {
        Log($"NPC: Put");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, short length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var data) = NabuLib.Slice(buffer, i, length);
            OnPacketSliced(i, buffer.Length);

            var (success, error) = await Storage.Put(index, offset, data);
            if (success)
            {
                Log($"Put into slot {index}: OK");
                SendFramed(0x81); // OK
            }
            else
                StorageError(500, error);
        }
        catch (Exception ex)
        {
            StorageError(500, ex.Message);
        }

    }


    async Task DateTime(byte[]? none)
    {
        Log($"NPC: DateTime");
        var (_, date, time) = await Storage.DateTime();
        SendFramed(
            0x85,
            NabuLib.ToSizedASCII(date),
            NabuLib.ToSizedASCII(time)
        );
        Log($"NA: DataTime Send");
    }
    protected override async Task Handle(byte command, CancellationToken cancel)
    {
        var header = Recv(7);
        var (p, acp) = NabuLib.Slice(header, 0, 3, NabuLib.ToASCII);
        (p, var version) = NabuLib.Slice(header, p, 2);
        (p, var options) = NabuLib.Slice(header, p, 2, NabuLib.ToShort);

        //TODO: Handle future options Here

        Log($"Start v{version}");
        StorageStarted();
        var handlers = PrepareHandlers();
        while (cancel.IsCancellationRequested is false)
            try
            {
                var (length, buffer) = ReadFrame();
                if (length is 0)
                    return;

                (int next, command) = NabuLib.Pop(buffer, 0);
                var (_, message) = NabuLib.Slice(buffer, next, length);

                switch (command)
                {

                    case 0x00:
                        Warning($"v{Version} Received: 0, Aborting");
                        break;
                    case 0xEF:
                        break;
                    case var c when handlers.ContainsKey(c):
                        await handlers[command](message);
                        break;
                    default:
                        Warning($"Unsupported: {Format(command)}");
                        continue;
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }

        Log($"End v{Version}");
        return;
    }
}

public class NHACPProtocol : Protocol
{
    INabuNetwork NabuNet { get; }
    NHACPProtocolService Storage { get; }
    public NHACPProtocol(IConsole<NHACPProtocol> logger, INabuNetwork nabuNet) : base(logger)
    {
        Storage = new(Logger, Settings);
        NabuNet = nabuNet;
    }

    public override byte[] Commands { get; } = new byte[] { 0xAF };
    public override byte Version => 0x01;

    #region ACP Frames / Messages
    void StorageStarted()
    {
        SendFramed(
            0x80,
            NabuLib.FromShort(Version),
            NabuLib.ToSizedASCII(Emulator.Id)
        );
    }

    void StorageError(short code, string message)
    {
        SendFramed(
            0x82,
            NabuLib.FromShort(code),
            NabuLib.ToSizedASCII(message)
        );
    }
    void StorageLoaded(short index, int length)
    {
        SendFramed(
            0x83,
            NabuLib.FromShort(index),
            NabuLib.FromInt(length)
        );
    }
    void DataBuffer(byte[] buffer)
    {
        SendFramed(
            0x84,
            NabuLib.FromShort((short)buffer.Length),
            buffer
        );
    }

    #endregion

    #region Helpers

    void OnPacketSliced(int next, long length)
    {
        if (next > 0) Warning($"Packet length {length - next} != Frame length {length}");
    }

    #endregion

    #region ACP Operations
    async Task Open(byte[] buffer)
    {
        Log($"NPC: Open");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, short flags) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, byte uriLength) = NabuLib.Pop(buffer, i);
            (i, string uri) = NabuLib.Slice(buffer, i, uriLength, NabuLib.ToASCII);
            OnPacketSliced(i, buffer.Length);

            var (success, error, slot, length) = await Storage.Open(index, flags, uri);
            if (success)
            {
                Log($"NA: Loading into slot {index}: {uri}");
                StorageLoaded(slot, length);
            }
            else
                Log($"NA: Loading into slot {index}: {uri} failed: {error}");
                StorageError((short)length, error);
        }
        catch (Exception ex)
        {
            StorageError(500, ex.Message);
        }
    }

    async Task Get(byte[] buffer)
    {
        Log($"NPC: Get");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset)     = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, short length)   = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            OnPacketSliced(i, buffer.Length);

            var (success, error, data) = await Storage.Get(index, offset, length);
            if (success is false) StorageError(0, error);
            else
            {
                Log($"NA: slot {index}:{offset}->{length} bytes");
                DataBuffer(data);
            }
        }
        catch (Exception ex)
        {
            StorageError(500, ex.Message);
        }

    }

    async Task Put(byte[] buffer)
    {
        Log($"NPC: Put");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, short length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var data) = NabuLib.Slice(buffer, i, length);
            OnPacketSliced(i, buffer.Length);

            var (success, error) = await Storage.Put(index, offset, data);
            if (success)
            {
                Log($"Put into slot {index}: OK");
                SendFramed(0x81); // OK
            }
            else
                StorageError(500, error);
        }
        catch (Exception ex)
        {
            StorageError(500, ex.Message);
        }

    }
    async Task DateTime(byte[]? none)
    {
        Log($"NPC: DateTime");
        var (_, date, time) = await Storage.DateTime();
        SendFramed(
            0x85,
            NabuLib.ToSizedASCII(date),
            NabuLib.ToSizedASCII(time)
        );
        Log($"NA: DataTime Send");
    }

    #endregion

    protected virtual Dictionary<byte, Func<byte[], Task>> PrepareHandlers()
    {
        return new()
        {
            { 0x01, Open },
            { 0x02, Get },
            { 0x03, Put },
            { 0x04, DateTime }
        };
    }

    protected override async Task Handle(byte command, CancellationToken cancel)
    {
        Log($"Start v{Version}");
        StorageStarted();
        var handlers = PrepareHandlers();
        while (cancel.IsCancellationRequested is false)
            try
            {
                var (length, buffer) = ReadFrame();
                if (length is 0)
                    return;

                (int next, command) = NabuLib.Pop(buffer, 0);
                var (_, message) = NabuLib.Slice(buffer, next, length);

                switch (command)
                {

                    case 0x00:
                        Warning($"v{Version} Received: 0, Aborting");
                        break;
                    case 0xEF:
                        break;
                    case var c when handlers.ContainsKey(c):
                        await handlers[command](message);
                        break;
                    default:
                        Warning($"Unsupported: {Format(command)}");
                        continue;
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }

        Log($"End v{Version}");
        return;
    }

    public override void Detach()
    {
        Storage.End();
        base.Detach();
    }

    public override bool ShouldAccept(byte unhandled)
    {
        return base.ShouldAccept(unhandled) && 
               NabuNet.Source(Settings).EnableRetroNet is false;
    }

}

