using System.Net.Sockets;

namespace Nabu;

public static partial class NabuLib
{
    public static Span<byte> Frame(byte[] header, params IEnumerable<byte>[] buffer)
    {
        return Concat(header, Concat(buffer)).ToArray().AsSpan();
    }

    public static Socket Socket(bool noDelay = true, int sBufferSize = 1024, int rBufferSize = 1024)
    {
        var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        socket.NoDelay = noDelay;
        //socket.DontFragment = true;
        socket.SendBufferSize = sBufferSize;
        socket.ReceiveBufferSize = rBufferSize;
        socket.LingerState = new LingerOption(false, 0);
        return socket;
    }
}