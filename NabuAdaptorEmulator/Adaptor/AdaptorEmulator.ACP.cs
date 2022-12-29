using Nabu.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Adaptor;

public partial class AdaptorEmulator
{
    #region HCCA-ACP
    void StorageStarted(short version, string id)
    {
        Send(0x80);
        Send(NABU.FromShort(version));
        Writer.Write(id);
    }

    void StorageError(string message)
    {
        Send(0x82);
        Writer.Write(message);
    }
    void StorageLoaded(short index, int length)
    {
        Send(0x83);
        Send(NABU.FromShort(index));
        Send(NABU.FromInt(length));
    }
    void DataBuffer(byte[] buffer)
    {
        Send(0x84);
        Send(NABU.FromShort((short)buffer.Length));
        SlowerSend(buffer);
    }

    async Task StorageOpen()
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

    void StorageGet()
    {
        try
        {
            byte index = Recv();
            int offset = NABU.ToInt(Recv(4));
            short length = NABU.ToShort(Recv(2));
        
            var (success, error, data) = Storage.Get(index, offset, length);
            if (success is false) StorageError(error);
            else DataBuffer(data);

        } catch (Exception ex) {
            StorageError(ex.Message);
        }
    }

    void StoragePut()
    {
        try
        {
            byte index = Recv();
            int offset = NABU.ToInt(Recv(4));
            short length = NABU.ToShort(Recv(2));
            var data = Recv(length);
            var (success, error) = Storage.Put(index, offset, data);
            if (success)
                Send(0x81); // OK
            else
                StorageError(error);
        }
        catch (Exception ex)
        {
            StorageError(ex.Message);
        }
    }
    void StorageTime()
    {
        var (_, date, time) = Storage.DateTime();
        Send(0x85);
        Writer.Write(date);
        Writer.Write(time);
    }

    async Task<bool> ACPHandler(CancellationToken cancel)
    {
        short version = 0x01;
        var id = Guid.NewGuid().ToString();
        StorageStarted(version, id);
        Log($"Started HCCA-ACP v{version} [{id}]");

        while (cancel.IsCancellationRequested is false)
        {
            try
            {
                var input = Recv();
                switch (input)
                {
                    case 0x00:
                        Warning($"FAIL HCCA-ACP v{version} [{id}]");
                        return false;
                    case 0x83:
                        goto End;
                    case 0xEF: 
                        return true;
                    case 0x01:
                        await StorageOpen();
                        continue;
                    case 0x02:
                        StorageGet();
                        continue;
                    case 0x03:
                        StoragePut();
                        continue;
                    case 0x04:
                        StorageTime();
                        continue;
                }
            }
            catch (Exception ex)
            {
                Warning(ex.Message);
                continue;
            }
        }
    End:
        Log($"End HCCA-ACP v{version} [{id}]");
        return true;
    }

    #endregion


}
