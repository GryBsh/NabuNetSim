﻿using Microsoft.Extensions.DependencyInjection;
using Nabu.Network;
using System.Net.Sockets;
using System.Net;
using Nabu.Services;

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
        TCPAdaptorSettings settings, 
        CancellationToken stopping
    ){
        //var tcpSettings = (TCPAdaptorSettings)settings;
        var logger = serviceProvider.GetRequiredService<IConsole<TCPAdaptor>>();
        var storage = serviceProvider.GetRequiredService<StorageService>();
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

                storage.InitializeStorage(clientSettings, clientIP);
                
                // CLONE

                var stream = new NetworkStream(incoming);
                var adaptor = new EmulatedAdaptor(
                     clientSettings,
                     serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                     serviceProvider.GetServices<IProtocol>(),
                     logger,
                     stream,
                     name
                );

                Connections[name] = clientSettings;
                ServerListen(logger, adaptor, incoming, stream, clientCancel.Token);

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
