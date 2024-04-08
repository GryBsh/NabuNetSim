using Gry.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Protocols.Classic;
using Nabu.Settings;
using Nabu.Sources;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;
using Gry.Protocols;
using Gry;
using System.Xml.Linq;
using System.Diagnostics;

namespace Nabu.Adaptors;


public class TCPAdapter(
    ILogger<TCPAdapter> logger, 
    IServiceScopeFactory scopes,    GlobalSettings global,    ProcessService process
) : StreamListener<AdaptorSettings>(logger, scopes)
{
    public override async Task Listen(Adapter adapter, CancellationToken stopping)
    {
        using var scope = ScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var settings = Model.As<AdapterDefinition, TCPAdaptorSettings>(adapter.Definition);

        var socket = NabuLib.Socket(true, settings.SendBufferSize, settings.ReceiveBufferSize);

        if (!int.TryParse(settings.Port, out int port))
        {
            port = Constants.DefaultTCPPort;
        };

        try
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Listening Failed");
            return;
        }

        logger.LogInformation("Listening on port {}", port);
        logger.LogInformation("{}: Started", adapter.Definition.Name ?? adapter.Definition.Port);        if (global.ForceHeadless && global.StartEmulatorInHeadlessMode)            process.Start(global.EmulatorPath);
        while (stopping.IsCancellationRequested is false)
            try
            {
                Socket incoming = await socket.AcceptAsync(stopping);
                var name = $"{incoming.RemoteEndPoint}";
                var clientIP = name.Split(':')[0];
                var clientCancel = CancellationTokenSource.CreateLinkedTokenSource(stopping);
                var clientSettings = Model.Copy<AdapterDefinition, TCPAdaptorSettings>(settings) 
                    with {
                        Port = name,
                        Connection = true,
                        ListenTask = clientCancel,
                        Name = clientIP
                    };

                Send(Adapter.Startup, (AdaptorSettings)clientSettings);

                var stream = new NetworkStream(incoming);
            
                Connections[name] = clientSettings;
                Connection(Logger, clientSettings, incoming, stream, clientCancel.Token);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{}: Failed", adapter.Definition.Name);
                break;
            }
        logger.LogInformation("{}: Stopped", adapter.Definition.Port);
        socket.Close();
        socket.Dispose();
    }

    public static ConcurrentDictionary<string, TCPAdaptorSettings> Connections { get; } = new();

    private async void Connection(ILogger logger, TCPAdaptorSettings settings, Socket socket, Stream stream, CancellationToken stopping)
    {
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
                    settings, 
                    stream,
                    serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                    serviceProvider,
                    stopping
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{}: ABORT", settings.Name);
            }
        }, stopping);

        logger.LogInformation("{}: Disconnected", socket.RemoteEndPoint);
        stream.Dispose();
        socket.Dispose();
        Connections.Remove(settings.Port!, out _);
    }

    public override string Type { get; } = AdapterType.TCP;
}
