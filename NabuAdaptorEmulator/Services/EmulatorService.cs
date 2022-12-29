using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Network;
using System.IO.Ports;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Reflection.Metadata;
using System.IO;

namespace Nabu.Services;

public class SerialAdaptorEmulator { }
public class TCPAdaptorEmulator { }

public class EmulatorService : BackgroundService
{
    readonly ILogger Logger;
    readonly AdaptorSettings[] DefinedAdaptors;
    private readonly IServiceProvider ServiceProvider;

    public EmulatorService(
        ILogger<EmulatorService> logger,
        Settings settings,
        IServiceProvider serviceProvider
    )
    {
        Logger = logger;
        DefinedAdaptors = settings.Adaptors ?? Array.Empty<AdaptorSettings>();
        ServiceProvider = serviceProvider;
    }

    async Task Serial(AdaptorSettings settings, CancellationToken stopping)
    {
        var serial = new SerialPort(
            settings.Port,
            115200,
            Parity.None,
            8,
            StopBits.Two
        ){
            ReceivedBytesThreshold = 1,
            Handshake   = Handshake.None,
            RtsEnable   = true,
            DtrEnable   = true,
            ReadTimeout = settings.ReadTimeout,
            ReadBufferSize = 1024,
            WriteBufferSize = 1024,
        };

        settings.SendDelay = settings.SendDelay ??
                    settings.Type switch
                    {
                        AdaptorType.TCP => Constants.DefaultTCPSendDelay,
                        AdaptorType.Serial => Constants.DefaultSerialSendDelay,
                        _ => 500
                    };

        Logger.LogInformation(
            $"Serial:" +
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
                Logger.LogWarning($"Serial Adaptor: {ex.Message}");
                Thread.Sleep(5000);
            }

        var adaptor = new AdaptorEmulator(
            ServiceProvider.GetRequiredService<NetworkEmulator>(),
            ServiceProvider.GetRequiredService<ILogger<SerialAdaptorEmulator>>(),
            serial.BaseStream
        );

        adaptor.OnStart(settings);
        await adaptor.Emulate(stopping);
        serial.Close();
        serial.Dispose();
    }

    async Task TCP(AdaptorSettings settings, CancellationToken stopping)
    {
        var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        if (!int.TryParse(settings.Port, out int port))
        {
            port = Constants.DefaultTCPPort;
        };

        settings.SendDelay = settings.SendDelay ??
                    settings.Type switch
                    {
                        AdaptorType.TCP => Constants.DefaultTCPSendDelay,
                        AdaptorType.Serial => Constants.DefaultSerialSendDelay,
                        _ => 500
                    };

        Logger.LogInformation(
            $"TCP:" +
            $"\n\t Port: {port}" +
            $"\n\t SendDelay: {settings.SendDelay}"
        );

        while (socket.Connected is false)
            try
            {
                socket.Connect(
                    new IPEndPoint(IPAddress.Loopback, port)
                );
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"TCP Adaptor: {ex.Message}");
                Thread.Sleep(5000);
            }

        var stream = new NetworkStream(socket);
        var adaptor = new AdaptorEmulator(
            ServiceProvider.GetRequiredService<NetworkEmulator>(),
            ServiceProvider.GetRequiredService<ILogger<TCPAdaptorEmulator>>(),
            stream
        );

        adaptor.OnStart(settings);
        await adaptor.Emulate(stopping);
        stream.Dispose();
        socket.Close();
    }
       

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        await Task.Run(() => {

            // We are going to keep track of the services that were defined
            // so if they stop, we can restart them
            Task[] services = NABU.SetLength(
                DefinedAdaptors.Length, 
                Array.Empty<Task>(), 
                Task.CompletedTask
            ); 
            
            //int[] fails = new int[DefinedAdaptors.Length];
            //bool started = false;
            Logger.LogInformation($"Defined Adaptors: {DefinedAdaptors.Length}");
            

            // Until the host tells us to stop
            while (stopping.IsCancellationRequested is false)
            {
                for (int index = 0; index < DefinedAdaptors.Length; index++)
                {
                    // Is this service stopped?
                    if (services[index].IsCompleted)
                    {
                        // If it was already started, increase the fails
                        //if (started) fails[index] += 1;

                        // If so, restart it
                        var settings = DefinedAdaptors[index];
                        if (settings.Enabled is false) //but not if it's disabled
                            continue;

                        services[index] = settings.Type switch
                        {
                            AdaptorType.Serial => Task.Run(() => Serial(settings, stopping)),
                            AdaptorType.TCP => Task.Run(() => TCP(settings, stopping)),
                            _ => throw new NotImplementedException()
                        };      
                    }
                }
                //started = true;
                Thread.Sleep(100); // Lazy Wait, we don't care how long it takes to resume
            }
        }, stopping);
    }
}


