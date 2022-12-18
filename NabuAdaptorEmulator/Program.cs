using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nabu.Adaptor;
using Nabu.Network;
using Nabu.Binary;
using Nabu.Services;

/*
        --Serial:Port="NAME/PATH"
        Sets the serial port

        --Network:Source="NAME"
        Sets the source

        --Network:Channel="NAME"
        Sets the channel        
*/

var builder = Host.CreateDefaultBuilder(args);


builder.ConfigureServices(
    (context, services) =>
    {
        T AddSettings<T>(
            HostBuilderContext context,
            IServiceCollection services,
            string section
        ) where T : class, new()
        {
            T settings = new();
            context.Configuration.Bind(section, settings);
            services.AddSingleton(settings);
            return settings;
        }

        AddSettings<ServiceSettings>(context, services, "Services");
        AddSettings<SerialAdapterSettings>(context, services, "Serial");
        AddSettings<TCPAdapterSettings>(context, services, "TCP");
        AddSettings<AdaptorSettings>(context, services, "Adaptor");
        AddSettings<NetworkSettings>(context, services, "Network");
        AddSettings<ChannelSources>(context, services, "Sources");

        services.AddLogging(
            logging =>

                logging.AddSimpleConsole(
                    options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    }
                )
        );

        services.AddHttpClient();
        services.AddSingleton<SerialAdaptorEmulator>();
        services.AddSingleton<TCPAdaptorEmulator>();
        services.AddSingleton<NetworkEmulator>();
        services.AddHostedService<EmulatorService>();
    }
);



//var tokenSource = new CancellationTokenSource();
await builder.RunConsoleAsync();
