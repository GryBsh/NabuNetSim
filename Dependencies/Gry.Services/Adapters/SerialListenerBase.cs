using Gry.Protocols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Gry.Options;
using System.IO.Ports;
using Microsoft.Extensions.Configuration;

namespace Gry.Adapters;

public abstract class SerialListenerBase<TDefinition, TBase>(
    ILogger logger, 
    IServiceScopeFactory scopes, 
    IProtocol<TDefinition>? defaultProtocol = null
)   : StreamListener<TDefinition>(logger, scopes)
    where TDefinition : TBase, new()
    where TBase : AdapterDefinition, new()

{
    public override string Type { get; } = AdapterType.Serial;

    public override async Task Listen(Adapter adapter, CancellationToken stopping)
    {
        using var scope = ScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var definition = Model.As<AdapterDefinition, TDefinition>(adapter.Definition);
        
        var settings = Model.As<AdapterDefinition, SerialAdapterOptions>(adapter.Definition);
        
        var protocols = serviceProvider.GetServices<IProtocol<TDefinition>>();

        
        var serial = new SerialPort(
            settings.Port,
            settings.BaudRate, // > 0 ? settings.BaudRate : settings.BaudRate = 115200, //115200
            Parity.None,
            8,
            StopBits.Two
        )
        {
            ReceivedBytesThreshold = 1,
            Handshake = Handshake.None,
            //RtsEnable = true,
            //DtrEnable = true,
            ReadBufferSize = settings.ReceiveBufferSize,
            WriteBufferSize = settings.SendBufferSize,
        };

        if (settings.Timeout.Microseconds > 0)
        {
            serial.ReadTimeout = settings.Timeout.Milliseconds;
        }
        logger.LogInformation(
            $"Port: {settings.Port}, BaudRate: {settings.BaudRate}, ReadTimeout: {settings.Timeout}"
        );

        settings.Name = settings.Port!.Split(Path.DirectorySeparatorChar)[^1];
        //settings.StoragePath = global.StoragePath;

        var originalSettings = settings with { };

        await SendAsync(Adapter.Startup, Model.As<AdapterDefinition, TBase>(adapter.Definition));

        while (serial.IsOpen is false)
            try
            {
                serial.Open();
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning("Serial Port {} is inaccessible: {}", settings.Port, ex.Message);
                await Task.Delay(10000, stopping);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Serial Port {} failed to open: {}", settings.Port, ex.Message);
                await Task.Delay(10000, stopping);
            }

        try
        {
            logger.LogInformation($"Started");
            await Listen(
                this,
                Logger,
                definition,
                serial.BaseStream,
                defaultProtocol,
                serviceProvider,
                stopping,
                () => (byte)serial.ReadByte()
            );
            logger.LogInformation($"Stopped");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "");
        }

        settings.StoragePath = originalSettings.StoragePath;
        serial.Close();
        serial.Dispose();
        logger.LogInformation("{} disconnected", settings.Port);
    }

}


