using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using System.Text;

namespace Nabu.ACP;

public class ACPProtocol : Protocol
{
    StorageService Storage { get; }
    public ACPProtocol(ILogger<ACPProtocol> logger) : base(logger)
    {
        Storage = new(Logger, Settings);
    }

    public override byte Identifier => 0xAF;
    public override string Name => "HCCA-ACP";
    protected override byte Version => 0x01;

    #region ACP Return Messages

    void SendFramed(params byte[] buffer)
    {
        Send(NABU.FromShort((short)buffer.Length)); 
        Send(buffer);
    }

    void SendFramed(params IEnumerable<byte>[] buffer)
    {
        var toSend = Combine(buffer).ToArray();
        Send(NABU.FromShort((short)toSend.Length));
        Send(toSend);
    }

    void StorageStarted()
    {
        SendFramed(
            new byte[] { 0x80 },
            NABU.FromShort(Version),
            String(Id)
        );
    }

    void StorageError(string message)
    {
        SendFramed(
            new byte[] { 0x82 },
            String(message)
        );
    }
    void StorageLoaded(short index, int length)
    {
        SendFramed(
            new byte[] { 0x83 },
            NABU.FromShort(index),
            NABU.FromInt(length)
        );
    }
    void DataBuffer(byte[] buffer)
    {
        SendFramed(
            new byte[] { 0x84 },
            NABU.FromShort((short)buffer.Length),
            buffer
        );
    }

    #endregion

    #region ACP Operations
    async Task ACPOpen()
    {
        try
        {
            byte index = Recv();
            short flags = NABU.ToShort(Recv(2));
            string uri = Reader.ReadString();

            var (success, error, i, length) = await Storage.Open(index, flags, uri);
            if (success)
                StorageLoaded(i, length);
            else
                StorageError(error);
        }
        catch (Exception ex)
        {
            StorageError(ex.Message);
        }
    }

    void ACPGet()
    {
        try
        {
            byte index = Recv();
            int offset = NABU.ToInt(Recv(4));
            short length = NABU.ToShort(Recv(2));
            var (success, error, data) = Storage.Get(index, offset, length);
            if (success is false) StorageError(error);
            else DataBuffer(data);
        }
        catch (Exception ex)
        {
            StorageError(ex.Message);
        }

    }

    void ACPPut()
    {
        try
        {
            byte index = Recv();
            int offset = NABU.ToInt(Recv(4));
            short length = NABU.ToShort(Recv(2));
            var data = Recv(length);
            var (success, error) = Storage.Put(index, offset, data);
            if (success)
                SendFramed(0x81); // OK
            else
                StorageError(error);
        }
        catch (Exception ex)
        {
            StorageError(ex.Message);
        }

    }
    void ACPDateTime()
    {
        var (_, date, time) = Storage.DateTime();
        SendFramed(
            new byte[] { 0x85 },
            Encoding.ASCII.GetBytes(date),
            Encoding.ASCII.GetBytes(time)
        );
        
    }

    #endregion

    public override void Listening()
    {
        Log($"v{Version} Started");
        StorageStarted();
    }

    public override async Task<bool> Listen(byte incoming)
    {
        var size = Recv(2);
        if (size[0] is 0x83 || size[1] is 0x83) 
            return false;

        incoming = Recv();
        switch (incoming)
        {
            case 0x00:
                return false;
            case 0x83:
                return false;
            case 0xEF:
                return false;
            case 0x01:
                await ACPOpen();
                return true;
            case 0x02:
                ACPGet();
                return true;
            case 0x03:
                ACPPut();
                return true;
            case 0x04:
                ACPDateTime();
                return true;
        }
        Warning($"ACP: Unsupported: {size}");
        return true;
    }


}

