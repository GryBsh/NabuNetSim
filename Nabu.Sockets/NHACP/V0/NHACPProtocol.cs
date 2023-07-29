using Nabu.Services;

namespace Nabu.Network.NHACP.V0;

public class NHACPProtocol : Protocol
{
    public NHACPProtocol(ILog<NHACPProtocol> logger, INabuNetwork nabuNet) : base(logger)
    {
        Storage = new(Logger, Adaptor);
        NabuNet = nabuNet;
    }

    public override byte[] Commands { get; } = new byte[] { 0xAF };
    public override byte Version => 0x00;
    private INabuNetwork NabuNet { get; }
    private NHACPProtocolService Storage { get; }

    #region ACP Frames / Messages

    private void DataBuffer(Memory<byte> buffer)
    {
        WriteFrame(
            0x84,
            NabuLib.FromUShort((ushort)buffer.Length),
            buffer.ToArray()
        );
    }

    private void StorageError(ushort code, string message)
    {
        WriteFrame(
            0x82,
            NabuLib.FromUShort(code),
            NabuLib.ToSizedASCII(message).ToArray()
        );
    }

    private void StorageLoaded(ushort index, int length)
    {
        WriteFrame(
            0x83,
            NabuLib.FromUShort(index),
            NabuLib.FromInt(length)
        );
    }

    private void StorageStarted()
    {
        WriteFrame(
            0x80,
            NabuLib.FromUShort(Version),
            NabuLib.ToSizedASCII(Emulator.Id).ToArray()
        );
    }

    #endregion ACP Frames / Messages

    #region Helpers

    private void OnPacketSliced(int next, long length)
    {
        if (next > 0) Warning($"Packet length {length - next} != Frame length {length}");
    }

    #endregion Helpers

    #region NHACP Operations

    private async Task DateTime(Memory<byte> none)
    {
        Log($"NPC: DateTime");
        var (_, date, time) = await Storage.DateTime();
        WriteFrame(
            0x85,
            NabuLib.ToSizedASCII(date).ToArray(),
            NabuLib.ToSizedASCII(time).ToArray()
        );
        Log($"NA: DataTime Send");
    }

    private async Task Get(Memory<byte> buffer)
    {
        Log($"NPC: Get");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, ushort length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToUShort);
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

    private async Task Open(Memory<byte> buffer)
    {
        Log($"NPC: Open");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, ushort flags) = NabuLib.Slice(buffer, i, 2, NabuLib.ToUShort);
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
            StorageError((ushort)length, error);
        }
        catch (Exception ex)
        {
            StorageError(500, ex.Message);
        }
    }

    private async Task Put(Memory<byte> buffer)
    {
        Log($"NPC: Put");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, ushort length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToUShort);
            (i, var data) = NabuLib.Slice(buffer, i, length);
            OnPacketSliced(i, buffer.Length);

            var (success, error) = await Storage.Put(index, offset, data);
            if (success)
            {
                Log($"Put into slot {index}: OK");
                WriteFrame(0x81); // OK
            }
            else
                StorageError(500, error);
        }
        catch (Exception ex)
        {
            StorageError(500, ex.Message);
        }
    }

    #endregion NHACP Operations

    public override void Detach()
    {
        Storage.End();
        base.Detach();
    }

    public override bool ShouldAccept(byte unhandled)
    {
        return base.ShouldAccept(unhandled) &&
               NabuNet.Source(Adaptor)?.EnableRetroNet is false;
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

    protected virtual Dictionary<byte, Func<Memory<byte>, Task>> PrepareHandlers()
    {
        return new()
        {
            { 0x01, Open },
            { 0x02, Get },
            { 0x03, Put },
            { 0x04, DateTime }
        };
    }
}