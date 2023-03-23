using System.Net.Sockets;

namespace Nabu;

public static partial class NabuLib
{
    public static Span<byte> Frame(byte[] header, params IEnumerable<byte>[] buffer)
    {
        return Concat(header, Concat(buffer)).ToArray().AsSpan();
    }

    public static Socket Socket(bool noDelay = true, int sBufferSize = 8, int rBufferSize = 8)
    {
        var socket = 
            new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            )
            {
                NoDelay = noDelay,
                SendBufferSize = sBufferSize,
                ReceiveBufferSize = rBufferSize,
                LingerState = new LingerOption(false, 0)
            };
        return socket;
    }
}

