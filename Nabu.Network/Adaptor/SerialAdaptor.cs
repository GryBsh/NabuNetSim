using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using System.IO.Ports;

namespace Nabu.Adaptor;

public class SerialAdaptor
{
    private SerialAdaptor() { }
    public static async Task Start(
        IServiceProvider serviceProvider, 
        SerialAdaptorSettings settings, 
        CancellationToken stopping,
        int index = -1
    ) {
        var logger = serviceProvider.GetRequiredService<IConsole<SerialAdaptor>>();

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
            ReadBufferSize = 2,
            WriteBufferSize = 2,
        };

        if (settings.ReadTimeout > 0)
        {
            serial.ReadTimeout = settings.ReadTimeout;
        }

        logger.Write(
            $"Port: {settings.Port}, BaudRate: {settings.BaudRate}, ReadTimeout: {settings.ReadTimeout}"
        );

        while (serial.IsOpen is false)
            try
            {
                serial.Open();
            }
            catch (Exception ex)
            {
                logger.WriteWarning($"Serial Adaptor: {ex.Message}");
                Thread.Sleep(5000);
            }
        try
        {
            var adaptor = new EmulatedAdaptor(
                settings,
                serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                serviceProvider.GetServices<IProtocol>(),
                logger,
                serial.BaseStream,
                index
            );
            logger.Write($"Adaptor Started");
            await adaptor.Listen(stopping);

        }
        catch (Exception ex)
        {
            logger.WriteError(ex.Message, ex);
        }

        serial.Close();
        serial.Dispose();

    }

}
