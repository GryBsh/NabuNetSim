using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using System.IO.Ports;
using Nabu.ACP;

namespace Nabu.Adaptor;

public class SerialAdaptor
{
    private SerialAdaptor() { }
    public static async Task Start(IServiceProvider serviceProvider, AdaptorSettings settings, CancellationToken stopping)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<SerialAdaptor>>();

        var serial = new SerialPort(
            settings.Port,
            settings.BaudRate,
            Parity.None,
            8,
            StopBits.Two
        )
        {
            ReceivedBytesThreshold = 1,
            Handshake = Handshake.None,
            RtsEnable = true,
            DtrEnable = true,
            ReadTimeout = settings.ReadTimeout,
            ReadBufferSize = 1024,
            WriteBufferSize = 1024,
        };

        settings.SendDelay ??= Constants.DefaultSerialSendDelay;

        logger.LogInformation(
            $"\n\t Port: {settings.Port}" +
            $"\n\t BaudRate: {settings.BaudRate}" +
            $"\n\t ReadTimeout: {settings.ReadTimeout}" +
            $"\n\t SendDelay: {settings.SendDelay}"
        );

        while (serial.IsOpen is false)
            try
            {
                serial.Open();
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Serial Adaptor: {ex.Message}");
                Thread.Sleep(5000);
            }
        try
        {
            var adaptor = new AdaptorEmulator(
                settings,
                serviceProvider.GetRequiredService<NabuNetProtocol>(),
                serviceProvider.GetServices<IProtocol>(),
                logger,
                serial.BaseStream
            );

            await adaptor.Emulate(stopping);

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        finally
        {
            serial.Close();
            serial.Dispose();
        }


    }

}
