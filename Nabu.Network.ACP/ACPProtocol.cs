using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using System;
using System.Reflection;
using System.Text;

namespace Nabu.Network;

public class ACPProtocol : Protocol
{
    ACPProtocolService Storage { get; }
    public ACPProtocol(ILogger<ACPProtocol> logger) : base(logger)
    {
        Storage = new(Logger, Settings);
    }

    public override byte Command => 0xAF;
    protected override byte Version => 0x01;

    #region ACP Frames / Messages
    void StorageStarted()
    {
        SendFramed(
            0x80,
            NabuLib.FromShort(Version),
            NabuLib.FromSizedASCII(Emulator.Id)
        );
    }

    void StorageError(string message)
    {
        SendFramed(
            0x82,
            NabuLib.FromSizedASCII(message)
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
        if (next > 0) Warning($"Packet length {length-next} != Frame length {length}");
    }

    #endregion

    #region ACP Operations
    async Task Open(byte[] buffer)
    {
        Log($"NPC: Open");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, short flags)    = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, byte uriLength) = NabuLib.Pop(buffer, i);
            (i, string uri)     = NabuLib.Slice(buffer, i, uriLength, NabuLib.ToASCII);
            OnPacketSliced(i, buffer.Length);

            var (success, error, slot, length) = await Storage.Open(index, flags, uri);
            if (success)
            {
                Log($"NA: Loading into slot {index}: {uri}");
                StorageLoaded(slot, length);
            }
            else
                StorageError(error);
        }
        catch (Exception ex)
        {
            StorageError(ex.Message);
        }
    }

    void Get(byte[] buffer)
    {
        Log($"NPC: Get");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset)     = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt); 
            (i, short length)   = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            OnPacketSliced(i, buffer.Length);

            var (success, error, data) = Storage.Get(index, offset, length);
            if (success is false) StorageError(error);
            else
            {
                Log($"NA: Get from slot {index}:{offset}->{length} bytes");
                DataBuffer(data);
            }
        }
        catch (Exception ex)
        {
            StorageError(ex.Message);
        }

    }

    void Put(byte[] buffer)
    {
        Log($"NPC: Put");
        try
        {
            (int i, byte index) = NabuLib.Pop(buffer, 0);
            (i, int offset)     = NabuLib.Slice(buffer, i, 4, NabuLib.ToInt);
            (i, short length)   = NabuLib.Slice(buffer, i, 2, NabuLib.ToShort);
            (i, var data)       = NabuLib.Slice(buffer, i, length);
            OnPacketSliced(i, buffer.Length);

            var (success, error) = Storage.Put(index, offset, data);
            if (success)
            {
                Log($"Put into slot {index}: OK");
                SendFramed(0x81); // OK
            }
            else
                StorageError(error);
        }
        catch (Exception ex)
        {
            StorageError(ex.Message);
        }

    }
    void DateTime()
    {
        Log($"NPC: DateTime");
        var (_, date, time) = Storage.DateTime();
        SendFramed(
            0x85,
            NabuLib.FromSizedASCII(date),
            NabuLib.FromSizedASCII(time)
        );
        Log($"NA: DataTime Send");
    }

    #endregion

    public override void OnListen()
    {
        Log($"v{Version} Started");
        StorageStarted();
    }

    

    public override async Task<bool> Handle(byte command, CancellationToken cancel)
    {
        var (length, buffer) = ReadFrame();
        if (length is 0)
            return false;

        (int next, command) = NabuLib.Pop(buffer, 0); 
        var (_, message)    = NabuLib.Slice(buffer, next, length);
        
        switch (command)
        {
            case 0x00:
                Warning($"v{Version} Received: 0, Aborting");
                return false;
            case 0xEF:
                Log($"v{Version} Ending");
                //Storage.End();
                return false;
            case 0x01:
                await Open(message);
                return true;
            case 0x02:
                Get(message);
                return true;
            case 0x03:
                Put(message);
                return true;
            case 0x04:
                DateTime();
                return true;
            default:
                Warning($"Unsupported: {Format(command)}");
                return true;
        }
    }

    public override void Detach()
    {
        Storage.End();
        base.Detach();
    }

}

