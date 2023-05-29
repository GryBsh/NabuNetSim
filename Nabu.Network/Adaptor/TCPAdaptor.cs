using Microsoft.Extensions.DependencyInjection;
using Nabu.Network;
using System.Net.Sockets;
using System.Net;
using Nabu.Services;
using Napa;

namespace Nabu.Adaptor;

public class TCPAdaptor
{
    private TCPAdaptor() { }

    public static Dictionary<string, TCPAdaptorSettings> Connections { get; } = new();

    static void ServerListen(ILog logger, EmulatedAdaptor adaptor, Socket socket, Stream stream, IEnumerable<IProtocol> protocols, CancellationToken stopping)
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
            foreach (var protocol in protocols) { protocol.Detach(); }

        }, stopping);
    }
    
    public static async Task Start(
        IServiceProvider serviceProvider, 
        TCPAdaptorSettings settings, 
        CancellationToken stopping
    ){
        //var tcpSettings = (TCPAdaptorSettings)settings;
        var logger = serviceProvider.GetRequiredService<ILog<TCPAdaptor>>();
        var storage = serviceProvider.GetRequiredService<StorageService>();
        var packages = serviceProvider.GetRequiredService<IPackageManager>();
        var socket = NabuLib.Socket(true, settings.SendBufferSize, settings.ReceiveBufferSize);

        if (!int.TryParse(settings.Port, out int port))
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
                var clientIP = name.Split(':')[0];
                var clientCancel = CancellationTokenSource.CreateLinkedTokenSource(stopping);
                var clientSettings = settings with { Port = name, Connection = true, ListenTask = clientCancel };
                
                await packages.UpdateInstalled();
                storage.UpdateStorageFromPackages(packages, clientSettings);
                storage.UpdateStorage(clientSettings, clientIP);

                var protocols = serviceProvider.GetServices<IProtocol>();

                var stream = new NetworkStream(incoming);
                var adaptor = new EmulatedAdaptor(
                     clientSettings,
                     serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                     protocols,
                     logger,
                     stream,
                     name
                );

                Connections[name] = clientSettings;
                ServerListen(logger, adaptor, incoming, stream, protocols, clientCancel.Token);

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
