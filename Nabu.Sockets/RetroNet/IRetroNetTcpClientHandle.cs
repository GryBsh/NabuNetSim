using System.Net.Sockets;

namespace Nabu.Network.RetroNet;

public interface IRetroNetTcpClientHandle
{
    
    int Size();
    Task<Memory<byte>> Read(short length, CancellationToken cancel);
    Task Write(Memory<byte> data, CancellationToken cancel);
    void Close();
}

public class RetroNetTcpClientHandle : IRetroNetTcpClientHandle
{
    TcpClient Client;
    
    public RetroNetTcpClientHandle(string host, int port)
    {
        Client = new TcpClient(host, port);
        Client.NoDelay = true;
        Client.LingerState = new LingerOption(false, 0);
        Client.ReceiveBufferSize = 8;
        Client.SendBufferSize = 8;
    }

    public void Close() {
        Client.Close();
        Client.Dispose();
    }

    public async Task<Memory<byte>> Read(short length, CancellationToken cancel)
    {
        if (Client.Available == 0) return Array.Empty<byte>();

        var buffer = new Memory<byte>(new byte[length]);
        var realLength = await Client.GetStream().ReadAsync(buffer, cancel);
        
        return buffer[0..realLength];
        
    }

    public int Size() => Client.Available;

    public async Task Write(Memory<byte> data, CancellationToken cancel)
    {
        await Client.GetStream().WriteAsync(data, cancel);
    }
}