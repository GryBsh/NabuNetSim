using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using Nabu.Services;
using System.Text;

namespace Nabu.Adaptor;

public class TCPAdaptor
{
    private TCPAdaptor() { }

    public static Dictionary<string, TCPAdaptorSettings> Connections { get; } = new();

    static void ServerListen(IConsole logger, EmulatedAdaptor adaptor, Socket socket, Stream stream, CancellationToken stopping)
    {
        Task.Run(async () =>
        {
            logger.Write($"TCP Client Connected from {socket.RemoteEndPoint}");
            try {
                await adaptor.Listen(stopping);
            } catch (Exception ex) {
                logger.WriteError(ex.Message);
            }
            stream.Dispose();
            logger.Write($"TCP Client from {socket.RemoteEndPoint} disconnected");
            var name = $"{socket.RemoteEndPoint}";

            Connections.Remove(name);

        }, stopping);
    }
    
    public static async Task Start(
        IServiceProvider serviceProvider, 
        AdaptorSettings settings, 
        CancellationToken stopping
    ){
        var tcpSettings = (TCPAdaptorSettings)settings;
        var logger = serviceProvider.GetRequiredService<IConsole<TCPAdaptor>>();
        var socket = NabuLib.Socket(true, tcpSettings.SendBufferSize, tcpSettings.ReceiveBufferSize);

        if (!int.TryParse(tcpSettings.Port, out int port))
        {
            port = Constants.DefaultTCPPort;
        };
        
        try
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();
        } catch (Exception ex)
        {
            logger.WriteError(ex.Message);
            return;
        }
        
        logger.Write($"TCP Server Ready, listening on port {port}");
        
        while (stopping.IsCancellationRequested is false)
            try
            {
                Socket incoming = await socket.AcceptAsync(stopping);
                var name = $"{incoming.RemoteEndPoint}";
                var newSettings = (TCPAdaptorSettings)settings with { Port = name }; // CLONE
                var stream  = new NetworkStream(incoming);
                var adaptor = new EmulatedAdaptor(
                     newSettings,
                     serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                     serviceProvider.GetServices<IProtocol>(),
                     logger,
                     stream,
                     name
                );

                Connections[name] = newSettings;
                ServerListen(logger, adaptor, incoming, stream, stopping);
                
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message);
                break;   
            }
        
        socket.Close();
        socket.Dispose();
    }
}
