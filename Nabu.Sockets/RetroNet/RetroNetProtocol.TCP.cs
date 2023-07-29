using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Nabu.Network.RetroNet
{
    public partial class RetroNetProtocol
    {
        private static ConcurrentBag<int> ServerPorts { get; } = new();
        private Dictionary<byte, IRetroNetTcpClientHandle> Clients { get; } = new();
        private List<Socket> Connected { get; } = new();
        private Socket? Server { get; set; }

        private void Listener(AdaptorSettings settings)
        {
            if (settings is NullAdaptorSettings || Server is not null)
                return;

            var source = NabuNet.Source(settings);
            if (source is null)
                return;

            if (source.EnableRetroNet && source.EnableRetroNetTCPServer)
            {
                if (source.TCPServerPort.HasValue &&
                    ServerPorts.Contains(source.TCPServerPort.Value))
                    return;

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
                                    if (ex.HResult is -2147467259)
                                        continue;

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

        private byte NextClientIndex()
        {
            for (int i = 0x00; i < 0xFF; i++)
            {
                if (Clients.ContainsKey((byte)i)) continue;
                return (byte)i;
            }
            return 0xFF;
        }

        private bool ShutdownTCPServer()
        {
            if (Server is null) return false;
            Logger.WriteWarning($"Shutting down TCP Server");
            foreach (var connected in Connected)
            {
                connected.Disconnect(false);
                connected.Dispose();
            }

            Adaptor.TCPServerActive = false;
            Adaptor.TCPServerProtocol = string.Empty;
            Adaptor.TCPServerPort = 0;

            Server.Dispose();
            Server = null;
            Connected.Clear();
            return true;
        }

        private bool StartTCPServer(ProgramSource source)
        {
            int port = default;
            if (source.TCPServerPort is not null and not 0)
            {
                port = (int)source.TCPServerPort;
                if (ServerPorts.Contains(port) || !NabuLib.IsPortFree(port))
                {
                    Logger.WriteVerbose($"TCP Port `{port}` is unavailable");
                    return false;
                }
            }
            else port = NabuLib.GetFreePort(5817);

            if (port == default)
            {
                Logger.WriteWarning($"TCP Server Ports Exhausted");
                return false;
            }
            Adaptor.TCPServerProtocol = nameof(RetroNetProtocol);
            Adaptor.TCPServerPort = port;

            Server = NabuLib.Socket();
            Server.Bind(new IPEndPoint(IPAddress.Any, port));
            Server.Listen();
            Adaptor.TCPServerActive = true;
            Logger.Write($"RetroNet TCP Server Started on port {port}");
            ServerPorts.Add(port);
            return true;
        }

        private Task TCPHandleHandleClose()
        {
            var handle = Read();
            Clients[handle].Close();
            Log($"Close TCP Client: {handle}");
            Clients.Remove(handle);
            return Task.CompletedTask;
        }

        private Task TCPHandleOpen()
        {
            var url = ReadString();
            var port = Reader.ReadUInt16();
            var handle = Read();

            if (handle is 0xFF) handle = NextClientIndex();
            Log($"Open TCP Client: {handle} {url}:{port}");

            if (handle is 0xFF)
            {
                Writer.Write(0xFF);
                return Task.CompletedTask;
            }

            if (url == string.Empty) url = "127.0.0.1";

            try
            {
                Clients[handle] = new RetroNetTcpClientHandle(url, port);
                if (Clients[handle].Connected)
                {
                    Writer.Write(handle);
                }
                else
                {
                    Writer.Write(Byte(-1));
                }
            }
            catch
            {
                Writer.Write(Byte(-1));
            }

            return Task.CompletedTask;
        }

        private async Task TCPHandleRead(CancellationToken cancel)
        {
            var handle = Read();
            var length = Reader.ReadUInt16();
            try
            {
                var bytes = await Clients[handle].Read((short)length, cancel);
                Debug($"Read TCP Client: {handle}: L:{length}");
                WriteTCPBuffer(bytes);
            }
            catch
            {
                WriteTCPBuffer(Array.Empty<byte>());
            }
        }

        private Task TCPHandleSize()
        {
            var handle = Read();
            try
            {
                var size = Clients[handle].Size();
                Log($"Size TCP Client: {handle} {size}");
                Writer.Write(size);
            }
            catch
            {
                Writer.Write(0);
            }
            return Task.CompletedTask;
        }

        private async Task TCPHandleWrite(CancellationToken cancel)
        {
            var handle = Read();
            var length = Reader.ReadUInt16();
            var data = Reader.ReadBytes(length);
            try
            {
                Debug($"Write TCP Client: {handle}:  L:{length}");
                await Clients[handle].Write(data, cancel);
            }
            catch { }
            Writer.Write((int)length);
        }

        private Task TCPServerAvailable()
        {
            Writer.Write(Server is not null);

            return Task.CompletedTask;
        }

        private Task TCPServerClientCount()
        {
            UpdateConnected();
            Writer.Write((byte)Connected.Count);
            return Task.CompletedTask;
        }

        private async Task TCPServerListen()
        {
            var incoming = await Server!.AcceptAsync();
            var name = $"{incoming.RemoteEndPoint}";
            Log($"TCP Server Client Connected: {name}");
            Connected.Add(incoming);
        }

        private async Task TCPServerRead(CancellationToken cancel)
        {
            UpdateConnected();
            var length = Read();
            Debug($"TCP Server Read: L: {length}");
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

        private async Task TCPServerWrite(CancellationToken cancel)
        {
            UpdateConnected();
            var length = Read();
            //var offset = Reader.ReadByte();
            var data = new Memory<byte>(Reader.ReadBytes(length));
            Log($"TCP Server Write: L: {length}");
            foreach (var client in Connected.ToArray())
            {
                await client.SendAsync(data, cancel);
            }
        }

        private void UpdateConnected()
        {
            foreach (var client in Connected.ToArray())
            {
                if (client.Connected is false)
                {
                    Connected.Remove(client);
                }
            }
        }

        private void WriteTCPBuffer(Memory<byte> bytes)
        {
            Writer.Write(NabuLib.FromInt(bytes.Length));
            Writer.Write(bytes.ToArray());
        }
    }
}