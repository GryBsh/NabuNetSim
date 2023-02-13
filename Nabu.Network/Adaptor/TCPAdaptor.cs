using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace Nabu.Adaptor;

public class TCPAdaptor
{


    private TCPAdaptor() { }

    static async void ServerListen(IConsole logger, EmulatedAdaptor adaptor, Stream stream, CancellationToken stopping)
    {
        logger.Write($"Adaptor Started");
        await adaptor.Listen(stopping);
        await stream.DisposeAsync();
    }
    
    public static async Task Start(
        IServiceProvider serviceProvider, 
        AdaptorSettings settings, 
        CancellationToken stopping
    ){
        var logger = serviceProvider.GetRequiredService<IConsole<TCPAdaptor>>();
        var socket = NabuLib.Socket();
        //socket.LingerState = new LingerOption(false, 0);

        if (!int.TryParse(settings.Port, out int port))
        {
            port = Constants.DefaultTCPPort;
        };
        
        

        logger.Write($"Port: {port}");
        
        try
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();
            logger.Write("Server Started.");
        } catch (Exception ex)
        {
            logger.WriteError(ex.Message);
            return;
        }
        
        logger.Write($"Adaptor Started");
        
        while (stopping.IsCancellationRequested is false)
            try
            {
                Socket incoming = await socket.AcceptAsync(stopping);
                
                var stream  = new NetworkStream(incoming);
                var adaptor = new EmulatedAdaptor(
                     settings,
                     serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                     serviceProvider.GetServices<IProtocol>(),
                     logger,
                     stream
                );
                
                ServerListen(logger, adaptor, stream, stopping);
                logger.Write($"Client Connected");
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
