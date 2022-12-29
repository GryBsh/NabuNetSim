using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Adaptor
{
    public partial class AdaptorEmulator
    {
        #region HCCA-ACP
        void StorageStarted(short version, string id)
        {
            Send(0x80);
            Send(NABU.FromShort(version));
            Writer.Write(id);
        }

        void StorageLoaded(short index)
        {
            var length = Network.GetResponseSize(index);
            Send(0x83);
            Send(NABU.FromInt(length));
        }

        void StorageError(string message)
        {
            Send(0x82);
            Writer.Write(message);
        }

        async Task StorageHttpGet()
        {
            byte index = Recv();
            string url = Reader.ReadString();
            Log($"RequestStore HTTP Get {index}: {url}");
            var (success, error) = await Network.GetResponse(index, url);
            if (success)
                StorageLoaded(index);
            else
                StorageError(error);

        }

        async Task StorageLoadFile()
        {
            byte index = Recv();
            string filename = Reader.ReadString();
            var (success, error) = await Network.GetResponse(index, filename);
            if (success)
                StorageLoaded(index);
            else
                StorageError(error);
        }

        void DataBuffer(byte[] buffer)
        {
            Send(0x84);
            Send(NABU.FromShort((short)buffer.Length));
            Writer.Write(buffer);
        }

        void StorageGet()
        {
            byte index = Recv();
            int offset = NABU.ToInt(Recv(4));
            short length = NABU.ToShort(Recv(2));
            try {
                var data = Network.GetResponseData(index, offset, length);
                DataBuffer(data);
            } catch (Exception ex) {
                StorageError(ex.Message);
            }
            
        }

        void StoragePut()
        {
            byte index = Recv();
            int offset = NABU.ToInt(Recv(4));
            short length = NABU.ToShort(Recv(2));
            var data = Recv(length);
            var (success, error) = Network.SetResponseData(index, offset);
            if (success)
                Send(0x81); // OK
            else
                StorageError(error);
        }

        void StorageTime()
        {
            var now = DateTime.Now;
            Send(0x85);
            Writer.Write(now.ToString("YYYYMMdd").ToCharArray());
            Writer.Write(now.ToString("HHmmss").ToCharArray());
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
                            return false;
                        case 0x83:
                            return true;
                        case 0x01: // Storage-HTTP-Get
                            await StorageHttpGet();
                            continue;
                        case 0x02:
                            await StorageLoadFile();
                            continue;
                        case 0x03:
                            StorageGet();
                            continue;
                        case 0x04:
                            StoragePut();
                            continue;
                        case 0x05:
                            StorageTime();
                            continue;
                    }
                }
                catch (Exception ex)
                {
                    Warning(ex.Message);
                    break;
                }
            }
            return true;
        }

        #endregion


    }
}
