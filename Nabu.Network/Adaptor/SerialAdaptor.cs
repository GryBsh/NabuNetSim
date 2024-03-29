﻿using Microsoft.Extensions.DependencyInjection;
using Nabu.Network;
using Nabu.Packages;
using Nabu.Services;
using Napa;
using System.IO.Ports;

namespace Nabu.Adaptor;

public class SerialAdaptor
{
    private SerialAdaptor()
    { }

    public static async Task Start(
        IServiceProvider serviceProvider,
        SerialAdaptorSettings settings,
        CancellationToken stopping
    )
    {
        var global = serviceProvider.GetRequiredService<Settings>();
        var logger = serviceProvider.GetRequiredService<ILog<SerialAdaptor>>();
        var storage = serviceProvider.GetRequiredService<StorageService>();
        var packages = serviceProvider.GetRequiredService<PackageService>();
        var sources = serviceProvider.GetRequiredService<ISourceService>();

        var serial = new SerialPort(
            settings.Port,
            settings.BaudRate, //115200
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

        if (settings.ReadTimeout > 0)
        {
            serial.ReadTimeout = settings.ReadTimeout;
        }

        logger.Write(
            $"Port: {settings.Port}, BaudRate: {settings.BaudRate}, ReadTimeout: {settings.ReadTimeout}"
        );

        var name = settings.Port.Split(Path.DirectorySeparatorChar)[^1];
        //settings.StoragePath = global.StoragePath;

        var originalSettings = settings with { };

        while (serial.IsOpen is false)
            try
            {
                EmulatedAdaptor.InitializeAdaptor(settings, sources, storage, packages, name);
                serial.Open();
            }
            catch (UnauthorizedAccessException)
            {
                logger.WriteError($"Serial Adaptor: Port {settings.Port} is inaccessible");
                settings.State = ServiceShould.Stop;
                break;
            }
            catch (Exception ex)
            {
                logger.WriteWarning($"Serial Adaptor: {ex.Message}");
                await Task.Delay(5000, stopping);
            }

        if (serial.IsOpen is false)
        {
            serial.Close();
            serial.Dispose();
            return;
        }

        try
        {
            var adaptor = new EmulatedAdaptor(
                settings,
                serviceProvider.GetRequiredService<ClassicNabuProtocol>(),
                serviceProvider.GetServices<IProtocol>(),
                logger,
                serial.BaseStream
            );
            logger.Write($"Adaptor Started");
            await adaptor.Listen(stopping);
        }
        catch (Exception ex)
        {
            logger.WriteError(ex.Message, ex);
        }

        settings.StoragePath = originalSettings.StoragePath;
        serial.Close();
        serial.Dispose();
    }
}