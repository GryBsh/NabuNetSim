using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using System.Net.Sockets;
using System.Net;

namespace Nabu.Adaptor;

public class TCPAdaptor
{
    private TCPAdaptor() { }
    public static async Task Start(IServiceProvider serviceProvider, AdaptorSettings settings, CancellationToken stopping)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<TCPAdaptor>>();
        var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        if (!int.TryParse(settings.Port, out int port))
        {
            port = Constants.DefaultTCPPort;
        };

        settings.SendDelay = settings.SendDelay ?? Constants.DefaultTCPSendDelay;

        logger.LogInformation(
            $"\n\t Port: {port}" +
            $"\n\t SendDelay: {settings.SendDelay}"
        );

        while (socket.Connected is false)
            try
            {
                socket.Connect(
                    new IPEndPoint(IPAddress.Loopback, port)
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex.Message);
                Thread.Sleep(5000);
            }

        var stream = new NetworkStream(socket);
        
        while(stopping.IsCancellationRequested is false)
            try
            {
                var adaptor = new EmulatedAdaptor(
                     settings,
                     serviceProvider.GetRequiredService<NabuNetProtocol>(),
                     serviceProvider.GetServices<IProtocol>(),
                     logger,
                     stream
                 );
                logger.LogInformation($"Adaptor Started");
                await adaptor.Run(stopping);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                if (socket.Connected is false) break;
            }
        
        stream.Dispose();
        socket.Close();
    }
}
