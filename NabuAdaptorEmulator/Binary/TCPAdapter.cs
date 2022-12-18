using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;

namespace Nabu.Binary;

public class TCPAdapter : BinaryAdapter
{
    TcpClient? Client;
    Socket? Socket;
    
    readonly TCPAdapterSettings Settings;
    public TCPAdapter(
        TCPAdapterSettings settings,
        ILogger logger
    ) : base(logger)
    {
        Settings = settings;
        
    }

    public override void Open()
    {
        if (Socket is not null && Socket.IsBound) return;
        else if (Socket is not null) {
            Socket.Dispose();
            Socket = null;
        }
        Client = new TcpClient();
        int attempts = 0;
        Logger.LogInformation("Attempting Connection to Emulator");
        while (Client.Connected is false)
        {
            if (attempts > Settings.ConnectionAttempts)
            {
                Logger.LogWarning("Connection Failed");
                return;
            }

            attempts++;
            try
            {
                Client.Connect(IPAddress.Loopback, Settings.Port);
            }
            catch
            {
                Thread.Sleep(1000);
            }
        }
        Logger.LogInformation("Connected to Emulator");
        Stream = Client.GetStream();
    }

    public override void Close()
    {
        
        Socket?.Close();
        Socket?.Dispose();
    }
}
