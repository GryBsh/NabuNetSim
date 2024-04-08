using Gry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using Gry.Options;

namespace Gry.Adapters;

public abstract class TCPListenerBase<TDefinition, TBase>(
    ILogger logger,
    IServiceScopeFactory scopes
) : StreamListener<TDefinition>(logger, scopes)
    where TDefinition : TBase, new()
    where TBase : AdapterDefinition, new()
{
    const int DefaultTCPPort = 9090;

    public override async Task Listen(Adapter adapter, CancellationToken stopping)
    {
        using var scope = ScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var settings = Model.As<AdapterDefinition, TCPAdapterOptions>(adapter.Definition);

        var socket = Net.SerialSocket(true, settings.SendBufferSize, settings.ReceiveBufferSize);

        if (!int.TryParse(settings.Port, out int port) || port is 0)
        {
            port = DefaultTCPPort;
        };

        try
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error Starting Adapter");
            return;
        }

        Logger.LogInformation("Listening on port {}", port);

        //Send(Adapter.StartupEvent, (TOptions)settings);

        while (stopping.IsCancellationRequested is false)
            try
            {
                Socket incoming = await socket.AcceptAsync(stopping);
                var name = $"{incoming.RemoteEndPoint}";
                var clientIP = name.Split(':')[0];
                var clientCancel = CancellationTokenSource.CreateLinkedTokenSource(stopping);
                var clientSettings = Model.Copy<TCPAdapterOptions, TCPAdapterOptions>(settings) with
                {
                    Port = name,
                    Connection = true,
                    ListenerTokenSource = clientCancel,
                    Name = clientIP
                };

                Send(Adapter.Startup, Model.As<AdapterDefinition, TBase>(adapter.Definition));

                var stream = new NetworkStream(incoming);

                Connections[name] = clientSettings;
                Connection(clientSettings, incoming, stream, clientCancel.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "{}: Failed", adapter.Definition.Port);
                break;
            }
        logger.LogInformation($"Stopped");
        socket.Close();
        socket.Dispose();
    }

    public static ConcurrentDictionary<string, TCPAdapterOptions> Connections { get; } = new();

    private async void Connection(TCPAdapterOptions settings, Socket socket, Stream stream, CancellationToken stopping)
    {
        var name = $"{socket.RemoteEndPoint}";

        await Task.Run(async () =>
        {
            logger.LogInformation("{}: Connected", socket.RemoteEndPoint);
            try
            {
                using var scope = ScopeFactory.CreateScope();
                var serviceProvider = scope.ServiceProvider;

                await Listen(
                    this,
                    Logger,
                    Model.As<AdapterDefinition, TDefinition>(settings),
                    stream,
                    null,
                    serviceProvider,
                    stopping
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{}: ABORT", name);
            }
        }, stopping);

        logger.LogInformation("{}: Disconnected", socket.RemoteEndPoint);
        Connections.Remove(name, out var _);
        stream.Dispose();
        socket.Dispose();
    }

    public override string Type { get; } = AdapterType.TCP;
}


