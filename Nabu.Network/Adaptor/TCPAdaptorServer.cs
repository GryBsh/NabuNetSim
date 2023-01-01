using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using System.Net.Sockets;

namespace Nabu.Adaptor;

public class TCPAdaptorServer
{
    private TCPAdaptorServer() { }
    public static async Task Start(IServiceProvider serviceProvider, AdaptorSettings settings, CancellationToken stopping)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<TCPAdaptorServer>>();
        var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        if (!int.TryParse(settings.Port, out int port))
        {
            port = 5919;
        };

        settings.SendDelay ??= Constants.DefaultTCPSendDelay;

        logger.LogInformation(
            $"\n\t Port: {port}" +
            $"\n\t SendDelay: {settings.SendDelay}"
        );

        socket.Listen();

        while (stopping.IsCancellationRequested is false)
            try
            {
                var incoming = await socket.AcceptAsync(stopping);

                using var stream = new NetworkStream(incoming);
                var adaptor = new AdaptorEmulator(
                     settings,
                     serviceProvider.GetRequiredService<NabuNetProtocol>(),
                     serviceProvider.GetServices<IProtocol>(),
                     logger,
                     stream
                 );
                await adaptor.Emulate(stopping);
            }
            catch ( Exception ex )
            {
                logger.LogError(ex.Message, ex);
                break;
            }
    }
}