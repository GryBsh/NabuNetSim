using Microsoft.Extensions.DependencyInjection;
using Nabu.Network;
using Nabu.Services;
using System.Net.Sockets;
using System.Text;

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
        TCPAdaptorSettings settings, 
        CancellationToken stopping
    ){
        //var tcpSettings = (TCPAdaptorSettings)settings;
        var logger = serviceProvider.GetRequiredService<IConsole<TCPAdaptor>>();
        var storage = serviceProvider.GetRequiredService<StorageService>();
        //socket.LingerState = new LingerOption(false, 0);

        var parts = settings.Port.Split(':');
        var hostname = parts[0];
        var portString = parts.Length > 1 ? parts[1] : string.Empty;

        if (!int.TryParse(portString, out int port))
        {
            port = Constants.DefaultTCPPort;
        };

        storage.InitializeStorage(settings, settings.Port);

        while (stopping.IsCancellationRequested is false) {
            var socket = NabuLib.Socket(true, settings.SendBufferSize, settings.ReceiveBufferSize);
            try {
                socket.Connect(hostname, port);
            } catch (Exception ex)
            {
                logger.WriteWarning(ex.Message);
                await Task.Delay(5000, stopping);
                continue;
            }
            logger.Write($"TCP Client Connected to {hostname}:{port}");
            var name = $"{socket.RemoteEndPoint}";
            var clientIP = socket.RemoteEndPoint!.ToString()!.Split(':')[0];
            storage.InitializeStorage(settings, clientIP);
            try
            {
                var stream  = new NetworkStream(socket);
                var adaptor = new EmulatedAdaptor(
                     settings,
                     serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                     serviceProvider.GetServices<IProtocol>(),
                     logger,
                     stream,
                     name
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