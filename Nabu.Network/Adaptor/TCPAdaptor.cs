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

    static async void ServerListen(ILogger logger, EmulatedAdaptor adaptor, Stream stream, CancellationToken stopping)
    {
        logger.LogInformation($"Adaptor Started");
        await adaptor.Listen(stopping);
        await stream.DisposeAsync();
    }
    
    public static async Task Start(
        IServiceProvider serviceProvider, 
        AdaptorSettings settings, 
        CancellationToken stopping,
        int index = -1
    ){
        var logger = serviceProvider.GetRequiredService<ILogger<TCPAdaptor>>();
        var socket = NabuLib.Socket();
        //socket.LingerState = new LingerOption(false, 0);

        if (!int.TryParse(settings.Port, out int port))
        {
            port = Constants.DefaultTCPPort;
        };
        
        

        logger.LogInformation($"Port: {port}");
        
        try
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();
            logger.LogInformation("Server Started.");
        } catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return;
        }
        
        logger.LogInformation($"Adaptor Started");
        
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
                     stream,
                     index
                );
                
                ServerListen(logger, adaptor, stream, stopping);
                logger.LogInformation($"Client Connected");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                break;   
            }
        
        socket.Close();
        socket.Dispose();
    }
}
