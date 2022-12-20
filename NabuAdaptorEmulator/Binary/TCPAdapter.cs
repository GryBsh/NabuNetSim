using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;

namespace Nabu.Binary;

public class TCPAdapter : BinaryAdapter
{
   
    Socket? Socket;
    
    readonly TCPAdapterSettings Settings;
    public TCPAdapter(
        TCPAdapterSettings settings,
        ILogger logger
    ) : base(logger)
    {
        Settings = settings;
        
    }

    public override bool Connected => Socket?.Connected is true;

    public override void Open()
    {
        if (Socket is not null && Socket.IsBound) return;
        else if (Socket is not null) {
            Socket.Dispose();
            Socket = null;
        }

        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        while (Socket.Connected is false)
            try
            {
                Socket.Connect(new IPEndPoint(IPAddress.Loopback, Settings.Port));
            }
            catch
            {
                Thread.Sleep(1000);
            }
        Stream = new NetworkStream(Socket);
        Logger.LogInformation("Connected to Emulator");
    }

    public override void Close()
    {
        Stream?.Close();
        Stream?.Dispose();
        Stream = null;
        
        Socket?.Close();
        Socket?.Dispose();
        Socket = null;

    }
}
