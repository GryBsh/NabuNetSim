using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;

namespace Nabu.Services;

public class EmulatorService : BackgroundService
{
    readonly ILogger Logger;
    readonly SerialAdaptorEmulator Serial;
    readonly TCPAdaptorEmulator TCP;
    readonly ServiceSettings Settings;

    public EmulatorService(
        ILogger<EmulatorService> logger,
        ServiceSettings settings,
        SerialAdaptorEmulator adaptor,
        TCPAdaptorEmulator tcp
    )
    {
        Logger = logger;
        Settings = settings;
        Serial = adaptor;
        TCP = tcp;
    }

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        var services = new List<NabuService>();

        if (Settings.Serial)
        {
            Logger.LogInformation("Serial enabled");
            var service = NabuService.From(
                Serial.Emulate,
                stopping,
                () => Serial.Open(),
                () => Serial.Close()
            );
            services.Add(service);
        }
        if (Settings.TCP)
        {
            Logger.LogInformation("TCP enabled");
            var service = NabuService.From(
                TCP.Emulate,
                stopping,
                () => TCP.Open(),
                () => TCP.Close()
            );
            services.Add(service);
        }

        await Task.WhenAll(services.Select(s => s.Task).ToArray());
    }
}


