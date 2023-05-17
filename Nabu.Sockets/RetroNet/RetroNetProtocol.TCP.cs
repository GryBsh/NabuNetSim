using System.Net;
using System.Net.Sockets;

namespace Nabu.Network.RetroNet
{


    public partial class RetroNetProtocol
    {
        Dictionary<byte, IRetroNetTcpClientHandle> Clients { get; } = new();

        void WriteTCPBuffer(Memory<byte> bytes)
        {
            Writer.Write(NabuLib.FromInt(bytes.Length));
            Writer.Write(bytes.ToArray());
        }

        byte NextClientIndex()
        {
            for (int i = 0x00; i < 0xFF; i++)
            {
                if (Clients.ContainsKey((byte)i)) continue;
                return (byte)i;
            }
            return 0xFF;
        }

        bool StartTCPServer(ProgramSource source)
        {
            var portFree = NabuLib.IsPortFree(source.RetroNetTCPServerPort);
            if (!portFree)
            {
                Logger.WriteWarning($"TCP Server is already open by another client on {source.RetroNetTCPServerPort}");
                return false;
            }
            Server = NabuLib.Socket();
            Server.Bind(new IPEndPoint(IPAddress.Any, source.RetroNetTCPServerPort));
            Server.Listen();
            Logger.Write($"RetroNet TCP Server Started on port {source.RetroNetTCPServerPort}");
            return true;
        }

        async Task TCPServerListen()
        {
            var incoming = await Server!.AcceptAsync();
            var name = $"{incoming.RemoteEndPoint}";
            Log($"TCP Server Client Connected: {name}");
            Connected.Add(incoming);
        }

        void Listener(AdaptorSettings settings)
        {
            if (settings is NullAdaptorSettings)
                return;

            var source = NabuNet.Source(settings);
            if (source is null)
                return;

            if (Server is null && source.EnableRetroNet && source.EnableRetroNetTCPServer)
            {
                try
                {
                    var started = StartTCPServer(source);
                    if (!started) return;
                    Task.Run(
                        async () =>
                        {
                            while (Server is not null)
                            {
                                try
                                {
                                    await TCPServerListen();
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteWarning($"TCP Server Failed: {ex.Message}");
                                }
                                
                            }
                        }
                    );
                    
                    
                }
                catch (Exception ex)
                {
                    Logger.WriteError($"Failed to start TCP Server: {ex.Message}");
                }
            }
            
        }

        void ShutdownTCPServer()
        {
            Logger.WriteWarning($"Shutting down lingering TCP Server");
            if (Server is null) return;
            foreach (var connected in Connected)
            {
                connected.Disconnect(false);
                connected.Dispose();
            }

            //Server.Disconnect(true);
            Server.Dispose();
            Server = null;
            Connected.Clear();
        }

        private Task TCPHandleOpen()
        {
            var url = RecvString();
            var port = Reader.ReadUInt16();
            var handle = Recv();

            if (handle is 0xFF) handle = NextClientIndex();
            Log($"Open TCP Client: {handle} {url}:{port}");

            if (handle is 0xFF)
            {
                Writer.Write(0xFF);
            }
            else
            {
                Clients[handle] = new RetroNetTcpClientHandle(url, port);
                Writer.Write(handle);
            }
            return Task.CompletedTask;
        }

        private Task TCPHandleHandleClose()
        {
            var handle = Recv();
            Clients[handle].Close();
            Log($"Close TCP Client: {handle}");
            Clients.Remove(handle);
            return Task.CompletedTask;
        }

        private async Task TCPHandleRead(CancellationToken cancel)
        {
            var handle = Recv();
            var length = Reader.ReadUInt16();
            var bytes = await Clients[handle].Read((short)length, cancel);
            Log($"Read TCP Client: {handle}: L:{length}");
            WriteTCPBuffer(bytes);
        }

        private async Task TCPHandleWrite(CancellationToken cancel)
        {
            var handle = Recv();
            var length = Reader.ReadUInt16();
            var data = Reader.ReadBytes(length);
            Log($"Write TCP Client: {handle}:  L:{length}");
            await Clients[handle].Write(data, cancel);
            Writer.Write((int)length);
        }

        private Task TCPHandleSize()
        {
            var handle = Recv();
            var size = Clients[handle].Size();
            Log($"Size TCP Client: {handle} {size}");
            Writer.Write(size);
            return Task.CompletedTask;
        }


        static Socket? Server { get; set; }
        static List<Socket> Connected { get; } = new();

        void UpdateConnected()
        {
            foreach (var client in Connected.ToArray())
            {
                if (client.Connected is false)
                {
                    Connected.Remove(client);
                }
            }
        }

        Task TCPServerClientCount()
        {
            UpdateConnected();
            Writer.Write((byte)Connected.Count);
            return Task.CompletedTask;
        }

        Task TCPServerAvailable()
        {
            Writer.Write(Server is not null);
            return Task.CompletedTask;
        }


        async Task TCPServerRead(CancellationToken cancel)
        {
            UpdateConnected();
            var length = Recv();
            Log($"TCP Server Read: L: {length}");
            var firstWithData = Connected.Where(c => c.Available > 0).FirstOrDefault();
            if (firstWithData is null)
            {
                Writer.Write((byte)0);
            }
            else
            {
                var buffer = new Memory<byte>(new byte[length]);
                var realLength = await firstWithData.ReceiveAsync(buffer, cancel);
                Writer.Write((byte)realLength);
                Writer.Write(buffer[0..realLength].ToArray());
            }
        }

        async Task TCPServerWrite(CancellationToken cancel)
        {
            UpdateConnected();
            var length = Recv();
            //var offset = Reader.ReadByte();
            var data = new Memory<byte>(Reader.ReadBytes(length));
            Log($"TCP Server Write: L: {length}");
            foreach (var client in Connected.ToArray())
            {
                await client.SendAsync(data, cancel);
            }
        }
    }
}
