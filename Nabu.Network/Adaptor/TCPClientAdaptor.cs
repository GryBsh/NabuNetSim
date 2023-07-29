using Microsoft.Extensions.DependencyInjection;
using Nabu.Network;
using Nabu.Packages;
using Nabu.Services;
using Napa;
using System.Net.Sockets;

namespace Nabu.Adaptor;

public class TCPClientAdaptor
{
    private TCPClientAdaptor()
    { }

    public static async Task Start(
        IServiceProvider serviceProvider,
        TCPAdaptorSettings settings,
        CancellationToken stopping
    )
    {
        var global = serviceProvider.GetRequiredService<Settings>();
        var logger = serviceProvider.GetRequiredService<ILog<TCPAdaptor>>();
        var storage = serviceProvider.GetRequiredService<StorageService>();
        var packages = serviceProvider.GetRequiredService<PackageService>();
        var sources = serviceProvider.GetRequiredService<ISourceService>();
        //socket.LingerState = new LingerOption(false, 0);

        var parts = settings.Port.Split(':');
        var hostname = parts[0];
        var portString = parts.Length > 1 ? parts[1] : string.Empty;

        if (!int.TryParse(portString, out int port))
        {
            port = Constants.DefaultTCPPort;
        };
        //settings.StoragePath = global.StoragePath;

        while (stopping.IsCancellationRequested is false)
        {
            var socket = NabuLib.Socket(true, settings.SendBufferSize, settings.ReceiveBufferSize);
            var clientIP = socket.RemoteEndPoint!.ToString()!.Split(':')[0];
            try
            {
                EmulatedAdaptor.InitializeAdaptor(settings, sources, storage, packages, clientIP);
                socket.Connect(hostname, port);
            }
            catch (Exception ex)
            {
                logger.WriteWarning(ex.Message);
                await Task.Delay(5000, stopping);
                continue;
            }
            logger.Write($"TCP Client Connected to {hostname}:{port}");
            var name = $"{socket.RemoteEndPoint}";

            try
            {
                var stream = new NetworkStream(socket);
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

    private static async Task ClientListen(ILog logger, EmulatedAdaptor adaptor, Socket socket, Stream stream, CancellationToken stopping)
    {
        logger.Write($"TCP Client to {socket.RemoteEndPoint}");
        try
        {
            await adaptor.Listen(stopping);
        }
        catch (Exception ex)
        {
            logger.WriteError(ex.Message);
        }
        stream.Dispose();
        logger.Write($"TCP Client to {socket.RemoteEndPoint} disconnected");
    }
}