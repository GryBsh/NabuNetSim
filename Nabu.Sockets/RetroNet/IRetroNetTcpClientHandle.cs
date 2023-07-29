using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace Nabu.Network.RetroNet;

public interface IRetroNetTcpClientHandle
{
    bool Connected { get; }

    void Close();

    Task<Memory<byte>> Read(short length, CancellationToken cancel);

    int Size();

    Task Write(Memory<byte> data, CancellationToken cancel);
}

public class RetroNetTcpClientHandle : IRetroNetTcpClientHandle
{
    private TcpClient? Client;

    public RetroNetTcpClientHandle(string host, int port)
    {
        try
        {
            Client = new TcpClient(host, port);
            Client.NoDelay = true;
            Client.LingerState = new LingerOption(false, 0);
            Client.ReceiveBufferSize = 8;
            Client.SendBufferSize = 8;
        }
        catch { }
    }

    public bool Connected => Client is not null && Client.Connected;

    public void Close()
    {
        Client?.Close();
        Client?.Dispose();
    }

    public async Task<Memory<byte>> Read(short length, CancellationToken cancel)
    {
        if (Client is null || Client.Available == 0) return Array.Empty<byte>();

        var buffer = new Memory<byte>(new byte[length]);
        try
        {
            var realLength = await Client!.GetStream().ReadAsync(buffer, cancel);

            return buffer[0..realLength];
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    public int Size() => Client is null ? -1 : Client.Available;

    public async Task Write(Memory<byte> data, CancellationToken cancel)
    {
        try
        {
            await Client.GetStream().WriteAsync(data, cancel);
        }
        catch { }
    }
}