using Gry;
using Gry.Adapters;
using Gry.Protocols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Protocols.Classic;
using Nabu.Settings;
using Nabu.Sources;
using System.IO.Ports;
using System.Xml.Linq;

namespace Nabu.Adaptors;

public class SerialAdapter(ILogger<SerialAdapter> logger, IServiceScopeFactory scopes) : StreamListener<AdaptorSettings>(logger,scopes)
{
    public override string Type { get; } = AdapterType.Serial;

    public override async Task Listen(Adapter adapter, CancellationToken stopping)
    {
        using var scope = ScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var settings = Model.As<AdapterDefinition, SerialAdaptorSettings>(adapter.Definition);
        var protocols = serviceProvider.GetServices<IProtocol<AdaptorSettings>>();

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
            ReadBufferSize = 2,
            WriteBufferSize = 2,
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

        await SendAsync(Adapter.Startup, (AdaptorSettings)settings);

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
                settings, 
                serial.BaseStream, 
                serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
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
