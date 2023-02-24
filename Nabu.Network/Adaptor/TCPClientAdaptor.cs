using Microsoft.Extensions.DependencyInjection;
using Nabu.Network;
using Nabu.Services;
using System.Net.Sockets;

namespace Nabu.Adaptor;

public class TCPClientAdaptor
{
    private TCPClientAdaptor() { }

    static async Task ClientListen(IConsole logger, EmulatedAdaptor adaptor, Socket socket, Stream stream, CancellationToken stopping)
    {
        
        logger.Write($"TCP Client to {socket.RemoteEndPoint}");
        try {
            await adaptor.Listen(stopping);
        } catch (Exception ex) {
            logger.WriteError(ex.Message);
        }
        stream.Dispose();
        logger.Write($"TCP Client to {socket.RemoteEndPoint} disconnected");
     
    }
    
    public static async Task Start(
        IServiceProvider serviceProvider, 
        AdaptorSettings settings, 
        CancellationToken stopping
    ){
        var logger = serviceProvider.GetRequiredService<IConsole<TCPAdaptor>>();
        
        //socket.LingerState = new LingerOption(false, 0);

        var parts = settings.Port.Split(':');
        var hostname = parts[0];
        var portString = parts.Length > 1 ? parts[1] : string.Empty;

        if (!int.TryParse(portString, out int port))
        {
            port = Constants.DefaultTCPPort;
        };
        
        while (stopping.IsCancellationRequested is false) {
            var socket = NabuLib.Socket();
            try {
                socket.Connect(hostname, port);
            } catch (Exception ex)
            {
                logger.WriteWarning(ex.Message);
                continue;
            }
            logger.Write($"TCP Client Connected to {hostname}:{port}");
            try
            {
                var stream  = new NetworkStream(socket);
                var adaptor = new EmulatedAdaptor(
                     settings,
                     serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                     serviceProvider.GetServices<IProtocol>(),
                     logger,
                     stream
                );
                
                await ClientListen(logger, adaptor, socket, stream, stopping);
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
}