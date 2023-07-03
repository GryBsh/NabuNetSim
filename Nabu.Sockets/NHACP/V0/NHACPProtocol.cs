﻿using Nabu.Services;

namespace Nabu.Network.NHACP.V0;

public class NHACPProtocol : Protocol
{
    private INabuNetwork NabuNet { get; }
    private NHACPProtocolService Storage { get; }

    public NHACPProtocol(ILog<NHACPProtocol> logger, INabuNetwork nabuNet) : base(logger)
    {
        Storage = new(Logger, Adaptor);
        NabuNet = nabuNet;
    }

    public override byte[] Commands { get; } = new byte[] { 0xAF };
    public override byte Version => 0x01;

    #region ACP Frames / Messages

    private void StorageStarted()
    {
        SendFramed(
            0x80,
            NabuLib.FromShort(Version),
            NabuLib.ToSizedASCII(Emulator.Id).ToArray()
        );
    }

    private void StorageError(short code, string message)
    {
        SendFramed(
            0x82,
            NabuLib.FromShort(code),
            NabuLib.ToSizedASCII(message).ToArray()
        );
    }

    private void StorageLoaded(short index, int length)
    {
        SendFramed(
            0x83,
            NabuLib.FromShort(index),
            NabuLib.FromInt(length)
        );
    }

    private void DataBuffer(Memory<byte> buffer)
    {
        SendFramed(
            0x84,
            NabuLib.FromShort((short)buffer.Length),
            buffer.ToArray()
        );
    }

    #endregion ACP Frames / Messages

    #region Helpers

    private void OnPacketSliced(int next, long length)
    {
        if (next > 0) Warning($"Packet length {length - next} != Frame length {length}");
    }

    #endregion Helpers

    #region ACP Operations

    private async Task Open(Memory<byte> buffer)
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

    private async Task Get(Memory<byte> buffer)
    {
        Log($"NPC: Get");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset) = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, short length) = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
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

    private async Task Put(Memory<byte> buffer)
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

    private async Task DateTime(Memory<byte> none)
    {
        Log($"NPC: DateTime");
        var (_, date, time) = await Storage.DateTime();
        SendFramed(
            0x85,
            NabuLib.ToSizedASCII(date).ToArray(),
            NabuLib.ToSizedASCII(time).ToArray()
        );
        Log($"NA: DataTime Send");
    }

    #endregion ACP Operations

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
               NabuNet.Source(Adaptor)?.EnableRetroNet is false;
    }
}