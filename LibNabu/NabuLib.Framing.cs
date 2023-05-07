using System.Net.NetworkInformation;
using System.Net;
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

    public static IEnumerable<int> GetOpenPorts()
    {
        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();
        return tcpEndPoints.Select(p => p.Port);
    }

    public static bool IsPortFree(int port) 
        => GetOpenPorts().Contains(port) is false;

    public static int GetFreePort(int start, int? count = null)
    {
        count ??= IPEndPoint.MaxPort - start;
        var usedPorts = GetOpenPorts();
        int unusedPort = 0;
        unusedPort = Enumerable.Range(start, count.Value).Where(port => !usedPorts.Contains(port)).FirstOrDefault();
        return unusedPort;
    }
}

